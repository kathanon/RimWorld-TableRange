using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;
using Verse;

namespace TableRange {
    public class IndividualTables {
        private static Dictionary<Thing, TableState> tables = new Dictionary<Thing, TableState>();
        private static float max = -1f;

        public float Max => max;

        public TableState this[Thing t] => Find(t, TableState.For);

        public TableState Find(Thing t, Func<Thing, TableState> f = null) 
            => tables.TryGetValue(t, out var state) ? state : f?.Invoke(t);

        public void DirtyAll() {
            foreach (var state in tables.Values) {
                state.Dirty();
            }
        }

        public void Dirty(Thing t) {
            if (t.def.IsTable()) {
                if (t.Destroyed) {
                    if (tables.TryGetValue(t, out var state)) {
                        tables.Remove(t);
                        state.Remove(t);
                    }
                } else {
                    this[t].Dirty();
                }
            } else if (t.def.IsSittable()) {
                foreach (var pos in GenAdjFast.AdjacentCellsCardinal(t.Position)) {
                    Building table = pos.GetEdifice(t.Map);
                    if (table != null && table.def.IsSittable()) {
                        Dirty(table);
                    }
                }
            }
        }

        public void Link(Thing table, TableState value) 
            => tables[table] = value;

        public void Link(IEnumerable<Thing> linkTables, TableState value) {
            foreach (var table in linkTables) {
                tables[table] = value;
            }
        }

        public void UpdateMax(float value) {
            if (value > max && max >= 0f) {
                max = value;
            } else {
                var ranges = tables.Values.Select(x => x.Range);
                max = ranges.Any()
                    ? Mathf.Max(ranges.Max(), MySettings.SearchRange)
                    : value;
            }
        }

        public bool ChairInRange(Thing t, IntVec3 start) {
            IntVec3 pos = t.Position;
            float distanceSquared = (pos - start).LengthHorizontalSquared;
            foreach (var pos2 in GenAdjFast.AdjacentCellsCardinal(pos)) {
                Building table = pos2.GetEdifice(t.Map);
                if (table != null && table.def.surfaceType == SurfaceType.Eat) {
                    if (this[table].InRange(distanceSquared)) {
                        return true;
                    }
                }
            }

            return false;
        }

        public List<IntVec3> CellsFor(Map map, ThingDef def, IntVec3 center, Rot4 rot) 
            => TableState.ForGhost(map, center, def, rot).Area;

        private void Cleanup() {
            List<Thing> toDelete = tables.Keys.Where(t => t.Destroyed).ToList();
            toDelete.ForEach(t => tables.Remove(t));
        }

        private void ToSaveState(SaveState save) {
            foreach (var p in tables) {
                if (!save.indexWorkMap.ContainsKey(p.Key)) {
                    p.Value.ToSaveState(save);
                }
            }
        }

        private void FromSaveState(SaveState save) {
            tables.Clear();
            if (save.indexWorkMap != null && save.stateWorkList != null) {
                foreach (var p in save.indexWorkMap) {
                    tables[p.Key] = save.stateWorkList[p.Value];
                }
            }
            save.stateWorkList?.Clear();
            save.keyWorkList?.Clear();
            save.indexWorkList?.Clear();
            save.indexWorkMap?.Clear();
        }

        public void ExposeData(SaveState save) {
            if (Scribe.mode == LoadSaveMode.Saving) {
                Cleanup();
                ToSaveState(save);
            }

            Scribe_Collections.Look(ref save.indexWorkMap, "tableIndex", LookMode.Reference, LookMode.Value,
                ref save.keyWorkList, ref save.indexWorkList);
            Scribe_Collections.Look(ref save.stateWorkList, "tableState", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                FromSaveState(save);
                Cleanup();
                UpdateMax(0f);
            }
        }
    }
}