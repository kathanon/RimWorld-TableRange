using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using Verse;
using Verse.Noise;
using static UnityEngine.UI.CanvasScaler;

namespace TableRange {
    public abstract class TableState : IExposable {
        private float range = -1f;
        private bool active = false;

        protected List<IntVec3> adjacentCached;
        protected List<IntVec3> areaCached;
        protected bool areaDirty = true;
        protected bool adjacentDirty = true;

        public static TableState For(Thing t) {
            bool link = !(t is GhostThing);
            if (t.Linkable()) {
                TableState res = link ? null : new Linked();
                foreach (var adj in t.AdjacentLinked(MapOf(t))) {
                    var state = State.Tables.Find(adj);
                    if (res != null) {
                        res = res.Merge(state, t, link);
                    } else {
                        res = state;
                    }
                }
                return res ?? new Linked(t, link);
            } else {
                return new Single(t, link);
            }
        }

        public static TableState ForGhost(Map map, IntVec3 pos, ThingDef def, Rot4 rot) 
            => For(new GhostThing(map, pos, def, rot));

        public float Range {
            get => (active && range >= 0f) ? range : MySettings.SearchRange;
            set {
                if (range != value) {
                    range = value;
                    if (active) State.Tables.UpdateMax(range);
                    areaDirty = true;
                }
            }
        }

        public bool Active {
            get => active && MySettings.IndividualRange;
            set {
                if (active != value) {
                    active = value;
                    State.Tables.UpdateMax(active ? range : 0f);
                    areaDirty = true;
                }
            }
        }

        public List<IntVec3> Area => areaDirty ? Cache() : areaCached;

        public void Dirty() => areaDirty = true;

        public bool HasValue => range >= 0f;

        public bool IsActive() => active;

        public void Toggle() => Active = !active;

        public bool InRange(float distanceSquared) {
            float r = Range;
            return distanceSquared < r * r;
        }

        public void ExposeData() {
            Scribe_Values.Look(ref active, "active", false);
            Scribe_Values.Look(ref range, "range", -1);
        }

        public void ToSaveState(SaveState save) {
            int i = save.stateWorkList.Count;
            save.stateWorkList.Add(this);
            ToSaveState(i, save);
        }

        public virtual void Remove(Thing t) {}

        protected static Map MapOf(Thing t) => (t is GhostThing gt) ? gt.map : t.Map;

        protected abstract void ToSaveState(int i, SaveState save);

        protected abstract void CacheSet();

        protected virtual TableState Merge(TableState other, Thing t, bool link) {
            // Remove when debugging done
            throw new NotImplementedException();
            // Should never occur, but just pretend like nothing. :)
            return this;
        }

        protected List<IntVec3> Cache() {
            if (adjacentDirty) CacheSet();

            float range = Range;
            bool union = MySettings.RangeToAny;

            var cells = new HashSet<IntVec3>();
            IEnumerable<IntVec3> origins = adjacentCached;
            if (MySettings.UseChairs) origins = FilterForChairs(origins);
            bool first = true;
            foreach (IntVec3 origin in origins) {
                var cellsForThis = GenRadial.RadialCellsAround(origin, range, true);
                if (union || first) {
                    cells.UnionWith(cellsForThis);
                } else {
                    cells.IntersectWith(cellsForThis);
                }
                first = false;
            }
            areaCached = cells.ToList();

            areaDirty = false;
            return areaCached;
        }

        private IEnumerable<IntVec3> FilterForChairs(IEnumerable<IntVec3> origins) {
            var map = Find.CurrentMap;
            var filtered = origins.Where(HasSittable);
            return filtered.Any() ? filtered : origins;

            bool HasSittable(IntVec3 pos) => pos.Sittable(map);
        }

        private class Single : TableState {
            private readonly Thing table;

            public Single(Thing table, bool link) {
                this.table = table;
                if (link) State.Tables.Link(table, this);
            }

            protected override void CacheSet() {
                adjacentCached = table.AdjacentNonLinked(MapOf(table)).ToList();
            }

            protected override void ToSaveState(int i, SaveState save) 
                => save.indexWorkMap[table] = i;
        }

        private class Linked : TableState {
            private HashSet<Thing> tables;

            public Linked() {
                tables = new HashSet<Thing>();
            }

            public Linked(Thing table, bool link) 
                : this(DiscoverFrom(table), link) {}

            public Linked(HashSet<Thing> tables, bool link) {
                this.tables = tables;
                if (link) State.Tables.Link(tables, this);
            }

            protected override TableState Merge(TableState other, Thing t, bool link) {
                if (other is Linked linked) {
                    tables.AddRange(linked.tables);
                    if (link) State.Tables.Link(linked.tables, this);
                    bool otherActive = linked.active && !active;
                    bool otherValue = linked.active == active && linked.HasValue && !HasValue;
                    if (otherActive || otherValue) {
                        active = linked.active;
                        range = linked.range;
                    }
                }
                tables.Add(t);
                if (link) State.Tables.Link(t, this);
                areaDirty = adjacentDirty = true;
                return this;
            }

            protected override void CacheSet() {
                var adjacent = new HashSet<IntVec3>();
                foreach (var table in tables) {
                    adjacent.AddRange(table.AdjacentNonLinked(MapOf(table)));
                }
                adjacentCached = adjacent.ToList();
            }

            public override void Remove(Thing t) {
                if (tables != null || !tables.Contains(t)) return;

                tables.Remove(t);
                areaDirty = adjacentDirty = true;

                int n = 0;
                var linked = t.AdjacentLinked(MapOf(t));
                Thing first = null;
                foreach (var adj in linked) {
                    if (first == null) first = adj;
                    n++;
                }

                if (n > 1) {
                    bool split = false;
                    var firstPart = DiscoverFrom(first);
                    foreach (var part in linked.Where(p => !firstPart.Contains(p))) {
                        if (State.Tables[part] == this) {
                            new Linked(part, true);
                        }
                        split = true;
                    }
                    if (split) {
                        tables = firstPart;
                    }
                }
            }

            private static HashSet<Thing> DiscoverFrom(Thing t) {
                var res = new HashSet<Thing>();
                var queue = new Queue<Thing>();
                queue.Enqueue(t);

                while (queue.Count > 0) {
                    t = queue.Dequeue();
                    if (!res.Contains(t)) {
                        res.Add(t);
                        foreach (var adj in t.AdjacentLinked(MapOf(t))) {
                            queue.Enqueue(adj);
                        }
                    }
                }

                return res;
            }

            protected override void ToSaveState(int i, SaveState save) {
                foreach (var t in tables) {
                    save.indexWorkMap[t] = i;
                }
            }
        }

        private class GhostThing : Thing {
            public readonly Map map;

            public GhostThing(Map map, IntVec3 pos, ThingDef def, Rot4 rot) {
                Position = pos;
                this.def = def;
                Rotation = rot;
                this.map = map;
            }
        }
    }
}