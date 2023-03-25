// ReSharper disable All

using System;
using System.Collections;
using System.Collections.Generic;
using Assembly_CSharp.TasInfo.mm.Source;
using Mono.Cecil;
using MonoMod;
using UnityEngine;

#pragma warning disable CS0649, CS0414

class patch_GameManager : GameManager {
    private static readonly long TasInfoMark = 1234567890123456789;
    public static string TasInfo;

    //[MonoModIgnore]
    //public extern void orig_LeftScene(bool doAdditiveLoad);

    //[NoInlining]
    //public new void LeftScene(bool doAdditiveLoad) {
    //    //RandomInjection.OnLeftScene();
    //    orig_LeftScene(doAdditiveLoad);
    //}

#if V1028 || V1028_KRYTHOM
    [MonoModIgnore]
    private extern void orig_ManualLevelStart();
    private void ManualLevelStart() {
        orig_ManualLevelStart();
        Assembly_CSharp.TasInfo.mm.Source.TasInfo.AfterManualLevelStart();
    }
#endif

    public extern void orig_SetupSceneRefs(bool refreshTilemapInfo);

    public void SetupSceneRefs(bool refreshTilemapInfo)
    {
        orig_SetupSceneRefs(refreshTilemapInfo);


        if (IsGameplayScene())
        {
            GameObject go = GameCameras.instance.soulOrbFSM.gameObject.transform.Find("SoulOrb_fill").gameObject;
            GameObject liquid = go.transform.Find("Liquid").gameObject;
            tk2dSpriteAnimator tk2dsa = liquid.GetComponent<tk2dSpriteAnimator>();
            tk2dsa.GetClipByName("Fill").fps = 15 * 0.95f;
            tk2dsa.GetClipByName("Idle").fps = 10 * 0.95f;
            tk2dsa.GetClipByName("Shrink").fps = 15 * 0.95f;
            tk2dsa.GetClipByName("Drain").fps = 30 * 0.95f;
        }
    }}

[MonoModCustomMethodAttribute("NoInlining")]
public class NoInlining : Attribute {
}

namespace MonoMod {
    static partial class MonoModRules {
        // ReSharper disable once UnusedParameter.Global
        public static void NoInlining(MethodDefinition method, CustomAttribute attrib) => method.NoInlining = true;
    }
}