using HugsLib.Settings;
using System;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace TableRange
{
    public static class MySettings
    {
        public static SettingHandle<float> SearchRange;
        public static SettingHandle<bool>  IndividualRange;
        public static SettingHandle<bool>  DisplayRange;
        public static SettingHandle<bool>  RangeToAll;
        public static SettingHandle<bool>  UseChairs;
        public static SettingHandle<bool>  Selected;

        public const float MaxRange = 56f;
        public const float VanillaRange = 32f;

        public static void Setup(ModSettingsPack settings)
        {
            SearchRange     = settings.GetHandle("searchRange",     Strings.SearchRange_title,     Strings.SearchRange_desc,     VanillaRange);
            IndividualRange = settings.GetHandle("individualRange", Strings.IndividualRange_title, Strings.IndividualRange_desc, true);
            DisplayRange    = settings.GetHandle("displayRange",    Strings.DisplayRange_title,    Strings.DisplayRange_desc,    true);
            RangeToAll      = settings.GetHandle("rangeToAll",      Strings.RangeToAll_title,      Strings.RangeToAll_desc,      true);
            UseChairs       = settings.GetHandle("useChairs",       Strings.UseChairs_title,       Strings.UseChairs_desc,       true);
            Selected        = settings.GetHandle("selected",        Strings.Selected_title,        Strings.Selected_desc,        true);

            Selected.ValueChanged    += State.UpdateSelected;
            SearchRange.ValueChanged += State.UpdateRange;
            SearchRange.CustomDrawer = DrawRangeOption;
        }

        public static bool DrawRangeOption(Rect rect)
        {
            float value = TableRangeGizmo.Slider(rect, SearchRange);
            bool change = value != SearchRange;
            if (change) SearchRange.Value = value;
            return change;
        }
    }
}