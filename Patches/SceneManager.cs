using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Assembly_CSharp.TasInfo.mm.Source;
using Mono.Cecil;
using MonoMod;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assembly_CSharp.TasInfo.mm.Pathces {
    [MonoModPatch("global::SceneManager")]
    public class SceneManager: global::SceneManager {

        [MonoModReplace]
        public void DrawBlackBorders() {

        }

    }
}
