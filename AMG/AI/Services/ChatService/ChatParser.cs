using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService
{
    public readonly record struct ChatPlayer(byte Id, string Name, string Color);

    public sealed class ParseResult
    {
        public ChatIntent Intent;   // null = not understood
        public float Confidence;
        public string Rationale = "";
    }

    // Turns what a human typed into a ChatIntent. It decides nothing: it only translates.
    // Pure logic (players and rooms are passed in); the game adapter lives in ChatParser.Game.cs.
    //
    // v0 understands: where's-the-body question, body-location statement, "&lt;player&gt; wya?",
    // and BARE rooms ("elec", "upper", "nav") which are resolved against what was just asked (ClaimStore).
    public static partial class ChatParser
    {
        // A bare room with no open question counts as the body location only this early in the meeting
        private const float BodyGuessWindow = 15f;

        private sealed class Entities
        {
            public bool[] Consumed;
            public readonly List<byte> Players = [];
            public bool PlayerAmbiguous;
            public SystemTypes? Room;
            public bool RoomAmbiguous;
        }

        public static ParseResult Parse(string text, byte senderId, ChatContextEnum context,
            IReadOnlyList<ChatPlayer> players, IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var tokens = ChatLexicon.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count == 0) return Unknown("empty");

            bool hasQuestionMark = tokens.Contains("?");
            var ent = Extract(tokens, players, rooms);

            bool selfRef = tokens.Any(ChatLexicon.SelfWords.Contains);
            bool hasBody = tokens.Any(ChatLexicon.BodyWords.Contains);
            bool asksWhere = tokens.Any(ChatLexicon.WhereWords.Contains);
            bool youWord = tokens.Any(ChatLexicon.YouWords.Contains);
            bool abbrev = tokens.Any(ChatLexicon.WhereYouAbbrev.Contains);

            // 1) "white wya?" / "where were you white" -> asking somebody where they were
            if (ent.Players.Count == 1 && !ent.PlayerAmbiguous && ent.Players[0] != senderId && (abbrev || (asksWhere && youWord)))
            {
                return Ok(new ChatIntent
                {
                    Type = ChatIntentEnum.Question, PhraseId = "Whereabouts_Ask", Context = context, Target = ent.Players[0],
                }, 0.9f, "whereabouts question");
            }

            // 2) "where?" / "where was the body" / "loc?" -> asking where the body is
            if (ent.Players.Count == 0 && ent.Room == null && !ent.RoomAmbiguous && asksWhere
                && tokens.All(ChatLexicon.BodyQuestionWords.Contains))
            {
                return Ok(new ChatIntent { Type = ChatIntentEnum.Question, PhraseId = "Body_Location", Context = context },
                    0.9f, "body location question");
            }

            // a question about a room ("body in elec?") needs the Confirm/Deny flow, not here yet
            if (ent.Room != null && hasBody && hasQuestionMark)
                return Unknown("body/room question (not supported yet)");

            // 3) "body in electrical" / "found in elec"
            if (ent.Room is { } bodyRoom && ent.Players.Count == 0 && hasBody)
            {
                return Ok(new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = bodyRoom },
                    0.85f, "body location statement");
            }

            // 4) bare room: "elec", "upper", "it was in nav" -> an answer to whatever was just asked
            bool onlyFillers = OnlyFillers(tokens, ent.Consumed, -1);

            if (ent.Room == null && ent.RoomAmbiguous && ent.Players.Count == 0 && onlyFillers)
                return Unknown("ambiguous room (engine: upper or lower?)");

            if (ent.Room is { } room && ent.Players.Count == 0 && onlyFillers)
            {
                float hedge = hasQuestionMark ? 0.25f : 0f;

                // someone asked THIS player where they were -> it's their whereabouts ("white wya?" / "nav")
                var asked = ClaimStore.OpenQuestionFor(senderId);
                if (asked != null && (asked.Intent.PhraseId == "Whereabouts_Ask" || asked.Intent.PhraseId == "Whereabouts_Player"))
                {
                    return Ok(new ChatIntent
                    {
                        Type = ChatIntentEnum.Defend, PhraseId = "Alibi_Tasks", Context = context, Room = room, ReplyToClaim = asked.Id,
                    }, 0.9f - hedge, "answer to whereabouts question");
                }

                if (!selfRef)
                {
                    var bodyQ = ClaimStore.OpenGeneralQuestion("Body_Location", senderId);
                    if (bodyQ != null)
                    {
                        return Ok(new ChatIntent
                        {
                            Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = room, ReplyToClaim = bodyQ.Id,
                        }, 0.9f - hedge, "answer to body location question");
                    }
                }

                // "i was in nav" with nobody asking: still an alibi about themselves
                if (selfRef)
                {
                    return Ok(new ChatIntent { Type = ChatIntentEnum.Defend, PhraseId = "Alibi_Tasks", Context = context, Room = room },
                        0.7f - hedge, "own location statement");
                }

                // nobody asked, nobody said where the body is, meeting just started: most likely the reporter
                if (!ClaimStore.Any("Body_Location", ChatIntentEnum.Report) && ClaimStore.Elapsed <= BodyGuessWindow)
                {
                    return Ok(new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = room },
                        0.6f - hedge, "guess: first room at meeting start = body location");
                }

                return Unknown("room only, nothing to attach it to");
            }

            return Unknown("no rule matched");
        }


        private static Entities Extract(List<string> tokens, IReadOnlyList<ChatPlayer> players, IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var e = new Entities { Consumed = new bool[tokens.Count] };
            int n = tokens.Count;

            // rooms (strong aliases, longest first)
            var strong = BuildStrongRooms(rooms);
            for (int i = 0; i < n; i++)
            {
                if (i + 1 < n && strong.TryGetValue(tokens[i] + " " + tokens[i + 1], out var two))
                {
                    SetRoom(e, two);
                    e.Consumed[i] = e.Consumed[i + 1] = true;
                    i++;
                }
                else if (strong.TryGetValue(tokens[i], out var one))
                {
                    SetRoom(e, one);
                    e.Consumed[i] = true;
                }
                else if (ChatLexicon.AmbiguousRoomWords.Contains(tokens[i]))
                {
                    e.RoomAmbiguous = true;
                    e.Consumed[i] = true;
                }
            }

            // players (full name, name words, colour)
            var keys = BuildPlayerKeys(players);
            for (int i = 0; i < n; i++)
            {
                if (e.Consumed[i] || tokens[i] == "?") continue;

                if (i + 1 < n && !e.Consumed[i + 1] && keys.TryGetValue(tokens[i] + " " + tokens[i + 1], out var ids2))
                {
                    AddPlayers(e, ids2);
                    e.Consumed[i] = e.Consumed[i + 1] = true;
                    i++;
                }
                else if (keys.TryGetValue(tokens[i], out var ids1))
                {
                    AddPlayers(e, ids1);
                    e.Consumed[i] = true;
                }
                else if (TryFuzzy(tokens[i], keys, out var fuzzy))
                {
                    AddPlayers(e, fuzzy);
                    e.Consumed[i] = true;
                }
            }

            // weak room aliases ("sec", "up", "le") only when they are the whole message
            if (e.Room == null && !e.RoomAmbiguous && e.Players.Count == 0 && !e.PlayerAmbiguous)
            {
                for (int i = 0; i < n; i++)
                {
                    if (e.Consumed[i]) continue;
                    if (!ChatLexicon.WeakRoomAliases.TryGetValue(tokens[i], out var weak) || !rooms.ContainsKey(weak)) continue;
                    if (!OnlyFillers(tokens, e.Consumed, i)) break;

                    e.Room = weak;
                    e.Consumed[i] = true;
                    break;
                }
            }

            return e;
        }

        private static void SetRoom(Entities e, SystemTypes room)
        {
            if (e.Room == null) e.Room = room;
            else if (e.Room != room) { e.Room = null; e.RoomAmbiguous = true; }
        }

        private static void AddPlayers(Entities e, List<byte> ids)
        {
            if (ids.Count != 1) { e.PlayerAmbiguous = true; return; }
            if (!e.Players.Contains(ids[0])) e.Players.Add(ids[0]);
        }

        private static Dictionary<string, SystemTypes> BuildStrongRooms(IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var map = new Dictionary<string, SystemTypes>();

            foreach (var kv in ChatLexicon.StrongRoomAliases)
                if (rooms.ContainsKey(kv.Value)) map[kv.Key] = kv.Value;

            // whatever the active language pack calls the rooms ("Upper Engine", "Navegação"...)
            foreach (var kv in rooms)
            {
                var name = ChatLexicon.Normalize(kv.Value);
                if (name.Length == 0) continue;
                map[name] = kv.Key;
                map[name.Replace(" ", "")] = kv.Key;
            }

            return map;
        }

        private static Dictionary<string, List<byte>> BuildPlayerKeys(IReadOnlyList<ChatPlayer> players)
        {
            var keys = new Dictionary<string, List<byte>>();

            void Add(string key, byte id)
            {
                if (key.Length < 2) return;
                if (!keys.TryGetValue(key, out var list)) keys[key] = list = [];
                if (!list.Contains(id)) list.Add(id);
            }

            foreach (var p in players)
            {
                var name = ChatLexicon.Normalize(p.Name);
                if (name.Length > 0)
                {
                    Add(name, p.Id);
                    foreach (var word in name.Split(' '))
                        if (!ChatLexicon.StopNames.Contains(word)) Add(word, p.Id);
                }

                var color = ChatLexicon.Normalize(p.Color);
                if (color.Length > 0) Add(color, p.Id);
            }

            return keys;
        }

        private static bool TryFuzzy(string token, Dictionary<string, List<byte>> keys, out List<byte> ids)
        {
            ids = null;
            if (token.Length < 5 || token.Any(char.IsDigit) || IsKnownWord(token)) return false;

            var hits = keys.Where(kv => kv.Key.Length >= 5 && !kv.Key.Contains(' ') && WithinOneEdit(token, kv.Key)).ToList();
            if (hits.Count != 1) return false;

            ids = hits[0].Value;
            return true;
        }

        private static bool IsKnownWord(string t) =>
            ChatLexicon.Fillers.Contains(t) || ChatLexicon.SelfWords.Contains(t) || ChatLexicon.BodyWords.Contains(t)
            || ChatLexicon.WhereWords.Contains(t) || ChatLexicon.YouWords.Contains(t) || ChatLexicon.WhereYouAbbrev.Contains(t)
            || ChatLexicon.BodyQuestionWords.Contains(t);

        private static bool WithinOneEdit(string a, string b)
        {
            if (Math.Abs(a.Length - b.Length) > 1) return false;

            int i = 0, j = 0, edits = 0;
            while (i < a.Length && j < b.Length)
            {
                if (a[i] == b[j]) { i++; j++; continue; }
                if (++edits > 1) return false;
                if (a.Length > b.Length) i++;
                else if (a.Length < b.Length) j++;
                else { i++; j++; }
            }

            return edits + (a.Length - i) + (b.Length - j) <= 1;
        }

        private static bool OnlyFillers(List<string> tokens, bool[] consumed, int except)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i == except || consumed[i]) continue;
                var t = tokens[i];
                if (t == "?" || ChatLexicon.Fillers.Contains(t) || ChatLexicon.SelfWords.Contains(t)) continue;
                return false;
            }
            return true;
        }

        private static ParseResult Ok(ChatIntent intent, float confidence, string why)
        {
            intent.Confidence = confidence;
            return new ParseResult { Intent = intent, Confidence = confidence, Rationale = why };
        }

        private static ParseResult Unknown(string why) => new() { Intent = null, Confidence = 0f, Rationale = why };
    }
}
