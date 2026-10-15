using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMG.AI.Services.ChatService.History;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService.Corpus
{
    // Passive collector of what real players say. It only OBSERVES: nothing here sends chat, touches the bank or
    // influences an agent. Output (Among Us_Data/AMG/Corpus/)
    public static class ChatCorpus
    {
        private const float UnderstoodMin = 0.6f;
        private const int PrevKept = 3;

        private static readonly string Session = Guid.NewGuid().ToString("N")[..6];
        private static readonly object FileLock = new();

        private static bool _enabled = true;
        private static int _meeting;
        private static int _seq;

        // Keep the original text of every kept message in ChatRaw.jsonl (LOCAL ONLY, never share it). It lets old
        // messages be re-masked/re-parsed when the parser improves. Dropped messages are never stored, not even here.
        public const bool StoreRaw = true;
        private const int RawMaxChars = 300;
        private static float _meetingStartedAt;
        private static readonly Dictionary<byte, string> _speakers = [];
        private static readonly List<CorpusPrev> _prev = [];
        private static readonly List<(string type, float time)> _tape = [];
        private static ChatSafetyFilter _filter;

        private static string CorpusFile => ModPaths.Data("Corpus", "ChatCorpus.jsonl");
        private static string UnknownFile => ModPaths.Data("Corpus", "UnknownMessages.jsonl");
        private static string BlocklistFile => ModPaths.Data("Corpus", "ChatBlocklist.txt");
        private static string RawFile => ModPaths.Data("Corpus", "ChatRaw.jsonl");
        private static string ReadmeFile => ModPaths.Data("Corpus", "README.txt");
        private static bool _readmeWritten;

        public static bool Enabled => _enabled;

        public static void Toggle()
        {
            _enabled = !_enabled;
            LogManager.LogDebug($"[Corpus] capture {(_enabled ? "ON" : "OFF")}");
        }

        public static void Mark(string type)
        {
            _tape.Add((type, Time.time));
            LogManager.LogDebug($"[Corpus] marked '{type}' ({_tape.Count} event(s) this round)");
        }

        public static void OnMeetingStart()
        {
            _meeting++;
            _meetingStartedAt = Time.time;
            _speakers.Clear();
            _prev.Clear();
            _seq = 0;
            EnsureReadme();

            var header = new CorpusMeeting
            {
                Session = Session,
                Meeting = _meeting,
                Utc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Alive = Utils.Players.AllPlayers.Count(p => p != null && p.Data != null && !p.Data.IsDead),
                Events = [.. _tape.Select(e => new CorpusEvent { Type = e.type, SecBefore = Round1(Time.time - e.time) })],
            };
            _tape.Clear();

            if (_enabled) Append(CorpusFile, CorpusJson.Line(header));

            // who is in the match (names stay in the local file only)
            var roster = ChatParser.RememberPlayers();
            if (_enabled && StoreRaw)
            {
                Append(RawFile, CorpusJson.Line(new CorpusRoster
                {
                    Session = Session, Meeting = _meeting,
                    Players = [.. roster.Select(p => new CorpusRosterPlayer { Id = p.Id, Name = p.Name, Color = p.Color })],
                }));
            }
        }

        public static string Record(PlayerControl sender, string raw, MessageParsedData parsed)
        {
            string speaker = Speaker(sender);
            string id = $"{Session}-{_meeting}-{++_seq}";
            var verdict = Filter.Check(raw);
            bool contact = ChatRedactor.HasContactInfo(raw);

            if (contact || verdict.Drop)
            {
                string category = contact ? "contact" : verdict.Category;

                // only the fact that something was dropped is kept, never its text
                if (_enabled)
                {
                    Append(CorpusFile, CorpusJson.Line(new CorpusMessage
                    {
                        Id = id, Session = Session, Meeting = _meeting, At = Round1(Time.time - _meetingStartedAt),
                        Speaker = speaker, Bucket = "filtered", Category = category,
                    }));
                }

                return $"[dropped:{category}]";
            }

            var players = ChatParser.SnapshotPlayers(out var rooms);
            string masked = ChatParser.MaskEntities(raw, players, rooms);
            if (masked.Length == 0) return "";

            if (_enabled && StoreRaw)
            {
                string kept = raw.Length > RawMaxChars ? raw[..RawMaxChars] : raw;
                Append(RawFile, CorpusJson.Line(new CorpusRaw { Id = id, Raw = kept }));
            }

            bool understood = parsed.Intent != null && parsed.Confidence >= UnderstoodMin;
            string bucket = parsed.Intent == null ? "unknown" : understood ? "understood" : "low";

            var open = ClaimStore.OpenQuestionPhraseIds();
            var record = new CorpusMessage
            {
                Id = id,
                Session = Session,
                Meeting = _meeting,
                At = Round1(Time.time - _meetingStartedAt),
                Speaker = speaker,
                Text = masked,
                Bucket = bucket,
                Flag = verdict.Category,
                Reason = parsed.Rationale,
                Parse = parsed.Intent == null ? null : new CorpusParse
                {
                    Id = parsed.Intent.PhraseId,
                    Intent = parsed.Intent.Type.ToString(),
                    Room = parsed.Intent.Room?.ToString(),
                    Confidence = (float)Math.Round(parsed.Confidence, 2),
                },
                Prev = _prev.Count > 0 ? [.. _prev] : null,
                Open = open.Count > 0 ? open : null,
            };

            if (_enabled) Append(understood ? CorpusFile : UnknownFile, CorpusJson.Line(record));

            _prev.Add(new CorpusPrev { Speaker = speaker, Text = masked });
            if (_prev.Count > PrevKept) _prev.RemoveAt(0);

            return masked;
        }

        private static string Speaker(PlayerControl sender)
        {
            if (sender == PlayerControl.LocalPlayer) return "me";
            if (!_speakers.TryGetValue(sender.PlayerId, out var name)) _speakers[sender.PlayerId] = name = "S" + (_speakers.Count + 1);
            return name;
        }

        private static ChatSafetyFilter Filter => _filter ??= LoadFilter();

        private static ChatSafetyFilter LoadFilter()
        {
            var lines = new List<string>(ChatSafetyFilter.DefaultFile.Split('\n'));

            try
            {
                var path = BlocklistFile;
                if (!File.Exists(path)) File.WriteAllText(path, ChatSafetyFilter.DefaultFile);
                else lines.AddRange(File.ReadAllLines(path));
            }
            catch (Exception e)
            {
                LogManager.LogError($"[Corpus] could not read the blocklist, using the built-in one: {e.Message}");
            }

            return ChatSafetyFilter.Parse(lines);
        }

        private static void Append(string path, string line)
        {
            try
            {
                lock (FileLock) File.AppendAllText(path, line + "\n");
            }
            catch (Exception e)
            {
                LogManager.LogError($"[Corpus] could not write {path}: {e.Message}");
            }
        }

        private static void EnsureReadme()
        {
            if (_readmeWritten) return;
            _readmeWritten = true;

            try
            {
                if (File.Exists(ReadmeFile)) return;
                File.WriteAllText(ReadmeFile,
@"AMG chat corpus

UnknownMessages.jsonl  messages the parser did not understand: names masked as {player}, no original text. Safe to share.
ChatCorpus.jsonl       understood messages, one header per meeting, counts of dropped messages. Safe to share.
ChatRaw.jsonl          the ORIGINAL text of every kept message + the player list of each meeting (real names).
                       LOCAL ONLY: never upload or share this file. It is joined to the others by ""id"".
ChatBlocklist.txt      words that make a message be dropped (never stored anywhere, not even in ChatRaw.jsonl).
");
            }
            catch (Exception e)
            {
                LogManager.LogError($"[Corpus] could not write the README: {e.Message}");
            }
        }

        private static float Round1(float v) => (float)Math.Round(v, 1);
    }
}
