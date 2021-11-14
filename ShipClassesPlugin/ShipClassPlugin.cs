using Sandbox.Game.Entities.Cube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Managers.PatchManager;
using Torch.Session;
using Torch.API.Managers;
using static ShipClassesPlugin.ShipClassDefinition;
using NLog;

namespace ShipClassesPlugin
{
    public class ShipClassPlugin : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            SetupConfig();

        }
        public static string path;
        public static Config config;

        private TorchSessionManager sessionManager;
        public static FileUtils utils = new FileUtils();
        public static Boolean LoadedFiles = false;
        public void SetupConfig()
        {

            path = StoragePath;
            Directory.CreateDirectory(StoragePath + "\\ShipClasses\\");
            if (File.Exists(StoragePath + "\\ShipClasses\\config.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml", config, false);
                Directory.CreateDirectory(StoragePath + "\\ShipClasses\\ShipConfigs");
            }
            if (!File.Exists(StoragePath + "\\ShipClasses\\ShipConfigs\\example.xml"))
            {
                ShipClassDefinition def = new ShipClassDefinition();
                def.Name = "Example";
                def.Enabled = false;
                def.BeaconBlockPairName = ("Beacon");
                BlockId id = new BlockId();
                id.BlockPairName = "Gyroscope";
                BlocksDefinition dee = new BlocksDefinition();
                dee.blocks.Add(id);
                dee.MaximumAmount = 2;
                dee.BlocksDefinitionName = "GYROSCOPE";
                def.DefinedBlocks.Add(dee);
                utils.WriteToXmlFile<ShipClassDefinition>(StoragePath + "\\ShipClasses\\ShipConfigs\\example.xml", def, false);
            }
            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
            //throw new NotImplementedException();
        }
        public static void ReloadClasses()
        {
            config = utils.ReadFromXmlFile<Config>(path + "\\ShipClasses\\config.xml");
            ActiveShips.Clear();
            DefinedClasses.Clear();
            foreach (String s in Directory.GetFiles(path + "\\ShipClasses\\ShipConfigs\\"))
            {
                try
                {
                    ShipClassDefinition def = utils.ReadFromXmlFile<ShipClassDefinition>(s);
                    if (def.Enabled)
                    {
                        def.SetupDefinedBlocks();
                        if (!DefinedClasses.ContainsKey(def.Name))
                        {
                            DefinedClasses.Add(def.Name, def);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error reading file " + s);
                    throw;
                }
            }
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (state == TorchSessionState.Loaded)
            {

                foreach (String s in Directory.GetFiles(path + "\\ShipClasses\\ShipConfigs\\"))
                {
                    try
                    {
                        ShipClassDefinition def = utils.ReadFromXmlFile<ShipClassDefinition>(s);
                        if (def.Enabled)
                        {
                            def.SetupDefinedBlocks();
                            if (!DefinedClasses.ContainsKey(def.Name))
                            {
                                DefinedClasses.Add(def.Name, def);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error reading file " + s);
                        throw;
                    }
                }

                LoadedFiles = true;
            }
        }


        //Load the config files into this dictionary
        public static Dictionary<String, ShipClassDefinition> DefinedClasses = new Dictionary<string, ShipClassDefinition>();

        //we store the active ships here, grid entity Id as the key
        public static Dictionary<long, LiveShip> ActiveShips = new Dictionary<long, LiveShip>();

        //blockpairname as key, the list is the name of the Classes that limit this block
        public static Dictionary<String, List<String>> LimitedBlocks = new Dictionary<string, List<string>>();


        public static Dictionary<long, Boolean> EnableTheBlock = new Dictionary<long, bool>();


        public static Logger Log = LogManager.GetLogger("ShipClass");
        public static DateTime oof = DateTime.Now;
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
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    //now we need to add to the ship since we havent had this block before.
                    if (ship.CanBlockFunction(block.BlockDefinition.BlockPairName))
                    {


                        EnableTheBlock.Add(block.EntityId, true);
                        ActiveShips[block.CubeGrid.EntityId] = ship;
                        return true;
                    }
                    else
                    {

                        ActiveShips[block.CubeGrid.EntityId] = ship;

                        EnableTheBlock.Add(block.EntityId, false);
                        block.Enabled = false;
                        return false;
                    }
                }
                return true;
            }

            public static Boolean KeepDisabled(MyFunctionalBlock __instance)
            {
                //  Log.Info("1");
                if (!LoadedFiles)
                {
                    return true;
                }
                // if (DateTime.Now >= oof)
                //  {
                //    oof = oof.AddSeconds(30);
                //  foreach (KeyValuePair<String, List<String>> pair in LimitedBlocks)
                //  {
                //     Log.Info(pair.Key);
                //     foreach (String s in pair.Value)
                //    {
                //      Log.Info(s);
                //  }
                //   }
                //   }
                if (LimitedBlocks.TryGetValue(__instance.BlockDefinition.BlockPairName, out List<String> ShipClasses))
                {
                    //  Log.Info("HAS LIMITED BLOCK");
                    if (ActiveShips.TryGetValue(__instance.CubeGrid.EntityId, out LiveShip ship))
                    {
                        if (ship.HasToBeStation)
                        {
                            if (!__instance.CubeGrid.IsStatic)
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                        else
                        {
                            if (__instance.CubeGrid.IsStatic)
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }

                       
                        //do any checks here for if the grid has a pilot
                        if (DateTime.Now >= ship.NextCheck)
                        {

                            //first check if it has a pilot before doing this
                            if (ship.RequiresPilot && !ship.HasPilot)
                            {
                                ship.NextCheck = DateTime.Now.AddSeconds(config.SecondsBetweenBeaconChecks);
                                __instance.Enabled = false;
                                return false;
                            }
                            var Beacons = __instance.CubeGrid.GetFatBlocks().OfType<MyBeacon>();
                            ship.HasWorkingBeacon = false;

                            //so we need to recheck if the grid still has a working beacon with our class ID
                            //should probably change the seconds to a value from the config file
                            ship.NextCheck = DateTime.Now.AddSeconds(config.SecondsBetweenBeaconChecks);
                            ShipClassDefinition shipClass = DefinedClasses[ship.ClassName];
                            //   foreach (BlocksDefinition defin in shipClass.DefinedBlocks)
                            //    {
                            //       if (ship.UsedLimitsPerDefinition.ContainsKey(defin.BlocksDefinitionName))
                            //       {
                            //           ship.UsedLimitsPerDefinition[defin.BlocksDefinitionName] = 0;
                            //      }
                            //  }
                            //  EnableTheBlock.Remove(__instance.EntityId);
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
                            ActiveShips[__instance.CubeGrid.EntityId] = ship;
                            if (ship.HasWorkingBeacon)
                            {
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
                            else
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                        else
                        {
                            //so we are failing here, it isnt properly checking limits
                            if (ship.HasWorkingBeacon)
                            {
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
                            else
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        //We are registering a new ship here, this is when you should check block counts

                        LiveShip newShip = new LiveShip();
                        newShip.NextCheck = DateTime.Now.AddSeconds(config.SecondsBetweenBeaconChecks);
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
                                            newShip.ClassName = def.Name;
                                            newShip.RequiresPilot = def.RequiresPilot;
                                            newShip.HasWorkingBeacon = true;
                                            ActiveShips.Remove(newShip.GridEntityId);
                                            ActiveShips.Add(newShip.GridEntityId, newShip);
                                            newShip.HasToBeStation = def.HasToBeStation;
                                            break;
                                        }
                                    }
                                }
                            }
                            if (newShip.HasWorkingBeacon)
                            {
                                if (DoChecks(__instance, DefinedClasses[newShip.ClassName], newShip))
                                {
                                    return true;
                                }
                                else
                                {
                                    __instance.Enabled = false;
                                    return false;
                                }
                            }
                            else
                            {
                                __instance.Enabled = false;
                                return false;
                            }
                        }
                    }
                }
                return true;
            }

        }
    }
}

