using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public static class InputsLogger {
        private const int BlockSize = 10000;

        private static List<Data[]> _blocks;
        private static int _blockIndex;
        private static int _dataIndex;
        private static KeyCode[] _keysToMonitor;
        private static string[] _inputStrings;
        private static bool _initialized;
        private static string _lastScene;
        private static List<string> _sceneManifest;

        private static void Init() {
            _blocks = new List<Data[]>();
            _blocks.Add(new Data[BlockSize]);
            _blockIndex = 0;
            _dataIndex = 0;
            _keysToMonitor = new[] {
                KeyCode.LeftArrow,
                KeyCode.RightArrow,
                KeyCode.UpArrow,
                KeyCode.DownArrow,
                KeyCode.Z,
                KeyCode.X,
                KeyCode.C,
                KeyCode.A,
                KeyCode.D,
                KeyCode.F,
                KeyCode.Escape,
                KeyCode.I,
                KeyCode.S,
                KeyCode.Tab,
                KeyCode.Return,
                KeyCode.RightBracket,
                KeyCode.LeftBracket
            };
            _inputStrings = new[] {
                "ff51",
                "ff53",
                "ff52",
                "ff54",
                "7a",
                "78",
                "63",
                "61",
                "64",
                "66",
                "ff1b",
                "69",
                "73",
                "ff09",
                "ff0d",
                "5d",
                "5b"
            };
            _sceneManifest = new List<string>();
            _sceneManifest.Add("Scene,StartTime");
            _initialized = true;
        }

        public static void Update() {
            if (!_initialized) {
                Init();
            }

            if (_dataIndex >= BlockSize) {
                _blockIndex++;
                _blocks.Add(new Data[BlockSize]);
                _dataIndex = 0;
            }
            
            var block = _blocks[_blockIndex];
            block[_dataIndex].unscaledDeltaTime = Time.unscaledDeltaTime;

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            block[_dataIndex].scene = scene.name;
            var gameManager = GameManager.instance;
            if (gameManager)
                block[_dataIndex].isGameplayScene = gameManager.IsGameplayScene();

            uint keys = 0;
            for (int i = 0; i < _keysToMonitor.Length; i++) {
                if (Input.GetKey(_keysToMonitor[i])) {
                    keys |= (uint)(1 << i);
                }
            }
            block[_dataIndex].inKeys = keys;
            _dataIndex++;

            if (scene.name != _lastScene) {
                _lastScene = scene.name;
                _sceneManifest.Add($"{scene.name},{Time.unscaledTime}");
            }

        }

        public static void DumpLogs() {
            var inputsPath = "./Inputs";
            if (!Directory.Exists(inputsPath))
                Directory.CreateDirectory(inputsPath);

            int seqIndex = 0;
            int sceneIndex = 0;
            StreamWriter inputsWriter = null;
            try {
                string lastScene = null;
                for (int i = 0; i < _blocks.Count; i++) {
                    int dataCount = i < _blocks.Count - 1 ? BlockSize : _dataIndex;
                    for (int k = 0; k < dataCount; k++) {
                        var datum = _blocks[i][k];
                        if (inputsWriter == null || datum.scene != lastScene) {
                            lastScene = datum.scene;

                            inputsWriter?.Dispose();

                            var inputsFile = File.Open($"{inputsPath}/Q{seqIndex:00000}_S{sceneIndex:00000}_{lastScene ?? ""}_Inputs.txt", FileMode.Create, FileAccess.Write);
                            inputsWriter = new StreamWriter(inputsFile);

                            //When starting a new frame, automatically tag it with a '[' to make it easier to find when editing
                            var bracketIndex = Array.FindIndex(_keysToMonitor, key => key == KeyCode.LeftBracket);
                            datum.inKeys |= (uint)(1 << bracketIndex);

                            if (datum.isGameplayScene)
                                sceneIndex++;
                            seqIndex++;
                        }

                        Data? nextDatum = null;
                        if (k < dataCount - 1) {
                            nextDatum = _blocks[i][k + 1];
                        }
                        else if (i < _blocks.Count - 1) {
                            nextDatum = _blocks[i + 1][0];
                        }
                        if (nextDatum != null) {
                            //Compute FPS at high resolution
                            int fpsDenom = 1000000;
                            int fpsNumer = Mathf.RoundToInt(fpsDenom*1f/nextDatum.Value.unscaledDeltaTime);
                            string frameRateText;
                            if (fpsNumer != 100*fpsDenom) {
                                //Reduce fraction to simplify
                                while (fpsDenom > 1 && fpsNumer%10 == 0) {
                                    fpsNumer /= 10;
                                    fpsDenom /= 10;
                                }

                                frameRateText = $"T{fpsNumer}:{fpsDenom}|";
                            } else {
                                //At 100 FPS, don't need to add anything
                                frameRateText = "";
                            }

                            inputsWriter.WriteLine($"{InputsFormat(datum.inKeys)}{frameRateText}");
                        } else {
                            inputsWriter.WriteLine($"{InputsFormat(datum.inKeys)}");
                        }
                    }
                }
            } finally {
                inputsWriter?.Dispose();
            }

            using (var file = File.Open(Path.Combine(inputsPath, "SceneManifest.csv"), FileMode.Create, FileAccess.Write)) 
            using (var writer = new StreamWriter(file)) {
                foreach (var line in _sceneManifest)
                    writer.WriteLine(line);
            }
        }

        private static string InputsFormat(uint inKeys) {
            var builder = new StringBuilder();
            builder.Append("|K");
            bool anyKeys = false;
            for (int i = 0; i < _keysToMonitor.Length; i++) {
                if ((inKeys & (uint) (1 << i)) != 0) {
                    builder.Append(_inputStrings[i]);
                    builder.Append(":");
                    anyKeys = true;
                }
            }

            //Remove excess delimiter
            if (anyKeys) {
                builder.Remove(builder.Length - 1, 1);
            }
            builder.Append('|');
            return builder.ToString();
        }

        private struct Data {
            public float unscaledDeltaTime;
            public string scene;
            public bool isGameplayScene;
            public uint inKeys;
        }
    }
}
