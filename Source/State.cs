using HugsLib.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace TableRange
{
    public static class State
    {
        private static readonly List<ThingDef> tableDefs = new List<ThingDef>();

        public static IEnumerable<ThingDef> TableDefs => tableDefs;

        public static readonly IndividualTables Tables = new IndividualTables();

        public static void Setup()
        {
            Type worker = typeof(PlaceWorker_EatRange);
            Type comp = typeof(TableRangeComp);
            CompProperties compProperties = new CompProperties(comp);
            tableDefs.Clear();
            tableDefs.AddRange(DefDatabase<ThingDef>.AllDefs.Where(WillBeTable));
            foreach (var def in TableDefs)
            {
                if (def.placeWorkers == null)
                {
                    def.placeWorkers = new List<Type> { worker };
                }
                else if (!def.placeWorkers.Contains(worker))
                {
                    def.placeWorkers.Add(worker);
                }

                if (IsTable(def))
                {
                    if (def.comps == null)
                    {
                        def.comps = new List<CompProperties> { compProperties };
                    }
                    else if (!def.comps.Any(p => p.compClass == comp))
                    {
                        def.comps.Add(compProperties);
                    }
                }
            }
            UpdateSelected(null);
            UpdateRange(null);

            bool WillBeTable(ThingDef d) =>
                IsTable(d) || IsTable(d.entityDefToBuild as ThingDef);

            bool IsTable(ThingDef d) => d?.surfaceType == SurfaceType.Eat;
        }

        public static void UpdateSelected(SettingHandle _)
        {
            bool selected = MySettings.Selected;
            foreach (var def in TableDefs)
            {
                def.drawPlaceWorkersWhileSelected = selected;
            }
        }

        public static void UpdateRange(SettingHandle _)
        {
            Tables.UpdateMax(MySettings.SearchRange);
        }

        public static void ExposeData(SaveState save)
        {
            if (Scribe.EnterNode(Strings.MOD_IDENTIFIER))
            {
                Tables.ExposeData(save);
                Scribe.ExitNode();
            }
        }

        public class IndividualTables
        {
            private static Dictionary<Thing, Table> tables = new Dictionary<Thing, Table>();
            private static float max = 0f;

            public float Max => max;

            public Table this[Thing t]
            {
                get
                {
                    if (!tables.ContainsKey(t)) tables.Add(t, new Table());
                    return tables[t];
                }
            }

            public void UpdateMax(float value)
            {
                max = (value > max) 
                    ? value 
                    : Mathf.Max(
                        tables.Values.Select(x => x.Range).Max(), 
                        MySettings.SearchRange);
            }

            public bool ChairInRange(Thing t, IntVec3 start)
            {
                IntVec3 pos = t.Position;
                float distanceSquared = (pos - start).LengthHorizontalSquared;
                for (int i = 0; i < 4; i++)
                {
                    Building table = (pos + GenAdj.CardinalDirections[i]).GetEdifice(t.Map);
                    if (table != null && table.def.surfaceType == SurfaceType.Eat)
                    {
                        if (this[table].InRange(distanceSquared)) return true;
                    }
                }

                return false;
            }

            private void Cleanup()
            {
                List<Thing> toDelete = tables.Keys.Where(t => t.Destroyed).ToList();
                toDelete.ForEach(t => tables.Remove(t));
            }

            public void ExposeData(SaveState save)
            {
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    Cleanup();
                }

                Scribe_Collections.Look(ref tables, "tables", LookMode.Reference, LookMode.Deep, ref save.keyWorkList, ref save.valueWorkList);

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    Cleanup();
                }
            }

            public class Table : IExposable
            {
                private float range = -1f;
                private bool active = false;

                public float Range
                {
                    get => (active && range >= 0f) ? range : MySettings.SearchRange;
                    set
                    {
                        range = value;
                        if (active) Tables.UpdateMax(range);
                    }
                }

                public bool Active
                {
                    get => active;
                    set
                    {
                        active = value;
                        Tables.UpdateMax(active ? range : 0f);
                    }
                }

                public bool IsActive() => active;

                public void Toggle() => active = !active;

                public bool InRange(float distanceSquared)
                {
                    float r = Range;
                    return distanceSquared < r * r;
                }

                public static implicit operator float(Table t) => t.Range;
                public static implicit operator bool (Table t) => t.Active;

                public void ExposeData()
                {
                    Scribe_Values.Look(ref active, "active", false);
                    Scribe_Values.Look(ref range, "range", -1);
                }
            }
        }
    }

    public class SaveState : GameComponent
    {
        internal List<Thing> keyWorkList = new List<Thing>();
        internal List<State.IndividualTables.Table> valueWorkList = new List<State.IndividualTables.Table>();

        public SaveState(Game game) { }

        public override void ExposeData()
        {
            State.ExposeData(this);
        }
    }
}