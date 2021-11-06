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
    }
}
