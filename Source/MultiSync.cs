using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    /// <summary>
    /// This class is used to approximate the behavior of the multiplayer ItemSync mod,
    /// which allows players to synchronize their game states, sharing things like pickups
    /// to make progress in parallel.  This allows approximation of that behavior for
    /// two TAS movies nominally running in parallel.
    /// </summary>
    internal static class MultiSync {
        private const string SyncFile = "./Playback/MultiSync.txt";
        private const string RecordingFile = "./Recording/MultiSync.txt";

        private static DateTime _lastFileWriteTime;
        private static Queue<SyncEntry> _syncEntries = new();
        private static Dictionary<string, SyncType> _syncTypeMap = new();
        private static List<string> _syncRecord;
        private static bool _applyingEntry;
        private static bool _initialized;

        public static void Init() {
            foreach (var value in Enum.GetValues(typeof(SyncType))) {
                _syncTypeMap.Add(value.ToString().ToLower(), (SyncType)value);
            }
        }

        private static void OnSetPlayerDataBool(PlayerData self, string name, bool state) {
            if (_applyingEntry)
                return;

            if (self.GetBool(name) != state) {
                if (name == "disablePause")
                    return;

                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{(state ? "1" : "0")}");
            }
        }

        private static void OnSetPlayerDataInt(PlayerData self, string name, int value) {
            if (_applyingEntry)
                return;

            if (self.GetInt(name) != value) {
                if (name == "previousDarkness")
                    return;

                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{value}");
            }
        }

        private static void OnSetPlayerDataFloat(PlayerData self, string name, float value) {
            if (_applyingEntry)
                return;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (self.GetFloat(name) != value) {
                if (name.StartsWith("gMap_"))
                    return;

                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{value}");
            }
        }

        private static void OnPlayerDataIntAdd(PlayerData self, string name, int amount) {
            if (_applyingEntry)
                return;

            var value = self.GetInt(name) + amount;
            _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{value}");
        }

        private static void OnPlayerDataDecrementInt(PlayerData self, string name) {
            if (_applyingEntry)
                return;

            var value = self.GetInt(name) - 1;
            _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{value}");
        }

        private static void OnPlayerDataIncrementInt(PlayerData self, string name) {
            if (_applyingEntry)
                return;

            var value = self.GetInt(name) + 1;
            _syncRecord.Add($"{TimeStr(Time.unscaledTime)},PlayerData.{name},{value}");
        }

        private static void OnPlayerDataAddGeo(PlayerData self, int amount) {
            if (_applyingEntry)
                return;

            _syncRecord.Add($"{TimeStr(Time.unscaledTime)},Geo,{amount}");
        }

        private static void OnPlayerDataTakeGeo(PlayerData self, int amount) {
            if (_applyingEntry)
                return;

            _syncRecord.Add($"{TimeStr(Time.unscaledTime)},Geo,{-1 * amount}");
        }

        private static void OnSceneDataSaveMyState(SceneData self, PersistentBoolData data) {
            if (_applyingEntry || data.semiPersistent)
                return;

            var priorState = self.FindMyState(data);
            if (priorState == null || priorState.activated != data.activated) {
                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},SceneData.{data.sceneName}:bool[{data.id}],{(data.activated ? "1" : "0")}");
            }
        }

        private static void OnSceneDataSaveMyState(SceneData self, PersistentIntData data) {
            if (_applyingEntry || data.semiPersistent)
                return;

            var priorState = self.FindMyState(data);
            if (priorState == null || priorState.value != data.value) {
                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},SceneData.{data.sceneName}:int[{data.id}],{(data.value)}");
            }
        }

        private static void OnSceneDataSaveMyState(SceneData self, GeoRockData data) {
            if (_applyingEntry)
                return;

            var priorState = self.FindMyState(data);
            if (priorState == null || priorState.hitsLeft != data.hitsLeft) {
                _syncRecord.Add($"{TimeStr(Time.unscaledTime)},SceneData.{data.sceneName}:geo[{data.id}],{(data.hitsLeft)}");
            }
        }

        private static string TimeStr(float time) {
            var span = TimeSpan.FromSeconds(time);
            var builder = new StringBuilder();
            if (span.Hours > 0) {
                builder.Append($"{span.Hours:00}:");
            }

            if (span.Minutes > 0) {
                builder.Append($"{span.Minutes:00}:");
            }

            builder.Append($"{span.Seconds:00}.{span.Milliseconds:000}");
            return builder.ToString();
        }

        public static void WriteSyncRecording() {
            if (_syncRecord == null) return;

            if (!Directory.Exists("./Recording"))
                Directory.CreateDirectory("./Recording");

            using (var stream = File.Open(RecordingFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (var writer = new StreamWriter(stream)) {
                foreach (var line in _syncRecord) {
                    writer.WriteLine(line);
                }
            }
        }

        public static void OnPreRender() {
            if (!_initialized) {
                _initialized = true;
                if (ConfigManager.RecordMultiSync) {
                    _syncRecord = new List<string>();

                    //PlayerData value mutators (we're ignoring String and Vector)
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string, bool>>(nameof(PlayerData.SetBool), OnSetPlayerDataBool);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string, int>>(nameof(PlayerData.SetInt), OnSetPlayerDataInt);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string, float>>(nameof(PlayerData.SetFloat), OnSetPlayerDataFloat);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string>>(nameof(PlayerData.IncrementInt), OnPlayerDataIncrementInt);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string>>(nameof(PlayerData.DecrementInt), OnPlayerDataDecrementInt);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, string, int>>(nameof(PlayerData.IntAdd), OnPlayerDataIntAdd);

                    //PlayerData geo mutators
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, int>>(nameof(PlayerData.AddGeo), OnPlayerDataAddGeo);
                    HookUtils.HookEnter<PlayerData, Action<PlayerData, int>>(nameof(PlayerData.TakeGeo), OnPlayerDataTakeGeo);

                    //SceneData mutators
                    HookUtils.HookEnter<SceneData, Action<SceneData, PersistentBoolData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);
                    HookUtils.HookEnter<SceneData, Action<SceneData, PersistentIntData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);
                    HookUtils.HookEnter<SceneData, Action<SceneData, GeoRockData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);
                }

                var method = typeof(DeactivateInDarknessWithoutLantern).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
                HookEndpointManager.Modify(method, (Action<ILContext>)DeactivateInDarknessWithoutLanternStart);
            }
            TryParseSyncFile();
            ApplyEntries();
        }

        private static void DeactivateInDarknessWithoutLanternStart(ILContext il) {
            var c = new ILCursor(il);
            c.EmitDelegate((Func<bool>)GetFakeNoLantern);
            var skipRet = c.DefineLabel();
            c.Emit(OpCodes.Brfalse, skipRet);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(skipRet);
        }

        private static bool GetFakeNoLantern() => ConfigManager.FakeNoLantern;

        private static void ApplyEntries() {
            while (_syncEntries.Count > 0 && _syncEntries.Peek().Time <= Time.unscaledTime) {
                var entry = _syncEntries.Dequeue();
                ApplyEntry(entry);
            }
        }

        private static void ApplyEntry(SyncEntry entry) {
            _applyingEntry = true;
            switch (entry.SyncType) {
                case SyncType.Geo:
                    ApplyGeo(entry);
                    break;
                case SyncType.PlayerData:
                    ApplyPlayerData(entry);
                    break;
                case SyncType.SceneData:
                    ApplySceneData(entry);
                    break;
            }

            _applyingEntry = false;
        }

        private static void ApplyGeo(SyncEntry entry) {
            var hero = GameManager.instance.hero_ctrl;
            if (hero == null) {
                if (entry.Value > 0) {
                    PlayerData.instance.AddGeo((int)entry.Value);
                } else if (entry.Value < 0) {
                    PlayerData.instance.TakeGeo((int)(-1 * entry.Value));
                }
            } else {
                if (entry.Value > 0) {
                    hero.AddGeo((int)entry.Value);
                } else if (entry.Value < 0) {
                    //hero.TakeGeo((int)(-1 * entry.Value));
                    PlayerData.instance.TakeGeo((int)(-1 * entry.Value));
                    hero.geoCounter.NewSceneRefresh();
                }
            }
        }

        private static void ApplyPlayerData(SyncEntry entry) {
            var field = typeof(PlayerData).GetField(entry.Tag, BindingFlags.Instance | BindingFlags.Public);
            if (field == null)
                return;

            if (field.FieldType == typeof(bool)) {
                PlayerData.instance.SetBool(entry.Tag, entry.Value != 0);
            } else if (field.FieldType == typeof(float)) {
                PlayerData.instance.SetFloat(entry.Tag, entry.Value);
            } else if (field.FieldType == typeof(int)) {
                PlayerData.instance.SetInt(entry.Tag, (int)entry.Value);
                if (entry.Tag == "nailDamage") {
                    PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
                }
            }
        }

        private static void ApplySceneData(SyncEntry entry) {
            var colonIndex = entry.Tag.IndexOf(':');
            var bracketIndex = entry.Tag.IndexOf('[');
            if (colonIndex == -1 || bracketIndex == -1)
                return;

            var sceneName = entry.Tag.Substring(0, colonIndex);
            var persistentType = entry.Tag.Substring(colonIndex + 1, bracketIndex - colonIndex - 1).ToLower();
            var id = entry.Tag.Substring(bracketIndex + 1, entry.Tag.Length - bracketIndex - 2);
            Debug.Log($"Updating SceneData type {persistentType} for scene '{sceneName}', id '{id}', value '{entry.Value}'");

            if (persistentType == "geo") {
                var geo = new GeoRockData();
                geo.id = id;
                geo.sceneName = sceneName;
                geo.hitsLeft = (int)entry.Value;
                SceneData.instance.SaveMyState(geo);
            } else if (persistentType == "bool") {
                var data = new PersistentBoolData();
                data.id = id;
                data.sceneName = sceneName;
                data.activated = entry.Value != 0;
                SceneData.instance.SaveMyState(data);
            } else if (persistentType == "int") {
                var data = new PersistentIntData();
                data.id = id;
                data.sceneName = sceneName;
                data.value = (int)entry.Value;
                SceneData.instance.SaveMyState(data);
            }
        }

        private static void TryParseSyncFile() {
            if (!File.Exists(SyncFile)) {
                //If no sync file, we're not doing syncing
                return;
            }

            var writeTime = File.GetLastWriteTime(SyncFile);
            if (writeTime == _lastFileWriteTime) {
                //If the file hasn't changed, don't bother re-parsing it
                return;
            }

            try {
                _lastFileWriteTime = writeTime;
                var currentTime = Time.unscaledTime;
                using (var stream = File.Open(SyncFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var reader = new StreamReader(stream)) {
                    var syncEntries = new Queue<SyncEntry>();
                    while (!reader.EndOfStream) {
                        if (TryParseEntry(reader.ReadLine(), out var entry)) {
                            //Only include entries that are in the future
                            if (entry.Time >= currentTime) {
                                syncEntries.Enqueue(entry);
                            }
                        }
                    }

                    _syncEntries = syncEntries;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        private static bool TryParseEntry(string line, out SyncEntry entry) {
            entry = default;

            //Entries are comma-separated values, with optional spaces that are ignored
            //Entries starting with '//' are comments
            if (string.IsNullOrEmpty(line) || line.StartsWith("//")) {
                return false;
            }

            string[] parts = line.Split(',');
            if (parts.Length != 3) {
                return false;
            }

            var success = TryParseTime(parts[0].Trim(), out float time);
            int dotIndex = parts[1].IndexOf('.');
            var syncTypeText = dotIndex > -1 ? parts[1].Substring(0, dotIndex) : parts[1];
            success &= TryParseSyncType(syncTypeText, out SyncType syncType);

            var tag = dotIndex > -1 ? parts[1].Substring(dotIndex + 1) : "";
            success &= float.TryParse(parts[2].Trim().TrimStart('+'), out var value);

            entry = new SyncEntry(time, syncType, tag, value);
            return success;
        }

        private static bool TryParseTime(string timeText, out float time) {
            time = 0f;
            var split = timeText.Split(':');
            if (split.Length == 3) {
                if (!int.TryParse(split[0].TrimStart('0'), out var hours))
                    return false;
                time += hours * 3600f;
            }

            if (split.Length == 3 || split.Length == 2) {
                if (!int.TryParse(split[split.Length - 2].TrimStart('0'), out var minutes))
                    return false;
                time += minutes * 60f;
            }

            if (!float.TryParse(split[split.Length - 1].TrimStart('0'), out var seconds))
                return false;

            time += seconds;
            return true;
        }

        private static bool TryParseSyncType(string syncTypeText, out SyncType syncType) {
            return _syncTypeMap.TryGetValue(syncTypeText.ToLower(), out syncType);
        }

        private enum SyncType {
            None,
            Geo,
            PlayerData,
            SceneData
        }

        private sealed class SyncEntry {
            public SyncEntry(float time, SyncType syncType, string tag, float value) {
                Time = time;
                SyncType = syncType;
                Tag = tag;
                Value = value;
            }

            public float Time { get; }

            public SyncType SyncType { get; }

            public string Tag { get; }

            public float Value { get; }
        }
    }
}
