using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin.AllianceIntegration
{
    public static class AllianceHandler
    {
        public static int GetMaximumAmount(string factionTag, string classType)
        {
            if (!ShipClassPlugin.AlliancePluginEnabled)
            {
                return 0;
            }
            object[] MethodInput = new object[] { factionTag, classType};
            return (int) ShipClassPlugin.GetAllianceLimit?.Invoke(null, MethodInput);
        }
    }
}
