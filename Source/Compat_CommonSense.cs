using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TableRange {
    [HarmonyPatch]
    internal static class Compat_CommonSense {
        [HarmonyPrepare]
        public static bool Active() 
            => LoadedModManager.RunningMods.Any(m => m.PackageId == Strings.CS_ID);

        [HarmonyTargetMethod]
        public static MethodInfo Method() 
            => AccessTools.Method("CommonSense.JobDriver_PrepareToIngestToils_ToolUser_CommonSensePatch:reserveChewSpot");

        [HarmonyPostfix]
        public static Toil ReserveChewSpot_post(Toil toil) 
            => Patch_ToilsIngest.CarryIngestibleToChewSpot_Postfix(toil);
    }
}
