using System.Collections.Generic;
using System.IO;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public class RngLogger {
        private static RngLogger _instance;
        private List<string> _messages;
        private List<string> _transitionScenes;
        private List<RngState> _transitionStates;
        private RngState _lastRngState;

        public static void OnInit() {
            _instance = new RngLogger();
            _instance.Awake();
        }

        private void Awake() {
            _messages = new List<string>();
            _transitionStates = new List<RngState>();
            _transitionScenes = new List<string>();
        }

        public static void LogRngChange(RngState newState, int callCount) {
            LogMessage($"${newState} (+{callCount})");
            _instance._lastRngState = newState;
        }

        public static void LogMessage(string message) {
            if (_instance != null)
                _instance._messages.Add($"{SyncLogger.EstRealFrame}: {message}");
        }

        public static void DumpLogFile() {
            using (var stream = File.Open("./HK_RNG.csv", FileMode.Create, FileAccess.Write)) 
            using (var writer = new StreamWriter(stream)) {
                foreach (var message in _instance._messages)
                    writer.WriteLine(message);
            }

            using (var stream = File.Open("./HK_RNG_Transitions.csv", FileMode.Create, FileAccess.Write)) 
            using (var writer = new StreamWriter(stream)) {
                for (int i = 0; i < _instance._transitionStates.Count; i++) {
                    var scene = _instance._transitionScenes[i];
                    var state = _instance._transitionStates[i];
                    writer.WriteLine($"{scene},{state}");
                }
            }
        }

        public static void NotifySceneChanged(string priorScene) {
            _instance._transitionScenes.Add(priorScene);
            _instance._transitionStates.Add((RngState)Random.state);
        }
    }
}
