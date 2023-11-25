using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
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
    /// multiple TAS movies nominally running in parallel.
    /// </summary>
    internal static class MultiSync {
        private const string SyncFile = "./Playback/MultiSync.txt";

        private static HashSet<string> _filteredPlayerDataBools = new();
        private static HashSet<string> _filteredPlayerDataInts = new();
        private static HashSet<string> _filteredPlayerDataFloats = new();

        private static ListQueue<SyncEntry> _syncEntries;
        private static Dictionary<string, SyncType> _syncTypeMap = new();
        private static List<SyncEntry> _syncRecord;
        private static bool _applyingEntry;
        private static bool _initialized;

        private static SaveAndQuitState _saveAndQuitState;
        private static int _lastPersistedInt;
        private static bool _lastPersistedBool;
        private static int _lastPersistedGeoRock;
        

        public static void Init() {
            _filteredPlayerDataBools.Add("disablePause");
            _filteredPlayerDataBools.Add("atBench");
            _filteredPlayerDataBools.Add("respawnFacingRight");
            _filteredPlayerDataBools.Add("soulLimited");
            _filteredPlayerDataBools.Add("canOvercharm");
            _filteredPlayerDataBools.Add("overcharmed");
            _filteredPlayerDataBools.Add("isInvincible");
            _filteredPlayerDataBools.Add("metStag");
            _filteredPlayerDataBools.Add("traveling");

            _filteredPlayerDataInts.Add("previousDarkness");
            _filteredPlayerDataInts.Add("currentArea");
            _filteredPlayerDataInts.Add("");
            _filteredPlayerDataInts.Add("currentInvPane");
            _filteredPlayerDataInts.Add("charmSlotsFilled");
            _filteredPlayerDataInts.Add("geoPool");
            _filteredPlayerDataInts.Add("shadeHealth");
            _filteredPlayerDataInts.Add("shadeMP");
            _filteredPlayerDataInts.Add("MPCharge");
            _filteredPlayerDataInts.Add("environmentType");
            _filteredPlayerDataInts.Add("stagPosition");
            _filteredPlayerDataInts.Add("respawnType");
            for (int i = 1; i <= 40; i++) {
                _filteredPlayerDataInts.Add($"newCharm_{i}");
                _filteredPlayerDataInts.Add($"equippedCharm_{i}");
            }

            _filteredPlayerDataFloats.Add("gMap_doorOriginOffsetX");
            _filteredPlayerDataFloats.Add("gMap_doorOriginOffsetY");
            _filteredPlayerDataFloats.Add("gMap_doorSceneWidth");
            _filteredPlayerDataFloats.Add("gMap_doorSceneHeight");
            _filteredPlayerDataFloats.Add("gMap_doorX");
            _filteredPlayerDataFloats.Add("gMap_doorY");
            _filteredPlayerDataFloats.Add("shadePositionX");
            _filteredPlayerDataFloats.Add("shadePositionY");

            foreach (var value in Enum.GetValues(typeof(SyncType))) {
                _syncTypeMap.Add(value.ToString().ToLower(), (SyncType)value);
            }
        }

        private static void OnSetPlayerDataBool(PlayerData self, string name, bool state) {
            if (_applyingEntry)
                return;

            if (self.GetBool(name) != state) {
                if (_filteredPlayerDataBools.Contains(name))
                    return;

                _syncRecord.Add(SyncEntry.PlayerData(name, state ? 1 : 0, Operation.Set));
            }
        }

        private static void OnSetPlayerDataInt(PlayerData self, string name, int value) {
            if (_applyingEntry)
                return;

            if (self.GetInt(name) != value) {
                if (_filteredPlayerDataInts.Contains(name))
                    return;

                _syncRecord.Add(SyncEntry.PlayerData(name, value, Operation.Set));
            }
        }

        private static void OnSetPlayerDataFloat(PlayerData self, string name, float value) {
            if (_applyingEntry)
                return;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (self.GetFloat(name) != value) {
                if (_filteredPlayerDataFloats.Contains(name))
                    return;

                _syncRecord.Add(SyncEntry.PlayerData(name, value, Operation.Set));
            }
        }

        private static void OnPlayerDataIntAdd(PlayerData self, string name, int amount) {
            if (_applyingEntry)
                return;

            if (_filteredPlayerDataInts.Contains(name))
                return;

            _syncRecord.Add(SyncEntry.PlayerData(name, amount, Operation.Add));
        }

        private static void OnPlayerDataDecrementInt(PlayerData self, string name) {
            if (_applyingEntry)
                return;

            if (_filteredPlayerDataInts.Contains(name))
                return;

            _syncRecord.Add(SyncEntry.PlayerData(name, 0, Operation.Decrement));
        }

        private static void OnPlayerDataIncrementInt(PlayerData self, string name) {
            if (_applyingEntry)
                return;

            if (_filteredPlayerDataInts.Contains(name))
                return;

            _syncRecord.Add(SyncEntry.PlayerData(name, 0, Operation.Increment));
        }

        private static void OnPlayerDataAddGeo(PlayerData self, int amount) {
            if (_applyingEntry)
                return;

            _syncRecord.Add(SyncEntry.Geo(amount));
        }

        private static void OnPlayerDataTakeGeo(PlayerData self, int amount) {
            if (_applyingEntry)
                return;

            _syncRecord.Add(SyncEntry.Geo(-1*amount));
        }

        //private static void OnSceneDataSaveMyState(SceneData self, PersistentBoolData data) {
        //    if (_applyingEntry || data.semiPersistent)
        //        return;

        //    _syncRecord.Add(SyncEntry.SceneData($"{data.sceneName}:bool[{data.id}]", data.activated ? 1 : 0));
        //}

        //private static void OnSceneDataSaveMyState(SceneData self, PersistentIntData data) {
        //    if (_applyingEntry || data.semiPersistent)
        //        return;

        //    _syncRecord.Add(SyncEntry.SceneData($"{data.sceneName}:int[{data.id}]", data.value));
        //}

        //private static void OnSceneDataSaveMyState(SceneData self, GeoRockData data) {
        //    if (_applyingEntry)
        //        return;

        //    _syncRecord.Add(SyncEntry.SceneData($"{data.sceneName}:geo[{data.id}]", data.hitsLeft));
        //}

        private static void ModifySaveStateInt(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));
            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<PersistentIntItem>)(p => {
                _lastPersistedInt = p.persistentIntData.value;
            }));

            c.GotoNext(i => i.Match(OpCodes.Ret));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<PersistentIntItem>)(p => {
                int newValue = p.persistentIntData.value;
                if (newValue != _lastPersistedInt && !p.semiPersistent && !_applyingEntry) {
                    _syncRecord.Add(SyncEntry.SceneData($"{p.persistentIntData.sceneName}:int[{p.persistentIntData.id}]", newValue));
                }
            }));
        }

        private static void ModifySaveStateBool(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));
            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<PersistentBoolItem>)(p => {
                _lastPersistedBool = p.persistentBoolData.activated;
            }));

            c.GotoNext(i => i.Match(OpCodes.Ret));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<PersistentBoolItem>)(p => {
                var newValue = p.persistentBoolData.activated;
                if (newValue != _lastPersistedBool && !p.semiPersistent && !_applyingEntry) {
                    _syncRecord.Add(SyncEntry.SceneData($"{p.persistentBoolData.sceneName}:bool[{p.persistentBoolData.id}]", newValue ? 1 : 0));
                }
            }));
        }

        private static void ModifySaveStateGeo(ILContext il) {
            var c = new ILCursor(il);

            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));
            c.GotoNext(i => i.Match(OpCodes.Ldarg_0));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<GeoRock>)(p => {
                _lastPersistedGeoRock = p.geoRockData.hitsLeft;
            }));

            c.GotoNext(i => i.Match(OpCodes.Ret));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Action<GeoRock>)(p => {
                int newValue = p.geoRockData.hitsLeft;
                if (newValue != _lastPersistedGeoRock && !_applyingEntry) {
                    _syncRecord.Add(SyncEntry.SceneData($"{p.geoRockData.sceneName}:int[{p.geoRockData.id}]", newValue));
                }
            }));
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

            bool consolidateGeo = ConfigManager.MultiSyncConsolidateGeo;
            const float geoWindow = 5;
            var filename = $"./Recording/MultiSync{ConfigManager.MultiSyncName}.txt";
            using (var stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            using (var writer = new StreamWriter(stream)) {

                bool geoConsolidating = false;
                float geoStartTime = 0;
                float geoAmount = 0;
                float lastTime = 0;
                foreach (var entry in _syncRecord) {
                    if (entry.SyncType == SyncType.Geo && consolidateGeo) {
                        if (!geoConsolidating) {
                            geoConsolidating = true;
                            geoStartTime = entry.Time;
                            geoAmount = entry.Value;
                        } else {
                            //We round geo consolidation to nearest second that exceeds the window
                            var endGeo = (int)Math.Ceiling(geoStartTime + geoWindow);
                            if (entry.Time - endGeo >= 0) {
                                writer.WriteLine($"{TimeStr(endGeo)},Geo,{geoAmount}");

                                //Start the next consolidation window
                                geoStartTime = entry.Time;
                                geoAmount = entry.Value;
                            } else {
                                //Add to the consolidation pool
                                geoAmount += entry.Value;
                            }
                        }
                    } else {
                        if (consolidateGeo) {
                            //We round geo consolidation to nearest second that exceeds the window
                            var endGeo = (int)Math.Ceiling(geoStartTime + geoWindow);
                            if (geoConsolidating && (entry.Time - endGeo >= 0)) {
                                writer.WriteLine($"{TimeStr(endGeo)},Geo,{geoAmount}");
                                geoAmount = 0;
                                geoConsolidating = false;
                            }
                        }
                        writer.WriteLine(entry.ToString());
                    }
                    lastTime = entry.Time;
                }

                //At the end, write out any remaining geo from consolidation
                if (geoConsolidating) {
                    writer.WriteLine($"{TimeStr(lastTime)},Geo,{geoAmount}");
                }
            }
        }

        public static void OnPreRender() {
            if (!_initialized) {
                _initialized = true;
                if (ConfigManager.RecordMultiSync) {
                    //Preallocate a good chunk here to avoid having to resize several times during initial population
                    _syncRecord = new List<SyncEntry>(10000);

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
                    //HookUtils.HookEnter<SceneData, Action<SceneData, PersistentBoolData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);
                    //HookUtils.HookEnter<SceneData, Action<SceneData, PersistentIntData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);
                    //HookUtils.HookEnter<SceneData, Action<SceneData, GeoRockData>>(nameof(SceneData.SaveMyState), OnSceneDataSaveMyState);

                    var saveStateInt = typeof(PersistentIntItem).GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance);
                    HookEndpointManager.Modify(saveStateInt, (Action<ILContext>)ModifySaveStateInt);

                    var saveStateBool = typeof(PersistentBoolItem).GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance);
                    HookEndpointManager.Modify(saveStateBool, (Action<ILContext>)ModifySaveStateBool);

                    var saveStateGeo = typeof(GeoRock).GetMethod("SaveState", BindingFlags.NonPublic | BindingFlags.Instance);
                    HookEndpointManager.Modify(saveStateGeo, (Action<ILContext>)ModifySaveStateGeo);

                    //Keep track of whether we're in save+quit
                    HookUtils.HookEnter<GameManager, Action<GameManager>>(nameof(GameManager.ReturnToMainMenu), OnGameManagerReturnToMainMenu);
                    HookUtils.HookEnter<GameManager, Action<GameManager, int>>(nameof(GameManager.SaveGame), OnGameManagerSaveGame);
                    HookUtils.HookExit<GameManager, Action<GameManager, int>>(nameof(GameManager.LoadGame), OnGameManagerLoadGame);
                }

                //var method = typeof(DeactivateInDarknessWithoutLantern).GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
                //HookEndpointManager.Modify(method, (Action<ILContext>)DeactivateInDarknessWithoutLanternStart);
                TryParseSyncFiles();
            }

            //Only apply flags if we're not in the middle of a save+quit
            //Any flag updates in the that time will accumulate and then all be applied at once after the game is loaded
            if (_saveAndQuitState != SaveAndQuitState.WaitingForLoad) {
                ApplyEntries();
            }
        }

        private static void OnGameManagerLoadGame(GameManager self, int slot) {
            //After any load, we consider the game to be in a state where we can apply flags
            _saveAndQuitState = SaveAndQuitState.None;
        }

        private static void OnGameManagerSaveGame(GameManager self, int slot) {
            //We only want to stop applying flags if we're issuing a Save due to quitting to menu
            if (_saveAndQuitState == SaveAndQuitState.ReturningToMenu) {
                _saveAndQuitState = SaveAndQuitState.WaitingForLoad;
            }
        }

        private static void OnGameManagerReturnToMainMenu(GameManager self) {
            //We keep applying flags up until the point that the game is actually saved
            _saveAndQuitState = SaveAndQuitState.ReturningToMenu;
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
            if (_syncEntries == null) return;

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
                ApplyPlayerDataInt(entry);
            }
        }

        private static void ApplyPlayerDataInt(SyncEntry entry) {
            switch (entry.Operation) {
                case Operation.Set:
                    PlayerData.instance.SetInt(entry.Tag, (int)entry.Value);
                    break;
                case Operation.Increment:
                    PlayerData.instance.IncrementInt(entry.Tag);
                    break;
                case Operation.Decrement:
                    PlayerData.instance.DecrementInt(entry.Tag);
                    break;
                case Operation.Add:
                    PlayerData.instance.IntAdd(entry.Tag, (int)entry.Value);
                    break;
            }

            if (entry.Tag == "nailDamage") {
                PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
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

        public static void RequestReload() {
            TryParseSyncFiles();
        }

        private static void TryParseSyncFiles() {
            if (!Directory.Exists(PlaybackSystem.Folder)) {
                //If no sync file, we're not doing syncing
                return;
            }

            //var writeTime = File.GetLastWriteTime(SyncFile);
            //if (writeTime == _lastFileWriteTime) {
            //    //If the file hasn't changed, don't bother re-parsing it
            //    return;
            //}

            try {
                var currentTime = Time.unscaledTime;
                var syncEntries = new ListQueue<SyncEntry>(1000);

                foreach (var filename in Directory.GetFiles(PlaybackSystem.Folder, "MultiSync*.txt")) {
                    using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new StreamReader(stream)) {
                        while (!reader.EndOfStream) {
                            if (TryParseEntry(reader.ReadLine(), out var entry)) {
                                //Only include entries that are in the future
                                if (entry.Time >= currentTime) {
                                    syncEntries.Add(entry);
                                }
                            }
                        }
                    }
                }

                //Sort so that earlier time is later in the list, since de-queueing happens from the end
                syncEntries.Sort((a, b) => Math.Sign(b.Time - a.Time));
                _syncEntries = syncEntries;
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

            var valueText = parts[2].Trim();
            Operation op = Operation.Set;
            if (valueText.StartsWith("+=")) {
                op = Operation.Add;
                valueText = valueText.Substring(2);
            } else if (valueText.StartsWith("++")) {
                op = Operation.Increment;
                valueText = valueText.Substring(2);
            } else if (valueText.StartsWith("--")) {
                op = Operation.Decrement;
                valueText = valueText.Substring(2);
            }

            float value = 0;
            if (op == Operation.Set || op == Operation.Add) {
                success &= float.TryParse(valueText.TrimStart('+'), NumberStyles.Float, NumberFormatInfo.InvariantInfo, out value);
            }

            entry = new SyncEntry(time, syncType, tag, value, op);
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

        private enum SaveAndQuitState {
            None,
            ReturningToMenu,
            WaitingForLoad
        }
        private enum SyncType {
            None,
            Geo,
            PlayerData,
            SceneData
        }

        private enum Operation {
            Set,
            Add,
            Increment,
            Decrement
        }

        private readonly struct SyncEntry {
            public SyncEntry(float time, SyncType syncType, string tag, float value, Operation operation) {
                Time = time;
                SyncType = syncType;
                Tag = tag;
                Value = value;
                Operation = operation;
            }

            public float Time { get; }

            public SyncType SyncType { get; }

            public string Tag { get; }

            public float Value { get; }

            public Operation Operation { get; }

            public override string ToString() {
                switch (SyncType) {
                    case SyncType.None:
                        return "";

                    case SyncType.Geo:
                        return $"{TimeStr(Time)},Geo,{Value}";

                    case SyncType.PlayerData:
                        return $"{TimeStr(Time)},PlayerData.{Tag},{OpStr()}";

                    case SyncType.SceneData:
                        return $"{TimeStr(Time)},SceneData.{Tag},{Value}";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private string OpStr() {
                switch (Operation) {
                    case Operation.Set:
                        return $"{Value}";
                    case Operation.Add:
                        return $"+={Value}";
                    case Operation.Increment:
                        return "++";
                    case Operation.Decrement:
                        return "--";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public static SyncEntry Geo(int amount) {
                return new SyncEntry(UnityEngine.Time.unscaledTime, SyncType.Geo, "", amount, Operation.Set);
            }

            public static SyncEntry PlayerData(string tag, float amount, Operation op) {
                return new SyncEntry(UnityEngine.Time.unscaledTime, SyncType.PlayerData, tag, amount, op);
            }

            public static SyncEntry SceneData(string tag, float amount) {
                return new SyncEntry(UnityEngine.Time.unscaledTime, SyncType.SceneData, tag, amount, Operation.Set);
            }
        }
    }
}
