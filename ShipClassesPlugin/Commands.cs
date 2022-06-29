using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace ShipClassesPlugin
{
    [Category("shipclass")]
    public class Commands : CommandModule
    {
        [Command("reload", "Reload the config")]
        [Permission(MyPromoteLevel.Admin)]
        public void ReloadConfig()
        {
            ShipClassPlugin.ReloadClasses();

            Context.Respond("Reloaded config and cleared active ships");
        }

        [ProtoContract]
        public class SpeedMessage
        {
            [ProtoMember(1)]
            public long EntityId { get; set; }
            [ProtoMember(2)]
            public double WarpSpeed { get; set; }
        }

        [Command("yeet", "Yeet the grid to insane speed")]
        [Permission(MyPromoteLevel.Admin)]
        public void YeetGrid(double speed)
        {
            MyPlayer player = Context.Player as MyPlayer;
            if (player?.Controller?.ControlledEntity is MyCockpit controller)
            {
                foreach (MyUpgradeModule mod in controller.CubeGrid.GetFatBlocks().OfType<MyUpgradeModule>())
                {
                    if (mod.BlockDefinition.BlockPairName.Equals("FSDrive"))
                    {
                        SpeedMessage data = new SpeedMessage();
                        data.EntityId = mod.EntityId;
                        data.WarpSpeed = speed;
                        Context.Respond("found a drive");
                        MyAPIGateway.Multiplayer.SendMessageToServer(4378, MyAPIGateway.Utilities.SerializeToBinary<SpeedMessage>(data));
                    }
                }
            }
                Context.Respond("YEET");
        }
    }
}
