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
        private int _blockIndex;
        private int _dataIndex;
        private int _estRealFrame;
        private string _lastScene;
        private ulong _lastRollTimes;

        public static string LastScene => _instance?._lastScene ?? "";

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
                KeyCode.Return
            };
            _keyStrings = new[] {
                "L",
                "R",
                "U",
                "D",
                "[Z]",
                "[X]",
                "[C]",
                "[A]",
                "[D]",
                "[F]",
                "Esc",
                "[I]",
                "[S]",
                "Tab",
                "Ent"
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
                RngLogger.DumpLogFile();
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
                if (RngInfo.rollTimes != _lastRollTimes) {
                    RngLogger.LogRngChange(RngState.CurrentState(), (int)(RngInfo.rollTimes - _lastRollTimes));
                    _lastRollTimes = RngInfo.rollTimes;
                }

                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                block[_dataIndex].scene = scene;
                if (scene != _lastScene) {
                    _lastScene = scene;
                    //RngSyncer.OnLeftScene();
                    RngLogger.LogMessage($"### Scene: {scene}");
                    //RngLogger.NotifySceneChanged(_lastScene);
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
