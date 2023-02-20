using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

class BehaviorAttachRestricted: BlockBehavior {

    AssetLocation[] allowedBlockCodes;
    bool handleDrops = true;
    string dropBlockFace = "north";
    string dropBlock = null;

    public BehaviorAttachRestricted(Block block): base(block) {

    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        string[] blockCodes = properties["allowedBlocks"].AsArray<string>(new string[0]);
        allowedBlockCodes = new AssetLocation[blockCodes.Length];

        for (int i = 0; i < blockCodes.Length; i++) {
            allowedBlockCodes[i] = AssetLocation.Create(blockCodes[i], block.Code.Domain);
        }

        handleDrops = properties["handleDrops"].AsBool(true);

        if (properties["dropBlockFace"].Exists) {
            dropBlockFace = properties["dropBlockFace"].AsString();
        }

        if (properties["dropBlock"].Exists) {
            dropBlock = properties["dropBlock"].AsString();
        }
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PreventDefault;

        BlockPos targetPos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
        Block targetBlock = world.BlockAccessor.GetBlock(targetPos);
        bool match = targetBlock.WildCardMatch(allowedBlockCodes);
        world.Logger.VerboseDebug("The match result is {0}", match);

        if (match == false) return false;

        if (IsBlockAlreadyAttached(world.BlockAccessor, targetPos, blockSel.Face, ref failureCode)) return false;

        // Prefer selected block face
        if (blockSel.Face.IsHorizontal)
        {
            if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;
        }

        // Otherwise attach to any possible face
        BlockFacing[] faces = BlockFacing.HORIZONTALS;
        blockSel = blockSel.Clone();
        for (int i = 0; i < faces.Length; i++)
        {
            blockSel.Face = faces[i];
            if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;
        }

        failureCode = "requirehorizontalattachable";

        return false;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
    {
        if (handleDrops)
        {
            handled = EnumHandling.PreventDefault;
            if (dropBlock != null)
            {
                return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock))) };
            }
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace))) };

        } else
        {
            handled = EnumHandling.PassThrough;
            return null;
        }
        
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        if (dropBlock != null)
        {
            return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)));
        }

        return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)));
    }


    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        if (!CanBlockStay(world, pos))
        {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }


    bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
    {
        BlockFacing oppositeFace = blockSel.Face.Opposite;

        BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
        Block attachingBlock = world.BlockAccessor.GetBlock(attachingBlockPos);

        Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(oppositeFace.Code));

        if (attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, blockSel.Face) && orientedBlock.CanPlaceBlock(world, player, blockSel, ref failureCode))
        {
            orientedBlock.DoPlaceBlock(world, player, blockSel, itemstack);
            return true;
        }

        return false;
    }

    bool IsBlockAlreadyAttached(IBlockAccessor world, BlockPos pos, BlockFacing face, ref string failureCode) {

        BlockFacing[] faces = BlockFacing.HORIZONTALS;
        
        for (int i = 0; i < faces.Length; i++) {
            if (faces[i] == face) continue; // Skip face we're aiming at

            BlockPos posToCheck = pos.AddCopy(faces[i]);
            Block blockToCheck = world.GetBlock(posToCheck);

            if (blockToCheck.CodeWithoutParts(1) == block.CodeWithoutParts(1)) {
                failureCode = "blockalreadyattached";
                return true;
            }
        }
        return false;
    }

    bool CanBlockStay(IWorldAccessor world, BlockPos pos)
    {
        string[] parts = block.Code.Path.Split('-');
        BlockFacing facing = BlockFacing.FromCode(parts[parts.Length - 1]);
        Block attachingblock = world.BlockAccessor.GetBlock(pos.AddCopy(facing));

        return attachingblock.CanAttachBlockAt(world.BlockAccessor, block, pos.AddCopy(facing), facing.Opposite);
    }


    public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
    {
        handled = EnumHandling.PreventDefault;
        return false;
    }


    public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        BlockFacing beforeFacing = BlockFacing.FromCode(block.LastCodePart());
        int rotatedIndex = GameMath.Mod(beforeFacing.HorizontalAngleIndex - angle / 90, 4);
        BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];

        return block.CodeWithParts(nowFacing.Code);
    }

    public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
    {
        handling = EnumHandling.PreventDefault;

        BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
        if (facing.Axis == axis)
        {
            return block.CodeWithParts(facing.Opposite.Code);
        }
        return block.Code;
    }
}