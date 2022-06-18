using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipClassesPlugin
{
    public class ShipClassDefinition
    {
        public Boolean Enabled = true;
        public string Name = "Example";
        public string ClassType = "Frigate";
        public string ClassSubtype = "Frigate";
        public int ActiveLimitAmount = 5;
        public int ClassPriority = 1;
        public string BeaconBlockPairName = "SUPERDUPERBEACON";
        public List<BlocksDefinition> DefinedBlocks = new List<BlocksDefinition>();
        private Dictionary<string, BlocksDefinition> _DefinedBlocks = new Dictionary<string, BlocksDefinition>();
        private Dictionary<string, int> LimitsForBlocks = new Dictionary<string, int>();
        public Boolean RequiresPilot = false;
        public Boolean HasToBeStation = false;
        //run this after loading the file
        
        public void SetupDefinedBlocks()
        {
            _DefinedBlocks.Clear();
            foreach (BlocksDefinition def in DefinedBlocks)
            {
                if (!_DefinedBlocks.ContainsKey(def.BlocksDefinitionName))
                {
                    _DefinedBlocks.Add(def.BlocksDefinitionName, def);
                    foreach (BlockId id in def.blocks)
                    {
                        LimitsForBlocks.Add(id.BlockPairName, def.MaximumPoints);
                        //Also add these to a static dictionary that stores a list using the blocks pair name as key, the list should store the Names for the classes
                        //if a gun works with multiple beacons, we want that to work 
                        if (ShipClassPlugin.LimitedBlocks.TryGetValue(id.BlockPairName, out List<String> tee))
                        {
                            tee.Add(Name);
                            ShipClassPlugin.LimitedBlocks[id.BlockPairName] = tee;
                        }
                        else
                        {
                            List<String> temp = new List<string>();
                            temp.Add(Name);
                            ShipClassPlugin.LimitedBlocks.Add(id.BlockPairName, temp);
                        }
                    }
                }
            }
        }

        public int GetLimitForBlock(String BlockPairName)
        {
            if (LimitsForBlocks.TryGetValue(BlockPairName, out int max))
            {
                return max;
            }

            return 50000;
        }

        public BlocksDefinition GetBlocksDefinition(String BlocksDefinitionName)
        {
            if (_DefinedBlocks.TryGetValue(BlocksDefinitionName, out BlocksDefinition def))
            {
                return def;
            }
            return null;
        }

        public class BlocksDefinition
        {
            public int MaximumPoints = 10;
            public string BlocksDefinitionName = "Example Small Guns";
            public List<BlockId> blocks = new List<BlockId>();
        }

        public class BlockId
        {
            //So this doesnt really need to be a class, but i suppose keeping it as one would be easier for future proofing if we wanted to change any of this
            //THANKS PAST ME FOR KEEPING THIS AS A CLASS, I DID END UP CHANGING IT
            public string BlockPairName = "Example PairName";
            public int points = 1;
        }
    }
}
