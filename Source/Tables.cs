using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TableRange {
    public class IndividualTables {
        private static Dictionary<Thing, TableState> tables = new Dictionary<Thing, TableState>();
        private static float max = -1f;

        public float Max => max;

        public TableState this[Thing t] {
            get {
                if (!tables.ContainsKey(t)) {
                    var cells = TableCells.For(t);
                    cells.AddStateTo(tables);
                }
                return tables[t];
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

        private void Cleanup() {
            List<Thing> toDelete = tables.Keys.Where(t => t.Destroyed).ToList();
            toDelete.ForEach(t => tables.Remove(t));
        }

        private void UpdateLinkable() {
            var work = tables.Keys.Where(t => t.Linkable()).ToHashSet();
            TableCellsLinked.UpdateLinkable(tables, work);
        }

        public void ExposeData(SaveState save) {
            if (Scribe.mode == LoadSaveMode.Saving) {
                Cleanup();
            }

            Scribe_Collections.Look(ref tables, "tables", LookMode.Reference, LookMode.Deep,
                ref save.keyWorkList, ref save.valueWorkList);

            if (Scribe.mode == LoadSaveMode.PostLoadInit) {
                Cleanup();
                UpdateLinkable();
                UpdateMax(0);
            }
        }
    }

    public class TableState : IExposable {
        private float range = -1f;
        private bool active = false;

        public readonly HashSet<Thing> Tables;

        public TableState(bool linkable) {
            Tables = linkable ? new HashSet<Thing>() : null;
        }

        public float Range {
            get => (active && range >= 0f) ? range : MySettings.SearchRange;
            set {
                range = value;
                if (active)
                    State.Tables.UpdateMax(range);
            }
        }

        public bool Active {
            get => active;
            set {
                active = value;
                State.Tables.UpdateMax(active ? range : 0f);
            }
        }

        public bool HasValue => range >= 0f;

        public bool IsActive() => active;

        public void Toggle() => active = !active;

        public bool InRange(float distanceSquared) {
            float r = Range;
            return distanceSquared < r * r;
        }

        public static implicit operator float(TableState t) => t.Range;
        public static implicit operator bool(TableState t) => t.Active;

        public void ExposeData() {
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Values.Look(ref range, "range", -1);
        }

        public TableState Split() => 
            new TableState(true) {
                active = active,
                range = range,
            };
    }

    public interface ITableCells {
        void AddStateTo(Dictionary<Thing, TableState> tables);
    }

    public class TableCells : ITableCells {
        private readonly Thing t;

        public void AddStateTo(Dictionary<Thing, TableState> tables) => 
            tables[t] = new TableState(false);

        private TableCells(Thing t) {
            this.t = t;
        }

        public static ITableCells For(Thing t) {
            if (t.Linkable()) {
                return TableCellsLinked.For(t);
            } else {
                return new TableCells(t);
            }
        }

        public static IEnumerable<IntVec3> GetAdjacent(ThingDef def, IntVec3 center, Rot4 rot) {
            if (def.graphicData.Linked) {
                return TableCellsLinked.GetAdjacent(def, center, rot);
            } else {
                return GenAdjFast.AdjacentCellsCardinal(center, rot, def.size);
            }
        }
    }

    public class TableCellsLinked : ITableCells {
        private readonly List<Thing> parts;

        public void AddStateTo(Dictionary<Thing, TableState> tables) => AddStateTo(tables, false);

        private void AddStateTo(Dictionary<Thing, TableState> tables, bool split) {
            var existing = parts.Where(p => tables.ContainsKey(p)).Select(p => tables[p]);
            var state = existing.FirstOrDefault(s => s.HasValue) 
                     ?? existing.FirstOrDefault()
                     ?? new TableState(true);
            if (split) {
                state = state.Split();
            }
            var set = state.Tables;
            foreach (Thing t in parts) {
                tables[t] = state;
                set.Remove(t);
            }
            UpdateLinkable(tables, set, true);
            set.AddRange(parts);
        }

        public static void UpdateLinkable(Dictionary<Thing, TableState> tables, HashSet<Thing> work, bool split = false) {
            while (work.Any()) {
                var t = work.First();
                For(t).Merge(tables, work, split);
            }
        }

        private void Merge(Dictionary<Thing, TableState> tables, HashSet<Thing> work, bool split) {
            AddStateTo(tables, split);
            foreach (var part in parts) work.Remove(part);
        }

        private TableCellsLinked(ICollection<Thing> parts) {
            this.parts = new List<Thing>(parts);
        }

        public static TableCellsLinked For(Thing t) {
            var parts = new HashSet<Thing>();
            FindParts(t.def, t.Position, t.Rotation, parts);
            return new TableCellsLinked(parts);
        }

        public static IEnumerable<IntVec3> GetAdjacent(ThingDef def, IntVec3 center, Rot4 rot) {
            var adjacent = new HashSet<IntVec3>();
            FindParts(def, center, rot, new HashSet<Thing>(), adjacent);
            return adjacent;
        }

        private static void FindParts(
            ThingDef def, IntVec3 center, Rot4 rot, 
            HashSet<Thing> parts, HashSet<IntVec3> adjacent = null) {
            Map map = Find.CurrentMap;
            var work = new Queue<IntVec3>();
            work.Enqueue(center);
            bool first = true;
            while (work.Any()) {
                var pos = work.Dequeue();
                Building table = pos.GetEdifice(map);
                if (first) {
                    if (table != null) parts.Add(table);
                } else {
                    def = table.def;
                    rot = table.Rotation;
                    first = false;
                }
                foreach (var adj in GenAdjFast.AdjacentCellsCardinal(pos, rot, def.size)) {
                    if (Links(adj)) {
                        Building adjTable = adj.GetEdifice(map);
                        if (!parts.Contains(adjTable)) {
                            parts.Add(adjTable);
                            work.Enqueue(adjTable.Position);
                        }
                    } else if (adjacent != null) {
                        adjacent.Add(adj);
                    }
                }
            }

            bool Links(IntVec3 pos) => (map.linkGrid.LinkFlagsAt(pos) & def.graphicData.linkFlags) != 0;
        }
    }

    public static class TableExtensions {
        public static bool Linkable(this Thing t) => t.def.graphicData.Linked;
    }
}