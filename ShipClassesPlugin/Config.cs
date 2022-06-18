using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin
{
    public enum LimitType
    {
        Faction,
        Alliance
    }

    public class Config
    {
        public int SecondsBetweenBeaconChecks = 30;
        public LimitType limits = LimitType.Faction;
    }
}
