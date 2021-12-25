using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assembly_CSharp.TasInfo.mm.Source;
using MonoMod;

class patch_InputHandler : InputHandler {
    [MonoModIgnore]
    private extern void orig_Update();

    private void Update() {
        orig_Update();
        InputsLogger.Update();
    }
}
