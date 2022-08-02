using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TableRange
{
    public class PlaceWorker_EatRange : PlaceWorker
    {
        public const float MaxRenderRange = MySettings.MaxRange;

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            if (!MySettings.DisplayRange) return;

            var range = (thing != null) ? State.Tables[thing] : MySettings.SearchRange.Value;
            var union = MySettings.RangeToAll;

            if (range > MaxRenderRange) return;

            var cells = new HashSet<IntVec3>();
            var origins = TableCells.GetAdjacent(def, center, rot);
            if (MySettings.UseChairs) origins = FilterForChairs(origins);
            bool first = true;
            foreach (IntVec3 origin in origins)
            {
                var cellsForThis = GenRadial.RadialCellsAround(origin, range, true);
                if (union || first)
                {
                    cells.UnionWith(cellsForThis);
                }
                else
                {
                    cells.IntersectWith(cellsForThis);
                }
                first = false;
            }

            GenDraw.DrawFieldEdges(cells.ToList());
        }

        private IEnumerable<IntVec3> FilterForChairs(IEnumerable<IntVec3> origins)
        {
            var map = Find.CurrentMap;
            var filtered = origins.Where(HasSittable);
            return filtered.Any() ? filtered : origins;

            bool HasSittable(IntVec3 pos) =>
                pos.InBounds(map) && pos.GetThingList(map).Any(t => WillBeSittable(t.def));

            bool WillBeSittable(ThingDef d) => 
                IsSittable(d) || IsSittable(d.entityDefToBuild as ThingDef);

            bool IsSittable(ThingDef d) => d?.building?.isSittable ?? false;
        }

        private IEnumerable<IntVec3> GetOrigins(IntVec3 center, IntVec2 size, Rot4 rot)
        {
            if (rot.IsHorizontal) (size.z, size.x) = (size.x, size.z);

            int x = center.x - (size.x - 1) / 2;
            int z = center.z - (size.z - 1) / 2;
            if (rot.AsInt     > 1 && size.x % 2 == 0) --x;
            if (rot.AsInt % 3 > 0 && size.z % 2 == 0) --z;

            IntVec3 origin = center;
            int x1 = x - 1;
            int x2 = x + size.x;
            for (origin.z = z + size.z - 1; origin.z >= z; --origin.z)
            {
                origin.x = x1;
                yield return origin;
                origin.x = x2;
                yield return origin;
            }
            int z1 = z - 1;
            int z2 = z + size.z;
            for (origin.x = x + size.x - 1; origin.x >= x; --origin.x)
            {
                origin.z = z1;
                yield return origin;
                origin.z = z2;
                yield return origin;
            }
        }
    }
}
