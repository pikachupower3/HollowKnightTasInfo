using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.CompilerServices.SymbolWriter;

namespace Assembly_CSharp.TasInfo.mm.Source {
    /// <summary>
    /// This class manages Playback features, including RNG and MultiSync.
    /// Currently its primary role is to detect an Update signal and use that
    /// to tell the RNGInfo and MultiSyncInfo classes to reload their data
    /// from Playback
    /// </summary>
    public static class PlaybackSystem {
        public const string Folder = "./Playback";

        private static bool _initialized;
        private static DateTime _lastUpdateTime;

        //NOTE: This should be called before either MultiSync or RandomInjection
        //This way if reload is requested, it will be before their next update
        public static void OnPreRender() {
            if (!_initialized) {
                _initialized = true;
                _lastUpdateTime = DateTime.UtcNow;
            }

            const string updateFilename = Folder + "/Update";
            if (File.Exists(updateFilename)) {
                var updateTime = File.GetLastWriteTimeUtc(updateFilename);
                if (updateTime >= _lastUpdateTime) {
                    _lastUpdateTime = updateTime;
                    MultiSync.RequestReload();
                    RandomInjection.RequestReload();
                }
            }
        }
    }
}
