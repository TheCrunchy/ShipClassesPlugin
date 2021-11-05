using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Torch.Managers.PatchManager;

namespace ShipClassesPlugin
{
    public class ShipClassPlugin
    {
        //Load the config files into this dictionary
        public static Dictionary<String, ShipClassDefinition> DefinedClasses = new Dictionary<string, ShipClassDefinition>();

        //we store the active ships here, grid entity Id as the key
        public static Dictionary<long, LiveShip> ActiveShips = new Dictionary<long, LiveShip>();

        //blockpairname as key, the list is the name of the Classes that limit this block
        public static Dictionary<String, List<String>> LimitedBlocks = new Dictionary<string, List<string>>();


        public static Dictionary<long, Boolean> EnableTheBlock = new Dictionary<long, bool>();

        //fuck it, do the patching here, could be its own class file but effort
        [PatchShim]
        public class FunctionalBlockPatch
        {

            internal static readonly MethodInfo update =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation10", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

            internal static readonly MethodInfo update2 =
            typeof(MyFunctionalBlock).GetMethod("UpdateBeforeSimulation100", BindingFlags.Instance | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

            internal static readonly MethodInfo updatePatch =
           typeof(FunctionalBlockPatch).GetMethod(nameof(KeepDisabled), BindingFlags.Static | BindingFlags.Public) ??
           throw new Exception("Failed to find patch method");
            public static void Patch(PatchContext ctx)
            {
                ctx.GetPattern(update).Prefixes.Add(updatePatch);
                ctx.GetPattern(update2).Prefixes.Add(updatePatch);
            }

            public static Boolean DoChecks(MyFunctionalBlock block, ShipClassDefinition shipDefinition, LiveShip ship)
            {
                if (!ship.HasWorkingBeacon)
                {
                    block.Enabled = false;
                    return false;
                }
                if (EnableTheBlock.TryGetValue(block.EntityId, out Boolean val))
                {
                    if (!val)
                    {
                        block.Enabled = false;
                        return false;
                    }
                }
                else
                {
                    //now we need to add to the ship since we havent had this block before.
                    if (ship.CanBlockFunction(block.BlockDefinition.BlockPairName))
                    {
                        EnableTheBlock.Add(block.EntityId, true);
                        return true;
                    }
                    else
                    {
                        EnableTheBlock.Add(block.EntityId, false);
                        block.Enabled = false;
                        return false;
                    }
                }
                return true;
            }

            public static Boolean KeepDisabled(MyFunctionalBlock __instance)
            {
                if (LimitedBlocks.TryGetValue(__instance.BlockDefinition.BlockPairName, out List<String> ShipClasses))
                {
                    if (ActiveShips.TryGetValue(__instance.CubeGrid.EntityId, out LiveShip ship))
                    {

                        if (DateTime.Now >= ship.NextCheck)
                        {
                           var Beacons = __instance.CubeGrid.GetFatBlocks().OfType<MyBeacon>();

                            //so we need to recheck if the grid still has a working beacon with our class ID
                            //should probably change the seconds to a value from the config file
                            ship.NextCheck = DateTime.Now.AddSeconds(30);
                            ShipClassDefinition shipClass = DefinedClasses[ship.ClassName];

                            foreach (MyBeacon beacon in Beacons)
                            {
                                if (beacon.BlockDefinition.BlockPairName.Equals(shipClass.BeaconBlockPairName))
                                {
                                    if (beacon.IsFunctional && beacon.Enabled)
                                    {
                                        ship.HasWorkingBeacon = true;
                                    }
                                }
                            }
                        }
                        else
                        {

                            //move all this shit into a new method, pass it the shit it needs and reuse the method for after registering the live ship
                             if (DoChecks(__instance, DefinedClasses[ship.ClassName], ship))
                            {
                                
                                return true;
                            }
                             else
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        LiveShip newShip = new LiveShip();

                        newShip.GridEntityId = __instance.CubeGrid.EntityId;
                        foreach (String s in ShipClasses)
                        {
                            //fuck it, its using whichever beacon it finds first, if people use multiple tiers they can cry about it 
                            var Beacons = __instance.CubeGrid.GetFatBlocks().OfType<MyBeacon>();
                            if (DefinedClasses.TryGetValue(s, out ShipClassDefinition def))
                            {
                                foreach (MyBeacon beacon in Beacons)
                                {
                                    if (beacon.BlockDefinition.BlockPairName.Equals(def.BeaconBlockPairName))
                                    {
                                        if (beacon.IsFunctional && beacon.Enabled)
                                        {
                                            ship.ClassName = def.Name;
                                            ship.HasWorkingBeacon = true;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (DoChecks(__instance, DefinedClasses[ship.ClassName], ship))
                            {
                                return true;
                            }
                            else
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
