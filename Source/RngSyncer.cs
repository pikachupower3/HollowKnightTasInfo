using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Assembly_CSharp.TasInfo.mm.Source.Utils;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public static class RngSyncer {
        private static List<string> _syncScenes;
        private static List<RngState> _syncStates;
        private static int _syncIndex;

        public static void OnInit() {
            var filename = "./HK_RNG_Transitions.csv";
            if (File.Exists(filename)) {
                _syncScenes = new List<string>();
                _syncStates = new List<RngState>();
                _syncIndex = 0;
                using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read)) 
                using (var reader = new StreamReader(stream)) {
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        var split = line.Split(',');
                        _syncStates.Add(RngState.Parse(split[1]));
                        _syncScenes.Add(split[0]);
                    }
                }
            }
        }

        public static void OnLeftScene() {
            var sceneName = SyncLogger.LastScene;
            Debug.Log($"Left scene {sceneName}");
            if (_syncStates != null && _syncIndex < _syncStates.Count && sceneName == _syncScenes[_syncIndex]) {
                var state = _syncStates[_syncIndex];
                _syncIndex++;

                Debug.Log($"Setting random state to {state}");
                Random.state = (Random.State) state;
                RngInfo.lastState = Random.state;
            }

            RngLogger.NotifySceneChanged(sceneName);
        }
    }
}
