using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TableRange {
    public class PlaceWorker_EatRange : PlaceWorker {
        public const float MaxRenderRange = MySettings.MaxRange;

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null) {
            if (!MySettings.DisplayRange) return;

            List<IntVec3> cells;
            if (thing == null) {
                if (MySettings.SearchRange > MaxRenderRange) return;
                cells = State.Tables.CellsFor(Find.CurrentMap, def, center, rot);
            } else {
                var table = State.Tables[thing];
                if (table.Range > MaxRenderRange) return;
                cells = table.Area;
            }

            GenDraw.DrawFieldEdges(cells);
        }
    }
}
