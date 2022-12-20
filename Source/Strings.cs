using System.Collections.Generic;
using Verse;

namespace TableRange
{
    internal static class Strings
    {
        // Non-translated constants
        public const string MOD_IDENTIFIER = "kathanon.TableRange";
        public const string PREFIX = MOD_IDENTIFIER + ".";

        public const string CS_ID = "avilmask.commonsense";

        // Settings
        public static readonly string SearchRange_title     = (PREFIX + "SearchRange.title"    ).Translate();
        public static readonly string SearchRange_desc      = (PREFIX + "SearchRange.desc"     ).Translate();
        public static readonly string IndividualRange_title = (PREFIX + "IndividualRange.title").Translate();
        public static readonly string IndividualRange_desc  = (PREFIX + "IndividualRange.desc" ).Translate();
        public static readonly string DisplayRange_title    = (PREFIX + "DisplayRange.title"   ).Translate();
        public static readonly string DisplayRange_desc     = (PREFIX + "DisplayRange.desc"    ).Translate();
        public static readonly string RangeToAny_title      = (PREFIX + "RangeToAny.title"     ).Translate();
        public static readonly string RangeToAny_desc       = (PREFIX + "RangeToAny.desc"      ).Translate();
        public static readonly string UseChairs_title       = (PREFIX + "UseChairs.title"      ).Translate();
        public static readonly string UseChairs_desc        = (PREFIX + "UseChairs.desc"       ).Translate();
        public static readonly string Selected_title        = (PREFIX + "Selected.title"       ).Translate();
        public static readonly string Selected_desc         = (PREFIX + "Selected.desc"        ).Translate();

        // Gizmo
        public static readonly string UseRange    = (PREFIX + "UseRange"    ).Translate();
        public static readonly string UseRangeTip = (PREFIX + "UseRange.tip").Translate();

        // Resources
        public static readonly string UseRangeTexturePath = MOD_IDENTIFIER + "/UseRange";
    }
}
