using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    public static class PhraseBank
    {
        /// <summary>Every group created in-game (not present in the default file) must use this prefix,
        /// so it can never collide with an Id the mod ships later.</summary>
        public const string VividPrefix = "Vivid_";

        private static readonly string _folder = Path.Combine(Application.dataPath, "AMG");
        private static readonly string _defaultPath = Path.Combine(_folder, "DefaultPhraseBank.json");
        private static readonly string _vividPath = Path.Combine(_folder, "VividPhraseBank.json");

        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly object _lock = new();
        private static PhraseBankData? _default;
        private static PhraseBankData? _vivid;
        private static bool _vividReadOnly;          // vivid file is from a newer schema: never overwrite it
        private static volatile PhraseIndex? _index;


        public static PhraseIndex Index
        {
            get
            {
                var idx = _index;
                if (idx != null) return idx;
                lock (_lock)
                {
                    EnsureLoaded();
                    return _index!;
                }
            }
        }

        public static void Reload()
        {
            lock (_lock)
            {
                _default = LoadDefault();
                _vivid = LoadVivid();
                _index = Merge(_default, _vivid);
            }
        }

        public static void UpdateVivid(Action<PhraseBankData> edit)
        {
            lock (_lock)
            {
                EnsureLoaded();
                edit(_vivid!);
                WriteVivid(_vivid!);
                _index = Merge(_default!, _vivid!);
            }
        }

        public static void MergeInto(PhraseBankData target, PhraseBankData source, PhraseSourceEnum defaultSource)
        {
            foreach (var langEntry in source.Languages)
            {
                if (!target.Languages.TryGetValue(langEntry.Key, out var targetPack))
                {
                    targetPack = new LanguagePack();
                    target.Languages[langEntry.Key] = targetPack;
                }

                foreach (var room in langEntry.Value.Rooms)
                    targetPack.Rooms.TryAdd(room.Key, room.Value);

                foreach (var sourceGroup in langEntry.Value.Phrases)
                {
                    var origin = sourceGroup.Source ?? defaultSource;
                    var existing = targetPack.Phrases.Find(g => SameKey(g, sourceGroup));

                    if (existing == null)
                    {
                        if (defaultSource != PhraseSourceEnum.Base && !sourceGroup.Id.StartsWith(VividPrefix, StringComparison.Ordinal))
                            LogManager.LogWarning($"[PhraseBank] New group '{sourceGroup.Id}' should use the '{VividPrefix}' prefix.");

                        targetPack.Phrases.Add(CloneGroup(sourceGroup, origin));
                        continue;
                    }

                    foreach (var variant in sourceGroup.Phrases)
                    {
                        if (existing.Phrases.Exists(e => string.Equals(e.Text, variant.Text, StringComparison.OrdinalIgnoreCase)))
                            continue;
                        existing.Phrases.Add(CloneVariant(variant, origin));
                    }
                }
            }
        }

        public static string NewVividId(string name) =>
            name.StartsWith(VividPrefix, StringComparison.Ordinal) ? name : VividPrefix + name;



        private static void EnsureLoaded()
        {
            if (_index != null) return;
            _default = LoadDefault();
            _vivid = LoadVivid();
            _index = Merge(_default, _vivid);
        }

        private static PhraseBankData LoadDefault()
        {
            if (!File.Exists(_defaultPath))
            {
                LogManager.LogError($"[PhraseBank] Missing default bank: {_defaultPath}");
                return new PhraseBankData();
            }

            try
            {
                return Parse(File.ReadAllText(_defaultPath));
            }
            catch (Exception e)
            {
                LogManager.LogError($"[PhraseBank] Invalid default bank ({_defaultPath}): {e.Message}");
                return new PhraseBankData();
            }
        }

        private static PhraseBankData LoadVivid()
        {
            _vividReadOnly = false;

            if (!File.Exists(_vividPath))
            {
                var empty = new PhraseBankData();
                WriteVivid(empty);
                return empty;
            }

            try
            {
                var data = Parse(File.ReadAllText(_vividPath));

                if (data.SchemaVersion > PhraseBankData.CurrentSchemaVersion)
                {
                    // File written by a newer mod version: use it, but never overwrite what we don't understand.
                    _vividReadOnly = true;
                    LogManager.LogWarning($"[PhraseBank] {_vividPath} has schema v{data.SchemaVersion} (this mod supports v{PhraseBankData.CurrentSchemaVersion}). Loaded read-only.");
                }
                else if (data.SchemaVersion < PhraseBankData.CurrentSchemaVersion)
                {
                    File.Copy(_vividPath, _vividPath + $".v{data.SchemaVersion}.bak", true);
                    data.SchemaVersion = PhraseBankData.CurrentSchemaVersion;
                    WriteVivid(data);
                }

                return data;
            }
            catch (Exception e)
            {
                LogManager.LogError($"[PhraseBank] Invalid vivid bank, starting empty: {e.Message}");
                try { File.Move(_vividPath, _vividPath + $".corrupt-{DateTime.UtcNow.Ticks}"); }
                catch (Exception moveError) { LogManager.LogError($"[PhraseBank] Could not back up the corrupt file: {moveError.Message}"); }
                _vividReadOnly = false;
                return new PhraseBankData();
            }
        }

        private static PhraseBankData Parse(string json) =>
            JsonSerializer.Deserialize<PhraseBankData>(json, Options) ?? new PhraseBankData();

        private static void WriteVivid(PhraseBankData data)
        {
            if (_vividReadOnly) return;

            try
            {
                Directory.CreateDirectory(_folder);
                var tmp = _vividPath + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(data, Options));
                File.Move(tmp, _vividPath, true);
            }
            catch (Exception e)
            {
                LogManager.LogError($"[PhraseBank] Could not save {_vividPath}: {e.Message}");
            }
        }

        private static PhraseIndex Merge(PhraseBankData defaults, PhraseBankData vivid)
        {
            var merged = new PhraseBankData();
            MergeInto(merged, defaults, PhraseSourceEnum.Base);
            MergeInto(merged, vivid, PhraseSourceEnum.User);

            var languages = new Dictionary<string, LanguageIndex>();
            foreach (var entry in merged.Languages)
                languages[entry.Key] = new LanguageIndex(entry.Value.Rooms, entry.Value.Phrases);

            return new PhraseIndex(languages);
        }

        private static bool SameKey(PhraseGroup a, PhraseGroup b) =>
            string.Equals(a.Id, b.Id, StringComparison.Ordinal) && a.Intent == b.Intent && a.Context == b.Context;

        private static PhraseGroup CloneGroup(PhraseGroup g, PhraseSourceEnum origin)
        {
            var clone = new PhraseGroup
            {
                Id = g.Id,
                Intent = g.Intent,
                Context = g.Context,
                Weight = g.Weight,
                ReplyTo = g.ReplyTo,
                Evidence = g.Evidence,
                Tags = g.Tags == null ? null : [.. g.Tags],
                Source = origin
            };
            foreach (var v in g.Phrases) clone.Phrases.Add(CloneVariant(v, origin));
            return clone;
        }

        private static PhraseVariant CloneVariant(PhraseVariant v, PhraseSourceEnum origin) => new()
        {
            Tone = [.. v.Tone],
            Weight = v.Weight,
            Text = v.Text,
            MinConfidence = v.MinConfidence,
            MaxConfidence = v.MaxConfidence,
            Source = v.Source ?? origin
        };
    }
}