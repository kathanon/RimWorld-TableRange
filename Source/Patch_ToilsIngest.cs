using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace TableRange
{
    [HarmonyPatch(typeof(Toils_Ingest))]
    public static class Patch_ToilsIngest
    {
        private static bool active = false;

        public const float VanillaRange = MySettings.VanillaRange;

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Toils_Ingest.CarryIngestibleToChewSpot))]
        public static Toil CarryIngestibleToChewSpot_Postfix(Toil toil)
        {
            var original = toil.initAction;
            toil.initAction = delegate {
                active = true;
                original();
                active = false;
            };
            return toil;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GenClosest), nameof(GenClosest.ClosestThingReachable))]
        public static void GenClosest_ClosestThingReachable_Prefix(IntVec3 root, ref float maxDistance, ref Predicate<Thing> validator)
        {
            if (active)
            {
                // TODO: Find better way of deciding to alter distance than doing == to default value
                float max = State.Tables.Max;
                if (maxDistance == VanillaRange || maxDistance > max)
                {
                    maxDistance = max;
                }
                Predicate<Thing> original = validator;
                validator = t => original(t) && State.Tables.ChairInRange(t, root);
            }
        }
    }
}
