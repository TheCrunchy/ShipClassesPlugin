using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin.AllianceIntegration
{
    public static class AllianceHandler
    {
        public static Guid GetAllianceId(string FactionTag)
        {
            return Guid.NewGuid();
        }
    }
}
