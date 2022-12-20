using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TableRange {
    [HarmonyPatch(typeof(Thing))]
    public static class Patch_ThingUpdate {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Thing.SpawnSetup))]
        public static void SpawnSetup(Thing __instance) 
            => State.Tables.Dirty(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Thing.DeSpawn))]
        public static void DeSpawn(Thing __instance) 
            => State.Tables.Dirty(__instance);
    }
}
