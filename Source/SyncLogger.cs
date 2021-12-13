using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Utils;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    public class SyncLogger {
        private const int BlockSize = 10000;

        private static SyncLogger _instance;

        public static long EnterCount;
        public static long ExitCount;
        public static long StayCount;

        private List<Data[]> _blocks;
        private KeyCode[] _keysToMonitor;
        private string[] _keyStrings;
        private string[] _inputStrings;
        private int _blockIndex;
        private int _dataIndex;
        private int _estRealFrame;
        private string _lastScene;

        public static string LastScene => _instance?._lastScene ?? "";

        public const bool OutputCombinedLog = false;

        public static void OnInit() {
            _instance = new SyncLogger();
            _instance.Awake();
        }

        private void Awake() {
            _blocks = new List<Data[]>();
            _blocks.Add(new Data[BlockSize]);
            _blockIndex = 0;
            _dataIndex = 0;
            _estRealFrame = 0;
            EnterCount = 0;
            ExitCount = 0;
            StayCount = 0;
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
            _keyStrings = new[] {
                "L",
                "R",
                "U",
                "D",
                "z",
                "x",
                "c",
                "a",
                "d",
                "f",
                "Esc",
                "i",
                "s",
                "Tab",
                "Ent",
                "]",
                "["
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
        }

        public static void OnPreRender() {
            _instance.Update();
        }

        public static int EstRealFrame => _instance._estRealFrame;

        private void Update() {
            if (_estRealFrame == 0) {
                _estRealFrame = Mathf.RoundToInt(Time.unscaledTime/Time.unscaledDeltaTime);
            } else {
                _estRealFrame++;
            }

            RecordData();

            if (Input.GetKeyDown(KeyCode.Equals)) {
                DumpLogFile();
                RandomInjection.DumpLogs();
            }

            if (Input.GetKey(KeyCode.RightBracket)) {
                RandomInjection.RollRngNextScene();
            }

            if (Input.GetKey(KeyCode.Backslash)) {
                GameManager.instance.FreezeMoment(2);
            }
        }

        private void RecordData() {
            if (_dataIndex >= BlockSize) {
                _blockIndex++;
                _blocks.Add(new Data[BlockSize]);
                _dataIndex = 0;
            }
            
            var block = _blocks[_blockIndex];
            block[_dataIndex].realFrame = _estRealFrame;
            block[_dataIndex].fixedFrame = Mathf.RoundToInt(Time.fixedTime/Time.fixedDeltaTime);
            block[_dataIndex].realTime = Time.time;
            block[_dataIndex].fixedTime = Time.fixedTime;
            block[_dataIndex].unscaledDeltaTime = Time.unscaledDeltaTime;

            var hero = GameManager.instance.hero_ctrl;
            if (hero) {
                var flags = DataFlags.None;
                if (hero.CanInput()) flags |= DataFlags.CanInput;
                if (hero.cState.wallSliding) flags |= DataFlags.WallSliding;
                if (hero.cState.dashing) flags |= DataFlags.Dashing;
                if (hero.cState.superDashing) flags |= DataFlags.CDashing;
                if (hero.cState.spellQuake) flags |= DataFlags.Diving;
                block[_dataIndex].flags = flags;

                var hTrans = hero.transform;
                block[_dataIndex].posX = hTrans.position.x;
                block[_dataIndex].posY = hTrans.position.y;
                block[_dataIndex].velX = hero.current_velocity.x;
                block[_dataIndex].velY = hero.current_velocity.y;
                block[_dataIndex].rollTimes = RngInfo.rollTimes;

                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                block[_dataIndex].scene = scene;
                if (scene != _lastScene) {
                    _lastScene = scene;
                    RandomInjection.NotifyBeginScene(scene);
                }

                uint keys = 0;
                for (int i = 0; i < _keysToMonitor.Length; i++) {
                    if (Input.GetKey(_keysToMonitor[i])) {
                        keys |= (uint)(1 << i);
                    }
                }
                block[_dataIndex].inKeys = keys;

                block[_dataIndex].phEn = EnterCount;
                block[_dataIndex].phEx = ExitCount;
                block[_dataIndex].phSt = StayCount;
            }

            _dataIndex++;
        }

        private void DumpLogFile() {
            if (OutputCombinedLog) {
                using (var stream = File.Open("./HollowKnightSyncLog.csv", FileMode.Create, FileAccess.Write)) 
                using (var writer = new StreamWriter(stream)){
                    writer.WriteLine("RF,PF,RT,FT,InE,WC,DSH,CDSH,DIV,X,Y,VX,VY,RNG_Cnt,Scene,InK,PhEn,PhEx,PhSt");
                    for (int i = 0; i < _blocks.Count; i++) {
                        int dataCount = i < _blocks.Count - 1 ? BlockSize : _dataIndex;
                        for (int k = 0; k < dataCount; k++) {
                            var datum = _blocks[i][k];
                            var canInput = FlagChar(datum, DataFlags.CanInput, '1', '0');
                            var wallSliding = FlagChar(datum, DataFlags.WallSliding, '1', '0');
                            var dashing = FlagChar(datum, DataFlags.Dashing, '1', '0');
                            var cDashing = FlagChar(datum, DataFlags.CDashing, '1', '0');
                            var diving = FlagChar(datum, DataFlags.Diving, '1', '0');
                            writer.WriteLine($"{datum.realFrame},{datum.fixedFrame},{datum.realTime},{datum.fixedTime},{canInput},{wallSliding},{dashing},{cDashing},{diving},{datum.posX},{datum.posY},{datum.velX},{datum.velY},{datum.rollTimes},{datum.scene},{InKeys(datum.inKeys)},{datum.phEn},{datum.phEx},{datum.phSt}");
                        }
                    }
                }
            }

            var diagPath = "./Diagnostics";
            if (!Directory.Exists(diagPath))
                Directory.CreateDirectory(diagPath);

            var inputsPath = "./Inputs";
            if (!Directory.Exists(inputsPath))
                Directory.CreateDirectory(inputsPath);

            int sceneIndex = 0;
            int realFrameStart = 0;
            int physFrameStart = 0;
            StreamWriter diagWriter = null;
            StreamWriter inputsWriter = null;
            try {
                string lastScene = null;
                for (int i = 0; i < _blocks.Count; i++) {
                    int dataCount = i < _blocks.Count - 1 ? BlockSize : _dataIndex;
                    for (int k = 0; k < dataCount; k++) {
                        var datum = _blocks[i][k];
                        if (string.IsNullOrEmpty(datum.scene))
                            continue;

                        if (diagWriter == null || datum.scene != lastScene) {
                            lastScene = datum.scene;

                            diagWriter?.Dispose();
                            var diagFile = File.Open($"{diagPath}/S{sceneIndex:00000}_{lastScene}_Diag.csv", FileMode.Create, FileAccess.Write);
                            diagWriter = new StreamWriter(diagFile);
                            diagWriter.WriteLine("RelFrame,RelPhysFrame,T-FT,InE,WC,DSH,CDSH,DIV,X,Y,VX,VY,InK");

                            //bool isNew = inputsWriter == null;
                            inputsWriter?.Dispose();
                            var inputsFile = File.Open($"{inputsPath}/S{sceneIndex:00000}_{lastScene}_Inputs.txt", FileMode.Create, FileAccess.Write);
                            inputsWriter = new StreamWriter(inputsFile);

                            //When starting a new frame, automatically tag it with a '[' to make it easier to find when editing
                            var bracketIndex = Array.FindIndex(_keysToMonitor, key => key == KeyCode.LeftBracket);
                            datum.inKeys |= (uint)(1 << bracketIndex);

                            //if (isNew) {
                            //    //The first scene has bunch of blank input frames prior to receiving any input
                            //    for (int frame = 0; frame < datum.realFrame; frame++) {
                            //        inputsWriter.WriteLine("|K|");
                            //    }
                            //}

                            realFrameStart = datum.realFrame;
                            physFrameStart = datum.fixedFrame;
                            sceneIndex++;
                        }

                        var canInput = FlagChar(datum, DataFlags.CanInput, '1', '0');
                        var wallSliding = FlagChar(datum, DataFlags.WallSliding, '1', '0');
                        var dashing = FlagChar(datum, DataFlags.Dashing, '1', '0');
                        var cDashing = FlagChar(datum, DataFlags.CDashing, '1', '0');
                        var diving = FlagChar(datum, DataFlags.Diving, '1', '0');
                        diagWriter.WriteLine($"{datum.realFrame - realFrameStart},{datum.fixedFrame - physFrameStart},{datum.realTime - datum.fixedTime},{canInput},{wallSliding},{dashing},{cDashing},{diving},{datum.posX},{datum.posY},{datum.velX},{datum.velY},{InKeys(datum.inKeys)}");

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
                diagWriter?.Dispose();
                inputsWriter?.Dispose();
            }
        }

        private char FlagChar(Data datum, DataFlags flag, char trueChar, char falseChar) {
            return (datum.flags & flag) > 0 ? trueChar : falseChar;
        }

        private string InKeys(uint inKeys) {
            var builder = new StringBuilder();
            for (int i = 0; i < _keysToMonitor.Length; i++) {
                if ((inKeys & (uint) (1 << i)) != 0) {
                    builder.Append(_keyStrings[i]);
                }
            }

            return builder.ToString();
        }

        private string InputsFormat(uint inKeys) {
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

        [System.Flags]
        private enum DataFlags {
            None = 0,
            CanInput = 0x01,
            WallSliding = 0x02,
            Dashing = 0x04,
            CDashing = 0x08,
            Diving = 0x10
        }

        private struct Data {
            public DataFlags flags;
            public int realFrame;
            public int fixedFrame;
            public float realTime;
            public float fixedTime;
            public float unscaledDeltaTime;
            public float posX;
            public float posY;
            public float velX;
            public float velY;
            public ulong rollTimes;
            public string scene;
            public uint inKeys;
            public long phEn;
            public long phEx;
            public long phSt;
        }
    }
}
