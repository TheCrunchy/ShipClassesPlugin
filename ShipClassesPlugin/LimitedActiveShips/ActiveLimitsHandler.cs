using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin.LimitedActiveShips
{
    public class ActiveLimitsHandler
    {
        public static Dictionary<string, Dictionary<String, int>> Limited = new Dictionary<string, Dictionary<String, int>>();

        public bool CanShipBeActive(String factionTag, ShipClassDefinition shipClass)
        {
            if (shipClass.LimitType == Enums.LimitTypeEnum.FACTION)
            {
                return AddToActiveShips(factionTag, shipClass, shipClass.MaximumActiveAmount);
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.ALLIANCE)
            {
                var alliance = AllianceIntegration.AllianceHandler.GetAllianceId(factionTag);
                if (alliance != Guid.Empty)
                {
                    return AddToActiveShips(alliance.ToString(), shipClass, AllianceIntegration.AllianceHandler.GetMaximumAmount(shipClass.ClassType));
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

        public bool AddToActiveShips(String tagOrGuid, ShipClassDefinition shipClass, int Maximum)
        {
            if (shipClass.MaximumActiveAmount == 0)
            {
                return false;
            }
            if (Limited.TryGetValue(tagOrGuid, out Dictionary<String, int> Limits)){
                if (Limits.TryGetValue(shipClass.ClassType, out int inUse))
                {
                    if (inUse >= shipClass.MaximumActiveAmount || inUse + 1 >= shipClass.MaximumActiveAmount)
                    {
                        return false;
                    }else
                    {
                        Limits[shipClass.ClassType] += 1;
                        return true;
                    }
                }else
                {
                    Limits.Add(shipClass.ClassType, 1);
                }
            }
            else
            {
                var limits = new Dictionary<String, int>();
                limits.Add(shipClass.ClassType, 1);
                Limited.Add(tagOrGuid, limits);
            }

            return false;
        }


    }
}
