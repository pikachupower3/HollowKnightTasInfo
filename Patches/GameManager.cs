// ReSharper disable All

using System.Collections;
using System.Collections.Generic;
using Assembly_CSharp.TasInfo.mm.Source;
using MonoMod;

#pragma warning disable CS0649, CS0414

public class patch_GameManager : GameManager {
    private static readonly long TasInfoMark = 1234567890123456789;
    public static string TasInfo;

    [MonoModIgnore]
    public extern void orig_LeftScene(bool doAdditiveLoad);

    public new void LeftScene(bool doAdditiveLoad) {
        RngSyncer.OnLeftScene();
        orig_LeftScene(doAdditiveLoad);
    }

    //[MonoModIgnore]
    //public extern void orig_TransitionScene(TransitionPoint gate);

    //[MonoModIgnore]
    //public extern void orig_ChangeToScene(string targetScene, string entryGateName, float pauseBeforeEnter);

    //[MonoModIgnore]
    //public extern void orig_WarpToDreamGate();

    //public new void TransitionScene(TransitionPoint gate) {
    //    RngSyncer.OnLeftScene();
    //    orig_TransitionScene(gate);
    //}

    //public new void ChangeToScene(string targetScene, string entryGateName, float pauseBeforeEnter) {
    //    RngSyncer.OnLeftScene();
    //    orig_ChangeToScene(targetScene, entryGateName, pauseBeforeEnter);
    //}

    //public new void WarpToDreamGate() {
    //    RngSyncer.OnLeftScene();
    //    orig_WarpToDreamGate();
    //}

#if V1028
    [MonoModIgnore]
    private extern void orig_ManualLevelStart();
    private void ManualLevelStart() {
        orig_ManualLevelStart();
        Assembly_CSharp.TasInfo.mm.Source.TasInfo.AfterManualLevelStart();
    }
#endif
}