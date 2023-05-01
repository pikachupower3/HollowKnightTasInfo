using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal class SplitReader { 
        public static List<Split> SplitList = new();
        public static bool ReadSplits = false;
        public static void OnInit() {

            if (!File.Exists(ConfigManager.SplitFileLocation)) {
                Console.WriteLine("Can't read splits");
                return;
            }

            ReadSplits = true;

            XmlTextReader reader = new XmlTextReader(ConfigManager.SplitFileLocation);

            XmlDocument doc = new XmlDocument();
            doc.Load(reader);

            XmlNodeList segment_Names = doc.SelectNodes("Run/Segments/Segment/Name");
            XmlNodeList segment_Tirggers = doc.SelectNodes("Run/AutoSplitterSettings/Splits/Split");

            List<SplitName> splitNames = new List<SplitName>();

            foreach (XmlNode node in segment_Tirggers) {
                try {
                    splitNames.Add((SplitName)Enum.Parse(typeof(SplitName), node.InnerText));
                }
                catch {
                    splitNames.Add(SplitName.ManualSplit);
                }
            }

            for (int i = 0; i < segment_Names.Count; i++) {
                if (i >= splitNames.Count) {
                    SplitList.Add(new Split(segment_Names[i].InnerText, SplitName.ManualSplit));
                } else {
                    SplitList.Add(new Split(segment_Names[i].InnerText, splitNames[i]));
                }
            }

            for (int i = 0; i < SplitList.Count; i++) {
                Console.WriteLine(i + " " + SplitList.ElementAt(i).SplitTitle + " " + SplitList.ElementAt(i).SplitTrigger);
            }
        }
    }
}
