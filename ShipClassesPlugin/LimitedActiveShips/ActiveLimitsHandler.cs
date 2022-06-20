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
        public static List<long> gridIds = new List<long>();

        public static bool Remove(String factionTag, LiveShip ship, ShipClassDefinition shipClass, long gridId)
        {

            if (shipClass.LimitType == Enums.LimitTypeEnum.FACTION)
            {
                if (Limited.TryGetValue(factionTag, out var limits))
                {
                    if (limits.ContainsKey(ship.ClassName))
                    {
                        limits[ship.ClassName] -= 1;
                        gridIds.Remove(gridId);
                        return true;
                    }
                }
                return false;
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.ALLIANCE)
            {
                var alliance = AllianceIntegration.AllianceHandler.GetAllianceId(factionTag);
                if (alliance != Guid.Empty)
                {
                    if (Limited.TryGetValue(alliance.ToString(), out var limits))
                    {
                        if (limits.ContainsKey(ship.ClassName))
                        {
                            limits[ship.ClassName] -= 1;
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool CanShipBeActive(String factionTag, ShipClassDefinition shipClass, long gridId)
        {
            if (gridIds.Contains(gridId))
            {
                return true;
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.FACTION)
            {
                return AddToActiveShips(factionTag, shipClass, shipClass.MaximumActiveAmount, gridId);
            }
            if (shipClass.LimitType == Enums.LimitTypeEnum.ALLIANCE)
            {
                var alliance = AllianceIntegration.AllianceHandler.GetAllianceId(factionTag);
                if (alliance != Guid.Empty)
                {
                    return AddToActiveShips(alliance.ToString(), shipClass, AllianceIntegration.AllianceHandler.GetMaximumAmount(factionTag, shipClass.ClassType), gridId);
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

        public static bool AddToActiveShips(String tagOrGuid, ShipClassDefinition shipClass, int Maximum, long gridId)
        {
            if (shipClass.MaximumActiveAmount == 0)
            {
                return false;
            }
            if (gridIds.Contains(gridId))
            {
                return true;
            }
            if (Limited.TryGetValue(tagOrGuid, out Dictionary<String, int> Limits))
            {
                if (Limits.TryGetValue(shipClass.ClassType, out int inUse))
                {
                    if (inUse >= shipClass.MaximumActiveAmount || inUse + 1 > shipClass.MaximumActiveAmount)
                    {
                        return false;
                    }
                    else
                    {
                        if (!gridIds.Contains(gridId))
                        {

                            gridIds.Add(gridId);
                            Limits[shipClass.ClassType] += 1;
                            return true;
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
                else
                {
                    if (!gridIds.Contains(gridId))
                    {

                        gridIds.Add(gridId);
                        Limits.Add(shipClass.ClassType, 1);
                        return true;
                    }
               
                }
            }
            else
            {
                var limits = new Dictionary<String, int>();
                limits.Add(shipClass.ClassType, 1);
                Limited.Add(tagOrGuid, limits);
                gridIds.Add(gridId);
                return true;
            }

            return false;
        }


    }
}
