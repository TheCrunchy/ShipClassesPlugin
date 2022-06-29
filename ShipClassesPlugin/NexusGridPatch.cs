using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;

namespace ShipClassesPlugin
{


    [PatchShim]
    public class NexusGridPatch
    {
        internal static readonly MethodInfo update =
           typeof(Nexus.BoundarySystem.GridTransport).GetMethod("PrepareGrids", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");
        internal static readonly MethodInfo updatePatch =
typeof(NexusGridPatch).GetMethod(nameof(DisableDrive), BindingFlags.Static | BindingFlags.Public) ??
throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(update).Prefixes.Add(updatePatch);
        }


        public static Boolean DisableDrive(List<MyCubeGrid> Grids, bool AutoSend = true)
        {
            foreach (var grid in Grids)
            {
                foreach (MyUpgradeModule mod in grid.GetFatBlocks().OfType<MyUpgradeModule>())
                {
                    if (mod.BlockDefinition.BlockPairName.Equals("FSDrive"))
                    {
                        //SpeedMessage data = new SpeedMessage();
                        //data.EntityId = mod.EntityId;
                        //data.WarpSpeed = -1;

                        //MyAPIGateway.Multiplayer.SendMessageToServer(4378, MyAPIGateway.Utilities.SerializeToBinary<SpeedMessage>(data));
                        //ShipClassPlugin.Log.Info("Trying to yeet the drive.");
                        if (mod.Enabled) {
                            mod.Enabled = false;
                            return false;
                        }
                    }
                }
            }
            return true;
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
    }
}
