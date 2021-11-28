using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source.Extensions;
using InControl.NativeProfile;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Assembly_CSharp.TasInfo.mm.Source.Utils {
    [Serializable]
    public readonly struct RngState {
        [SerializeField]
        public readonly int s0;
        [SerializeField]
        public readonly int s1;
        [SerializeField]
        public readonly int s2;
        [SerializeField]
        public readonly int s3;
        public RngState(int s0, int s1, int s2, int s3) {
            this.s0 = s0;
            this.s1 = s1;
            this.s2 = s2;
            this.s3 = s3;
        }

        public static RngState CurrentState() {
            return (RngState)UnityEngine.Random.state;
        }

        public static explicit operator RngState(UnityEngine.Random.State st) {
            unsafe {
                return *(RngState*)(void*)&st;
            }
        }

        public static explicit operator Random.State(RngState st) {
            unsafe {
                return *(UnityEngine.Random.State*)(void*)&st;
            }
        }

        public override string ToString() {
            return $"{s0:X8}_{s1:X8}_{s2:X8}_{s3:X8}";
        }

        public static RngState Parse(string text) {
            var split = text.Split('_');
            if (split.Length != 4) 
                throw new ArgumentException("Text must be _ delimited hex numbers for the four components of Rng state");

            var s0 = int.Parse(split[0], NumberStyles.HexNumber);
            var s1 = int.Parse(split[1], NumberStyles.HexNumber);
            var s2 = int.Parse(split[2], NumberStyles.HexNumber);
            var s3 = int.Parse(split[3], NumberStyles.HexNumber);
            return new RngState(s0, s1, s2, s3);
        }
    }

}
