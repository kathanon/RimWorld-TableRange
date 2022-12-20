using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace TableRange {
    public static class TableExtensions {
        public static bool Linkable(this Thing t) => t.def.graphicData.Linked;

        public static bool Sittable(this IntVec3 pos, Map map) 
            => pos.InBounds(map) && pos.GetThingList(map).Any(t => IsSittable(t.def));

        public static bool IsSittable(this ThingDef d) 
            => IsSittableNow(d) || IsSittableNow(d.entityDefToBuild as ThingDef);

        private static bool IsSittableNow(this ThingDef d) 
            => d?.building?.isSittable ?? false;

        public static IEnumerable<Thing> AdjacentLinked(this Thing t, Map map = null) {
            map = map ?? t.Map;
            return Adjacent(map, t.Position, t.def, t.Rotation, true)
                .Select(c => c.GetEdifice(map));
        }

        public static IEnumerable<IntVec3> AdjacentNonLinked(this Thing t, Map map = null) 
            => Adjacent(map ?? t.Map, t.Position, t.def, t.Rotation, false);

        private static IEnumerable<IntVec3> Adjacent(
                Map map, IntVec3 pos, ThingDef def, Rot4 rot, bool linked) {
            foreach (var adj in GenAdjFast.AdjacentCellsCardinal(pos, rot, def.size)) {
                bool isLinked = (map.linkGrid.LinkFlagsAt(adj) & def.graphicData.linkFlags) != 0;
                if (isLinked == linked) {
                    yield return adj;
                }
            }
        }
    }
}
