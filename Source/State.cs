﻿using HugsLib.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace TableRange {
    public static class State {
        private static readonly HashSet<ThingDef> tableDefs = new HashSet<ThingDef>();

        public static bool IsTable(this ThingDef def) => tableDefs.Contains(def);

        public static readonly IndividualTables Tables = new IndividualTables();

        public static void Setup() {
            Type worker = typeof(PlaceWorker_EatRange);
            Type comp = typeof(TableRangeComp);
            CompProperties compProperties = new CompProperties(comp);
            tableDefs.Clear();
            tableDefs.AddRange(DefDatabase<ThingDef>.AllDefs.Where(WillBeTable));
            foreach (var def in tableDefs) {
                if (def.placeWorkers == null) {
                    def.placeWorkers = new List<Type> { worker };
                } else if (!def.placeWorkers.Contains(worker)) {
                    def.placeWorkers.Add(worker);
                }

                if (IsTable(def)) {
                    if (def.comps == null) {
                        def.comps = new List<CompProperties> { compProperties };
                    } else if (!def.comps.Any(p => p.compClass == comp)) {
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

        public static void UpdateRangeDirty(SettingHandle _) 
            => Tables.DirtyAll();

        public static void UpdateSelected(SettingHandle _) {
            bool selected = MySettings.Selected;
            foreach (var def in tableDefs) {
                def.drawPlaceWorkersWhileSelected = selected;
            }
        }

        public static void UpdateRange(SettingHandle _) {
            Tables.UpdateMax(MySettings.SearchRange);
        }

        public static void ExposeData(SaveState save) {
            if (Scribe.EnterNode(Strings.MOD_IDENTIFIER)) {
                try {
                    Tables.ExposeData(save);
                } finally {
                    Scribe.ExitNode();
                }
            }
        }
    }

    public class SaveState : GameComponent {
        internal List<Thing> keyWorkList = new List<Thing>();
        internal List<int> indexWorkList = new List<int>();
        internal List<TableState> stateWorkList = new List<TableState>();
        internal Dictionary<Thing,int> indexWorkMap = new Dictionary<Thing,int>();

        public SaveState(Game game) { }

        public override void ExposeData() {
            State.ExposeData(this);
        }
    }
}