using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace TableRange
{
    public class TableRangeComp : ThingComp
    {
        private TableRangeGizmo gizmo;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (MySettings.IndividualRange)
            {
                if (gizmo == null) gizmo = new TableRangeGizmo(parent);
                yield return gizmo;
            }
            yield break;
        }
    }
}
