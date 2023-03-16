using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal class LiveSplitWrite {
        private static readonly string FilePath = "./liveSplit.txt";

        public static void WriteTest(List<string> info) {
            using (var stream = File.Open(FilePath, FileMode.Create, FileAccess.Write))
            using (var writer  = new StreamWriter(stream)) {
                foreach (var line in info) {
                    writer.WriteLine(line);
                }
            }
        }
    }
}
