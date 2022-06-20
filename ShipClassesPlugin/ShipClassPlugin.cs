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
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using SpaceEngineers.Game.Entities.Blocks;
using VRageMath;
using Sandbox.Engine.Physics;
using VRage.Game.Components;
using System.Collections.Concurrent;
using Torch.Managers;
using Torch.API.Plugins;
using ShipClassesPlugin.LimitedActiveShips;
using Sandbox.Game.Entities.Character;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using System.Text;

namespace ShipClassesPlugin
{
    public class ShipClassPlugin : TorchPluginBase
    {
        public override void Init(ITorchBase torch)
        {

            base.Init(torch);
            SetupConfig();

        }
        public static void SendMessage(string author, string message, Color color, long steamID)
        {
            Logger _chatLog = LogManager.GetLogger("Chat");
            ScriptedChatMsg scriptedChatMsg1 = new ScriptedChatMsg();
            scriptedChatMsg1.Author = author;
            scriptedChatMsg1.Text = message;
            scriptedChatMsg1.Font = "White";
            scriptedChatMsg1.Color = color;
            scriptedChatMsg1.Target = Sync.Players.TryGetIdentityId((ulong)steamID);
            ScriptedChatMsg scriptedChatMsg2 = scriptedChatMsg1;
            MyMultiplayerBase.SendScriptedChatMessage(ref scriptedChatMsg2);
        }
        public static string path;
        public static Config config;

        [ProtoContract]
        public class ItemsMessage
        {
            [ProtoMember(1)]
            public long EntityId { get; set; }
            [ProtoMember(2)]
            public long SendingPlayerID { get; set; }
        }
        [ProtoContract]
        public class SpeedMessage
        {
            [ProtoMember(1)]
            public long EntityId { get; set; }
            [ProtoMember(2)]
            public double WarpSpeed { get; set; }
        }

        public static void YeetThisFuckingDrive(long entityId)
        {
            SpeedMessage data = new SpeedMessage();
            data.EntityId = entityId;
            data.WarpSpeed = -1;

            MyAPIGateway.Multiplayer.SendMessageToServer(4378, MyAPIGateway.Utilities.SerializeToBinary<SpeedMessage>(data));
        }


