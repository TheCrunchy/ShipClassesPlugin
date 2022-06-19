using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin.LimitedActiveShips
{
    public class ActiveLimitsHandler
    {
        public static Dictionary<string, Dictionary<ShipClassDefinition, int>> Limited = new Dictionary<string, Dictionary<ShipClassDefinition, int>>();

        public bool CanShipBeActive(String factionTag, ShipClassDefinition shipClass)
        {
            if (shipClass.LimitType == Enums.LimitTypeEnum.FACTION)
            {
                return AddToActiveShips(factionTag, shipClass);
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.ALLIANCE)
            {
                var alliance = AllianceIntegration.AllianceHandler.GetAllianceId(factionTag);
                if (alliance != Guid.Empty)
                {
                    return AddToActiveShips(alliance.ToString(), shipClass);
                }
                else
                {
                    return false;
                }
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.NONE)
            {
                return true;
            }
            return true;
        }

        public bool AddToActiveShips(String tagOrGuid, ShipClassDefinition shipClass)
        {
            if (shipClass.MaximumActiveAmount == 0)
            {
                return false;
            }
            if (Limited.TryGetValue(tagOrGuid, out Dictionary<ShipClassDefinition, int> Limits)){
                if (Limits.TryGetValue(shipClass, out int inUse))
                {
                    if (inUse >= shipClass.MaximumActiveAmount || inUse + 1 >= shipClass.MaximumActiveAmount)
                    {
                        return false;
                    }else
                    {
                        Limits[shipClass] += 1;
                        return true;
                    }
                }else
                {
                    Limits.Add(shipClass, 1);
                }
            }
            else
            {
                var limits = new Dictionary<ShipClassDefinition, int>();
                limits.Add(shipClass, 1);
                Limited.Add(tagOrGuid, limits);
            }

            return false;
        }
    }
}
