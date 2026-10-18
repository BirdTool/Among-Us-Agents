using System;
using System.Collections.Generic;
using System.Linq;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService
{
    // The colour as the game displays it (may be translated); another way players may refer to them.
    public readonly record struct ChatPlayer(byte Id, string Name, string Color, string ColorAlt = null);

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

        // "First room said = the body" only makes sense at the very start of the chat, not after talk about votes.
        private const int BodyGuessMaxPriorMessages = 2;

        // A bare room only agrees/disagrees with a body location said this recently.
        private const float BodyReplyWindow = 25f;

        private sealed class Entities
        {
            public bool[] Consumed;
            public int[] Kind;                                   // per token: 0 plain, 1 room, 2 player, 3 continuation of the previous entity
            public readonly List<byte?> PlayerSlots = [];        // in order of appearance; null = a player was named but is ambiguous
            public readonly List<byte> Players = [];
            public bool PlayerAmbiguous;
            public SystemTypes? Room;
            public bool RoomAmbiguous;
        }

        public static ParseResult Parse(string text, byte senderId, ChatContextEnum context,
            IReadOnlyList<ChatPlayer> players, IReadOnlyDictionary<SystemTypes, string> rooms,
            IReadOnlyList<PhraseGroup> groups = null)
        {
            var tokens = ChatLexicon.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count == 0) return Unknown("empty");

            tokens = SplitGlued(tokens, players);
            tokens = [.. tokens.Select(t => ChatLexicon.Typos.TryGetValue(t, out var fixedWord) ? fixedWord : t)];

            bool hasQuestionMark = tokens.Contains("?");
            var ent = Extract(tokens, players, rooms);

            bool selfRef = tokens.Any(ChatLexicon.SelfWords.Contains);
            bool hasBody = tokens.Any(ChatLexicon.BodyWords.Contains);
            bool asksWhere = tokens.Any(ChatLexicon.WhereWords.Contains);
            bool youWord = tokens.Any(ChatLexicon.YouWords.Contains);
            bool abbrev = tokens.Any(ChatLexicon.WhereYouAbbrev.Contains);

            bool negated = tokens.Any(ChatLexicon.Negators.Contains);

            // 1) "white wya?" / "where were you white" -> asking somebody where they were
            //    (not when it carries a room or a negation: "I didn't see you in cafe, where were you?" is Not_Seen_In_Room)
            if (ent.Players.Count == 1 && !ent.PlayerAmbiguous && ent.Players[0] != senderId && ent.Room == null && !negated
                && (abbrev || (asksWhere && youWord)))
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

            // 3) "body in electrical" / "found in elec" ("body in elec?" is a question: templates decide, see below)
            if (ent.Room is { } bodyRoom && ent.Players.Count == 0 && hasBody && !hasQuestionMark)
            {
                // same room as the one already said = agreeing with it
                var said = ClaimStore.Latest("Body_Location", ChatIntentEnum.Report);
                if (said != null && said.Age <= BodyReplyWindow && said.Intent.Room == bodyRoom && said.ClaimerId != senderId)
                {
                    return Ok(new ChatIntent { Type = ChatIntentEnum.Confirm, PhraseId = "Body_Location", Context = context, Room = bodyRoom },
                        0.85f, "agrees with the body location already said");
                }

                return Ok(new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = bodyRoom },
                    0.85f, "body location statement");
            }

            // 4) bare room: "elec", "upper", "it was in nav" -> an answer to whatever was just asked
            bool onlyFillers = OnlyFillers(tokens, ent.Consumed, -1, selfRef);

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

                var already = ClaimStore.Latest("Body_Location", ChatIntentEnum.Report);

                // the room was already said: a bare room now agrees with it or disputes it
                if (already != null && already.Age <= BodyReplyWindow && already.ClaimerId != senderId && already.Intent.Room != null)
                {
                    bool same = already.Intent.Room == room;
                    return Ok(new ChatIntent
                    {
                        Type = same ? ChatIntentEnum.Confirm : ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = room,
                    }, 0.7f - hedge, same ? "bare room agrees with the body location said" : "bare room differs from the body location said");
                }

                // nobody said where the body is: early in the meeting (the reporter), or later if they say "it was in X"
                bool refersToIt = tokens.Contains("it");
                bool earlyInChat = ClaimStore.Elapsed <= BodyGuessWindow && ClaimStore.MessagesBefore <= BodyGuessMaxPriorMessages;
                if (already == null && (earlyInChat || refersToIt))
                {
                    return Ok(new ChatIntent { Type = ChatIntentEnum.Report, PhraseId = "Body_Location", Context = context, Room = room },
                        (earlyInChat ? 0.6f : 0.55f) - hedge, "guess: first room said = body location");
                }

                return Unknown("room only, nothing to attach it to");
            }

            // 5) anything else: look for the closest sentence in the PhraseBank itself
            if (groups != null)
            {
                var matched = MatchTemplates(tokens, ent, groups, context);
                if (matched != null) return matched;
            }

            if (ent.Room != null && hasBody && hasQuestionMark)
                return Unknown("body/room question with no template for it");

            return Unknown("no rule matched");
        }

        public static string MaskEntities(string text, IReadOnlyList<ChatPlayer> players, IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var tokens = ChatLexicon.Normalize(text).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (tokens.Count == 0) return "";

            tokens = SplitGlued(tokens, players);

            var ent = Extract(tokens, players, rooms);
            var parts = new List<string>(tokens.Count);
            for (int i = 0; i < tokens.Count; i++)
            {
                switch (ent.Kind[i])
                {
                    case 0: parts.Add(tokens[i]); break;
                    case 1: parts.Add("{room}"); break;
                    case 2: parts.Add("{player}"); break;
                }
            }

            return string.Join(' ', parts).Replace(" ?", "?");
        }

        private static Entities Extract(List<string> tokens, IReadOnlyList<ChatPlayer> players, IReadOnlyDictionary<SystemTypes, string> rooms)
        {
            var e = new Entities { Consumed = new bool[tokens.Count], Kind = new int[tokens.Count] };
            int n = tokens.Count;

            // rooms (strong aliases, longest first)
            var strong = BuildStrongRooms(rooms);
            for (int i = 0; i < n; i++)
            {
                if (i + 1 < n && strong.TryGetValue(tokens[i] + " " + tokens[i + 1], out var two))
                {
                    SetRoom(e, two);
                    e.Consumed[i] = e.Consumed[i + 1] = true;
                    e.Kind[i] = 1; e.Kind[i + 1] = 3;
                    i++;
                }
                else if (TryStrongRoom(strong, tokens[i], out var one))
                {
                    SetRoom(e, one);
                    e.Consumed[i] = true;
                    e.Kind[i] = 1;
                }
                else if (ChatLexicon.AmbiguousRoomWords.Contains(tokens[i]))
                {
                    e.RoomAmbiguous = true;
                    e.Consumed[i] = true;
                    e.Kind[i] = 1;
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
                    e.Kind[i] = 2; e.Kind[i + 1] = 3;
                    i++;
                }
                else if (keys.TryGetValue(tokens[i], out var ids1))
                {
                    AddPlayers(e, ids1);
                    e.Consumed[i] = true;
                    e.Kind[i] = 2;
                }
                else if (TryFuzzy(tokens[i], keys, out var fuzzy))
                {
                    AddPlayers(e, fuzzy);
                    e.Consumed[i] = true;
                    e.Kind[i] = 2;
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
                    e.Kind[i] = 1;
                    break;
                }
            }

            return e;
        }

        // "storages", "meds", "cams": the plural of an alias is the same room
        private static bool TryStrongRoom(Dictionary<string, SystemTypes> strong, string token, out SystemTypes room) =>
            strong.TryGetValue(token, out room)
            || (token.Length >= 4 && token.EndsWith('s') && strong.TryGetValue(token[..^1], out room));

        // "maroonwya" -> "maroon wya" (a player and a keyword typed without the space).
        private static List<string> SplitGlued(List<string> tokens, IReadOnlyList<ChatPlayer> players)
        {
            List<string> result = null;
            HashSet<string> keys = null;

            for (int i = 0; i < tokens.Count; i++)
            {
                var t = tokens[i];
                if (t.Length >= 5 && (t.EndsWith("wya") || t.EndsWith("wru")))
                {
                    keys ??= [.. BuildPlayerKeys(players).Keys];
                    var prefix = t[..^3];
                    if (keys.Contains(prefix))
                    {
                        result ??= [.. tokens.Take(i)];
                        result.Add(prefix);
                        result.Add(t[^3..]);
                        continue;
                    }
                }
                result?.Add(t);
            }

            return result ?? tokens;
        }

        private static void SetRoom(Entities e, SystemTypes room)
        {
            if (e.Room == null) e.Room = room;
            else if (e.Room != room) { e.Room = null; e.RoomAmbiguous = true; }
        }

        private static void AddPlayers(Entities e, List<byte> ids)
        {
            if (ids.Count != 1) { e.PlayerAmbiguous = true; e.PlayerSlots.Add(null); return; }
            if (e.Players.Contains(ids[0])) return; // the same player named twice is still one slot
            e.Players.Add(ids[0]);
            e.PlayerSlots.Add(ids[0]);
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

            // "red's wires" is normalised to "reds wires"
            void AddPossessive(string word, byte id)
            {
                var poss = word + "s";
                if (word.Length >= 3 && !ChatLexicon.PossessiveBlocklist.Contains(poss) && !IsKnownWord(poss)) Add(poss, id);
            }

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
                        if (!ChatLexicon.StopNames.Contains(word)) { Add(word, p.Id); AddPossessive(word, p.Id); }
                }

                var color = ChatLexicon.Normalize(p.Color);
                if (color.Length > 0)
                {
                    Add(color, p.Id);
                    if (!color.Contains(' ')) AddPossessive(color, p.Id);
                    if (ChatLexicon.ColorNicknames.TryGetValue(color, out var nicks))
                        foreach (var nick in nicks) { Add(nick, p.Id); AddPossessive(nick, p.Id); }
                }

                var shown = ChatLexicon.Normalize(p.ColorAlt);   // the colour as the (maybe translated) game shows it
                if (shown.Length > 0 && shown != color) { Add(shown, p.Id); if (!shown.Contains(' ')) AddPossessive(shown, p.Id); }
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

        private static bool OnlyFillers(List<string> tokens, bool[] consumed, int except, bool selfTalk = false)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i == except || consumed[i]) continue;
                var t = tokens[i];
                if (t == "?" || ChatLexicon.Fillers.Contains(t) || ChatLexicon.SelfWords.Contains(t)) continue;
                if (selfTalk && ChatLexicon.StateWords.Contains(t)) continue;
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
