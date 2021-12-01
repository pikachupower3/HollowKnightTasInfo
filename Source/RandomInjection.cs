using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public static class RandomInjection {
        private static MethodInfo _rangeFloat;
        private static MethodInfo _rangeInt;
        private static MethodInfo _getRwi;
        private static List<Dictionary<string, PlaybackState>> _playback;
        private static List<Dictionary<string, List<float>>> _recording;
        private static List<string> _detailLog;
        private static List<string> _sceneNames;
        private static object _lock;
        private static int _sceneIndex;

        public static bool EnablePlayback;
        public static bool EnableRecording;
        public static bool EnableDetailLogging;

        public static void Init() {
            _lock = new object();
            _sceneNames = new List<string>();
            EnableRecording = true;
            EnableDetailLogging = false;
            EnablePlayback = true;
            _sceneIndex = 0;

            if (EnableDetailLogging)
                _detailLog = new List<string>();

            var ueRandom = typeof(UnityEngine.Random);
            _rangeFloat = ueRandom.GetMethod("Range", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(float), typeof(float)}, null);
            _rangeInt = ueRandom.GetMethod("Range", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(int), typeof(int)}, null);
            _getRwi = typeof(ActionHelpers).GetMethod("GetRandomWeightedIndex", BindingFlags.Static | BindingFlags.Public);

            var targetMethods = new List<MethodInfo>();
            targetMethods.Add(typeof(HealthCocoon).GetMethod("FlingObjects", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(HeroController).GetMethod("TakeDamage", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(ArrayGetRandom).GetMethod("DoGetRandomValue", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(ArrayListGetRandom).GetMethod("GetRandomItem", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(ArrayListShuffle).GetMethod("DoArrayListShuffle", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(ArrayShuffle).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(ChaseObject).GetMethod("DoBuzz", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(CreatePoolObjects).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(DistanceWalk).GetMethod("DoWalk", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(FireAtTarget).GetMethod("DoSetVelocity", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingFlashingGeo).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingObject).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingObjects).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingObjectsFromGlobalPool).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingObjectsFromGlobalPoolTime).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(FlingObjectsFromGlobalPoolVel).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(GetRandomChild).GetMethod("DoGetRandomChild", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(GetRandomObject).GetMethod("DoGetRandomObject", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(IdleBuzz).GetMethod("DoBuzz", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(IdleBuzzV2).GetMethod("DoBuzz", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(IdleBuzzV3).GetMethod("DoBuzz", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomBool).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomEvent).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomEvent).GetMethod("GetRandomEvent", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomFloat).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomFloatEither).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomFloatV2).GetMethod("Randomise", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomInt).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomlyFlipFloat).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomFloatEither).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(RandomWait).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnFromPool).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnFromPoolV2).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnObjectFromGlobalPoolOverTimeV2).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnRandomObjects).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnRandomObjectsOverTime).GetMethod("DoSpawn", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnRandomObjectsOverTimeV2).GetMethod("DoSpawn", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnRandomObjectsV2).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(SpawnRandomObjectsVelocity).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(WaitRandom).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance));
            targetMethods.Add(typeof(WalkLeftRight).GetMethod("SetupStartingDirection", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(IdleBuzzing).GetMethod("Buzz", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(Probability).GetMethod("GetRandomGameObjectByProbability", BindingFlags.Public | BindingFlags.Static));
            targetMethods.Add(typeof(SetZ).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(SetZRandom).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance));
            targetMethods.Add(typeof(StalactiteControl).GetMethod("FlingObjects", BindingFlags.NonPublic | BindingFlags.Instance));
            foreach (var method in targetMethods.Where(m => m != null)) {
                if (typeof(FsmStateAction).IsAssignableFrom(method.DeclaringType)) {
                    InjectOnRangeFsm(method);
                } else {
                    InjectOnRange(method);
                }
            }

            var rwiMethods = new[] {
                typeof(SelectRandomGameObject).GetMethod("DoSelectRandomGameObject", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(SelectRandomString).GetMethod("DoSelectRandomString", BindingFlags.NonPublic | BindingFlags.Instance),
                typeof(SelectRandomVector3).GetMethod("DoSelectRandomColor", BindingFlags.NonPublic | BindingFlags.Instance), //This is a copy/paste error in the game code; intentional
                typeof(SendRandomEvent).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance),
                typeof(SendRandomEventV2).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance),
                typeof(SendRandomEventV3).GetMethod("OnEnter", BindingFlags.Public | BindingFlags.Instance),
            };
            foreach (var method in rwiMethods.Where(m => m != null)) {
                InjectGetRwiFsm(method);
            }

            _playback = new List<Dictionary<string, PlaybackState>>();
            _recording = new List<Dictionary<string, List<float>>>();

            if (EnablePlayback)
                LoadPlaybackFiles();
        }

        private static void LoadPlaybackFiles() {
            string playbackPath = "./Playback/RNG";
            if (Directory.Exists(playbackPath)) {
                var files = Directory.GetFiles(playbackPath);
                EnablePlayback = files.Length > 0;
                foreach (var file in files.OrderBy(f => f, StringComparer.InvariantCulture)) {
                    LoadPlaybackFile(file);
                }
            }
        }

        private static void LoadPlaybackFile(string file) {
            using (var stream = File.Open(file, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(stream)) {
                _playback.Add(new Dictionary<string, PlaybackState>());
                var playback = _playback[_playback.Count - 1];
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.Length > 0 && line[0] != '#') {
                        int indexFirst = line.IndexOf(',');
                        if (indexFirst > -1) {
                            var name = line.Substring(0, indexFirst).Trim();
                            var values = StringUtils.ParseFloats(line.Substring(indexFirst + 1));
                            if (!playback.TryGetValue(name, out var playbackState)) {
                                playbackState = new PlaybackState();
                                playback.Add(name, playbackState);
                            }

                            playbackState.Values.AddRange(values);
                        }
                    }
                }
            }
        }

        public static void DumpLogs() {
            lock (_lock) {
                if (!Directory.Exists("./Recording"))
                    Directory.CreateDirectory("./Recording");
                string recordingPath = "./Recording/RNG";
                if (!Directory.Exists(recordingPath))
                    Directory.CreateDirectory(recordingPath);

                for (int i = 0; i < _recording.Count; i++) {
                    var recording = _recording[i];
                    var sceneName = i < _sceneNames.Count ? _sceneNames[i] : "Unknown";
                    using (var stream = File.Open($"{recordingPath}/S{i:00000}_{sceneName}.csv", FileMode.Create, FileAccess.Write))
                    using (var writer = new StreamWriter(stream)) {
                        
                        foreach (var name in recording.Keys) {
                            writer.WriteLine($"{name},{StringUtils.Join(",", recording[name])}");
                        }
                    }
                }

                if (EnableDetailLogging && _detailLog != null) {
                    using (var stream = File.Open("./HK_Rng_Details.csv", FileMode.Create, FileAccess.Write))
                    using (var writer = new StreamWriter(stream)) {
                        foreach (var entry in _detailLog) {
                            writer.WriteLine(entry);
                        }
                    }
                }
            }
        }

        public static void NotifyBeginScene(string sceneName) {
            lock (_lock) {
                _sceneNames.Add(sceneName);
                if (EnableDetailLogging && _detailLog != null) {
                    _detailLog.Add("");
                    _detailLog.Add("");
                    _detailLog.Add("### Scene: " + sceneName);
                    _detailLog.Add("");
                }
            }
        }

        public static void OnLeftScene() {
            _sceneIndex++;
        }

        private static void InjectOnRange(MethodInfo method) {
            HookEndpointManager.Modify(method, (Action<ILContext>)InjectOnRange);
        }

        private static void InjectOnRangeFsm(MethodInfo method) {
            HookEndpointManager.Modify(method, (Action<ILContext>)InjectOnRangeFsm);
        }

        private static void InjectGetRwiFsm(MethodInfo method) {
            HookEndpointManager.Modify(method, (Action<ILContext>)InjectGetRwiFsm);
        }
        
        private static void InjectOnRange(ILContext il) {
            var name = TrimNamespace(il.Method.Name);
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(_rangeFloat))) {
                c.Remove();
                c.Emit(OpCodes.Ldstr, name);
                c.Emit(OpCodes.Call, typeof(RandomInjection).GetMethod("OnRangeFloat", BindingFlags.Public | BindingFlags.Static));
            }

            c.Goto(0);
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(_rangeInt))) {
                c.Remove();
                c.Emit(OpCodes.Ldstr, name);
                c.Emit(OpCodes.Call, typeof(RandomInjection).GetMethod("OnRangeInt", BindingFlags.Public | BindingFlags.Static));
            }
        }

        private static void InjectOnRangeFsm(ILContext il) {
            var name = TrimNamespace(il.Method.Name);
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(_rangeFloat))) {
                c.Remove();
                c.Emit(OpCodes.Ldstr, name);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Castclass, typeof(FsmStateAction));
                c.Emit(OpCodes.Call, typeof(RandomInjection).GetMethod("OnRangeFloatFsm", BindingFlags.Public | BindingFlags.Static));
            }

            c.Goto(0);
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(_rangeInt))) {
                c.Remove();
                c.Emit(OpCodes.Ldstr, name);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Castclass, typeof(FsmStateAction));
                c.Emit(OpCodes.Call, typeof(RandomInjection).GetMethod("OnRangeIntFsm", BindingFlags.Public | BindingFlags.Static));
            }
        }

        private static void InjectGetRwiFsm(ILContext il) {
            var name = TrimNamespace(il.Method.Name);
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.Before, x => x.MatchCall(_getRwi))) {
                c.Remove();
                c.Emit(OpCodes.Ldstr, name);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Castclass, typeof(FsmStateAction));
                c.Emit(OpCodes.Call, typeof(RandomInjection).GetMethod("OnGetRwiFsm", BindingFlags.Public | BindingFlags.Static));
            }
        }

        private static string TrimNamespace(string name) {
            var playMakerNs = "HutongGames.PlayMaker.Actions.";
            if (name.StartsWith(playMakerNs))
                name = name.Substring(playMakerNs.Length);

            return name;
        }

        public static float OnRangeFloat(float min, float max, string name) {
            lock (_lock) {
                float result;
                if (EnablePlayback && TryGetPlayback(name, out var playbackState) && playbackState.Index < playbackState.Values.Count) {
                    playbackState.Index++;
                    result = playbackState.Values[playbackState.Index - 1];
                } else {
                    result = UnityEngine.Random.Range(min, max);
                }

                if (EnableRecording) {
                    var list = GetRecording(name);
                    list.Add(result);
                }

                if (_detailLog != null) {
                    _detailLog.Add($"{name}: {result}");
                }

                return result;
            }
        }

        public static int OnRangeInt(int min, int max, string name) {
            lock (_lock) {
                int result;
                if (EnablePlayback && TryGetPlayback(name, out var playbackState) && playbackState.Index < playbackState.Values.Count) {
                    playbackState.Index++;
                    result = (int)playbackState.Values[playbackState.Index - 1];
                } else {
                    result = UnityEngine.Random.Range(min, max);
                }

                if (EnableRecording) {
                    var list = GetRecording(name);
                    list.Add(result);
                }

                if (_detailLog != null) {
                    _detailLog.Add($"{name}: {result}");
                }

                return result;
            }
        }

        public static float OnRangeFloatFsm(float min, float max, string name, FsmStateAction action) {
            return OnRangeFloat(min, max, $"[{action.Fsm?.GameObjectName ?? ""}/{action.Fsm?.Name ?? ""}]{name}");
        }

        public static int OnRangeIntFsm(int min, int max, string name, FsmStateAction action) {
            return OnRangeInt(min, max, $"[{action.Fsm?.GameObjectName ?? ""}/{action.Fsm?.Name ?? ""}]{name}");
        }

        public static int OnGetRwiFsm(FsmFloat[] weights, string name, FsmStateAction action) {
            lock (_lock) {
                var compName = $"[{action.Fsm?.GameObjectName ?? ""}/{action.Fsm?.Name ?? ""}]{name}";
                int result;
                if (EnablePlayback && TryGetPlayback(compName, out var playbackState) && playbackState.Index < playbackState.Values.Count) {
                    playbackState.Index++;
                    result = (int)playbackState.Values[playbackState.Index - 1];
                } else {
                    result = ActionHelpers.GetRandomWeightedIndex(weights);
                }

                if (EnableRecording) {
                    var list = GetRecording(compName);
                    list.Add(result);
                }

                if (_detailLog != null) {
                    _detailLog.Add($"{compName}: {result}");
                }

                return result;
            }
        }

        private static bool TryGetPlayback(string name, out PlaybackState state) {
            state = null;
            if (_sceneIndex >= _playback.Count)
                return false;

            return _playback[_sceneIndex].TryGetValue(name, out state);
        }

        private static List<float> GetRecording(string name) {
            while (_sceneIndex >= _recording.Count) {
                _recording.Add(new Dictionary<string, List<float>>());
            }

            if (!_recording[_sceneIndex].TryGetValue(name, out var result)) {
                result = new List<float>();
                _recording[_sceneIndex].Add(name, result);
            }

            return result;
        }

        private sealed class PlaybackState {
            public PlaybackState() {
                Values = new List<float>();
            }

            public int Index { get; set; }

            public List<float> Values { get; }
        }
    }
}
