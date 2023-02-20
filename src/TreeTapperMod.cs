using Vintagestory.API.Common;

[assembly: ModInfo("treetapping",
    Description = "Tree Tapping!",
    Authors = new[]{ "ChaOS25" })]
namespace TreeTapper {
    class TreeTapperMod: ModSystem {

        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockBehaviorClass("AttachRestricted", typeof(BehaviorAttachRestricted));
        }
    }
}