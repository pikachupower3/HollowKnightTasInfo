using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Assembly_CSharp.TasInfo.mm.Source;
using MonoMod;
using UnityEngine;

public class patch_HeroController : HeroController {
    [MonoModIgnore]
    private extern void orig_OnCollisionEnter2D(Collision2D collision);

    [MonoModIgnore]
    private extern void orig_OnCollisionExit2D(Collision2D collision);

    [MonoModIgnore]
    private extern void orig_OnCollisionStay2D(Collision2D collision);

    private void OnCollisionEnter2D(Collision2D collision) {
        Interlocked.Increment(ref SyncLogger.EnterCount);
        orig_OnCollisionEnter2D(collision);
    }

    private void OnCollisionExit2D(Collision2D collision) {
        Interlocked.Increment(ref SyncLogger.ExitCount);
        orig_OnCollisionExit2D(collision);
    }

    private void OnCollisionStay2D(Collision2D collision) {
        Interlocked.Increment(ref SyncLogger.StayCount);
        orig_OnCollisionStay2D(collision);
    }
}
