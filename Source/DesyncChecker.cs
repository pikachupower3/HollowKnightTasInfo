using System.Text;
using UnityEngine;

namespace Assembly_CSharp.TasInfo.mm.Source {
    internal static class DesyncChecker {
        private static Random.State savedRandomState;
        private static int desyncFrame = 0;
        public static void BeforeUpdate() {
            savedRandomState = Random.state;
        }

        public static void AfterUpdate(StringBuilder infoBuilder) {
            if (desyncFrame == 0 && !savedRandomState.Equals(Random.state)) {
                desyncFrame = Time.frameCount;
            }

            //This is disabled, as it can be triggered by some features of the new tooling
            //It's just intended to check that the tooling itself doesn't cause rng to change
            //if (desyncFrame > 0) {
            //    infoBuilder.Append($"desync at frame {desyncFrame}");
            //}
        }
    }
}