        private TorchSessionManager sessionManager;
        public static FileUtils utils = new FileUtils();
        public static Boolean LoadedFiles = false;
        public void SetupConfig()
        {

            path = StoragePath;
            Directory.CreateDirectory(StoragePath + "\\ShipClasses\\");
            Directory.CreateDirectory(StoragePath + "\\ShipClasses\\ShipConfigs");
            if (File.Exists(StoragePath + "\\ShipClasses\\config.xml"))
            {
                config = utils.ReadFromXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml");
                utils.WriteToXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml", config, false);
            }
            else
            {
                config = new Config();
                utils.WriteToXmlFile<Config>(StoragePath + "\\ShipClasses\\config.xml", config, false);
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
                dee.MaximumPoints = 2;
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

        public static void EntityCreated(MyEntity entity)
        {

            //  if (entity is MySlimBlock block)
            //   {
            //      if (block.BlockDefinition.BlockPairName.Equals("FSDrive")){
            //         YeetThisFuckingDrive(entity.EntityId);
            //         Log.Info("YEET THIS FUCKING DRIVE");
            //     }

            //   }
            if (entity is MyCubeGrid grid)
            {
                //   Log.Info("GRID PASTE");
                foreach (MyUpgradeModule block in grid.GetFatBlocks().OfType<MyUpgradeModule>())
                {
                    if (block.BlockDefinition.BlockPairName.Equals("FSDrive"))
                    {
                        YeetThisFuckingDrive(entity.EntityId);
                        //      Log.Info("YEET THIS FUCKING DRIVE");
                    }
                }
            }
            //if (entity is IMyUpgradeModule block)
            //{
            //  if (block.BlockDefinition.SubtypeName.Contains("FSDrive"))
            //    {
            //        YeetThisFuckingDrive(entity.EntityId);
            //        Log.Info("YEET THIS FUCKING DRIVE");
            //    }

            //}
        }
        public static MethodInfo GetAllianceLimit;
        public static MethodInfo GetAllianceId;
        public static Boolean AlliancePluginEnabled = false;
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            if (state == TorchSessionState.Loaded)
            {
                Sandbox.Game.Entities.MyEntities.OnEntityAdd += new Action<MyEntity>(EntityCreated);


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

                if (session.Managers.GetManager<PluginManager>().Plugins.TryGetValue(Guid.Parse("74796707-646f-4ebd-8700-d077a5f47af3"), out ITorchPlugin All))
                {
                    Type alli = All.GetType().Assembly.GetType("AlliancesPlugin.Integrations");
                    try
                    {
                        GetAllianceLimit = All.GetType().GetMethod("GetMaximumForShipClassType", BindingFlags.Public | BindingFlags.Static, null, new Type[2] { typeof(string), typeof(string) }, null);
                        GetAllianceId = All.GetType().GetMethod("GetAllianceId", BindingFlags.Public | BindingFlags.Static, null, new Type[1] { typeof(string) }, null);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Error loading the alliance integration");

                    }
                    AlliancePluginEnabled = true;
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
            static int count = 0;

            public static Boolean DoActiveChecks(MyFunctionalBlock block, ShipClassDefinition shipDefinition, LiveShip ship)
            {
                if (!ActiveLimitsHandler.CanShipBeActive(block.GetOwnerFactionTag(), shipDefinition, block.CubeGrid.EntityId))
                {
                    BoundingSphereD sphere = new BoundingSphereD(block.CubeGrid.PositionComp.GetPosition(), 5000);
                    foreach (MyCharacter character in MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere).OfType<MyCharacter>())
                    {
                        SendMessage("Ship Classes", $"Ship {block.CubeGrid.DisplayNameText} could not be activated. Maximum active limit met.", Color.Red, (long)character.ControlSteamId);
                    }
                    return false;
                }

                return true;
            }

            public static Boolean DoChecks(MyFunctionalBlock block, ShipClassDefinition shipDefinition, LiveShip ship)
            {

                //todo move this to patch beacon turn on 

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
            static ConcurrentDictionary<long, Vector3> AccelForces = new ConcurrentDictionary<long, Vector3>();
            static Dictionary<long, int> UpdateTicks = new Dictionary<long, int>();

            public static Boolean KeepDisabled(MyFunctionalBlock __instance)
            {
                //  Log.Info("1");
                //  var speed = __instance.CubeGrid.Physics.Speed;
                //  var maxSpeed = 10;
                //  if (speed > maxSpeed)
                //  {
                //      //var resistance = 50f * (__instance.CubeGrid.Physics.Mass * 2 )* (1 - (maxSpeed / speed));
                //      //Vector3 velocity = __instance.CubeGrid.Physics.LinearVelocity * -resistance;

                ////      __instance.CubeGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, velocity, __instance.CubeGrid.Physics.CenterOfMassWorld, null, 10);

                //      var grid = __instance.CubeGrid;
                //      bool doSpeedUpdate = false;
                //      if (UpdateTicks.TryGetValue(grid.EntityId, out int ticks))
                //      {
                //          if (ticks == 10)
                //          {
                //              UpdateTicks[grid.EntityId] = 1;
                //              doSpeedUpdate = true;
                //          }
                //          else
                //          {
                //              UpdateTicks[grid.EntityId] += 1;
                //          }
                //      }
                //      else
                //      {
                //          UpdateTicks.Add(grid.EntityId, 1);
                //          doSpeedUpdate = true;
                //      }
                //  }
                if (!LoadedFiles)
                {
                    return true;
                }
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

                    if (DateTime.Now >= ship.NextCheck)
                    {
                        ship.NextCheck = DateTime.Now.AddSeconds(config.SecondsBetweenBeaconChecks);
                        //first check if it has a pilot before doing this
                        if (DoActiveChecks(__instance, DefinedClasses[ship.ClassName], ship))
                        {
                            ship.KeepDisabled = false;
                        }
                        else
                        {
                            ship.KeepDisabled = true;
                            __instance.Enabled = false;
                            return false;
                        }
                        if (ship.RequiresPilot && !ship.HasPilot)
                        {

                            __instance.Enabled = false;
                            return false;
                        }
                        var Beacons = __instance.CubeGrid.GetFatBlocks().OfType<MyBeacon>();
                        ship.HasWorkingBeacon = false;

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

                    if (ship.KeepDisabled)
                    {
                        __instance.Enabled = false;
                        return false;
                    }

                    if (ship.HasWorkingBeacon)
                    {
                        if (DoChecks(__instance, DefinedClasses[ship.ClassName], ship))
                        {
                            return true;
                        }
                        else
                        {
                            ship.HasWorkingBeacon = false;
                            __instance.Enabled = false;
                            return false;
                        }
                    }
                    ActiveShips[__instance.CubeGrid.EntityId] = ship;
        

                }
                else
                {
                    if (LimitedBlocks.TryGetValue(__instance.BlockDefinition.BlockPairName, out List<String> ShipClasses))
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
                                            newShip.HasToBeStation = def.HasToBeStation;
                                            ActiveShips.Remove(newShip.GridEntityId);
                                            ActiveShips.Add(newShip.GridEntityId, newShip);
                                            break;
                                        }
                                    }
                                }
                            }
                            if (newShip.HasWorkingBeacon)
                            {
                                if (!DoActiveChecks(__instance, DefinedClasses[ship.ClassName], ship))
                                {
                                    ship.KeepDisabled = true;
                                    __instance.Enabled = false;
                                }
                                if (DoChecks(__instance, DefinedClasses[newShip.ClassName], newShip))
                                {

                                    return true;
                                }
                                else
                                {
                                    newShip.KeepDisabled = true;
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

