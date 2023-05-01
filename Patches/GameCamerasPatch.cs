using MonoMod;
using Assembly_CSharp.TasInfo.mm.Source;

[MonoModPatch("global::GameCameras")]
public class GameCamerasPatch : global::GameCameras {
    public extern void orig_Start();

    public void Start() {
        orig_Start();
        ScreenShakeModifier.EditScreenShake(this);
    }
}