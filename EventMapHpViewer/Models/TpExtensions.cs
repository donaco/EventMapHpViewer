using Grabacr07.KanColleWrapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventMapHpViewer.Models.Settings;
using Grabacr07.KanColleWrapper.Models;

namespace EventMapHpViewer.Models
{
    static class TpExtensions
    {
        public static TransportCapacity TransportationCapacity(this Organization org)
        {
            if (org == null) return new TransportCapacity(0m);

            decimal tp;
            if (org.Combined && org.CombinedFleet != null)
            {
                tp = org.CombinedFleet.State?.TransportPoint ?? 0m;
            }
            else
            {
                var firstFleet = org.Fleets?.Values?.OrderBy(x => x.Id).FirstOrDefault();
                tp = firstFleet?.State?.TransportPoint ?? 0m;
            }

            return new TransportCapacity(tp);
        }
    }
}
