using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AdvancedMod.Items.Misc
{
    public class CaughtNPCItem : ModItem
    {
        public static Dictionary<int, int> CaughtTownies = new Dictionary<int, int>();

        public override string Name => _name;

        public string _name;
        public int AssociatedNpcId;
        public string NpcQuote;

        public CaughtNPCItem()
        {
            _name = base.Name;
            AssociatedNpcId = NPCID.None;
            NpcQuote = "";
        }

        public CaughtNPCItem(string internalName, int associatedNpcId, string npcQuote = "")
        {
            _name = internalName;
            AssociatedNpcId = associatedNpcId;
            NpcQuote = npcQuote;
        }

        public override bool IsLoadingEnabled(Mod mod) => AssociatedNpcId != NPCID.None;

        protected override bool CloneNewInstances => true;

        public override bool IsCloneable => true;

        public override void Unload()
        {
            base.Unload();

            CaughtTownies.Clear();
        }


        public override string Texture => AssociatedNpcId < NPCID.Count
            ? $"Terraria/Images/NPC_{AssociatedNpcId}"
            : NPCLoader.GetNPC(AssociatedNpcId).Texture;

        public override void SetStaticDefaults()
        {
            //DisplayName.SetDefault(Regex.Replace(_name, "([A-Z])", " $1").Trim());
            DisplayName.SetDefault(_name);
            Tooltip.SetDefault(NpcQuote);
            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, Main.npcFrameCount[AssociatedNpcId]));
            ItemID.Sets.AnimatesAsSoul[Item.type] = true;

            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Item.type] = 5;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 10;
            Item.rare = ItemRarityID.Blue;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.consumable = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item44;
            Item.makeNPC = AssociatedNpcId;

            switch (AssociatedNpcId)
            {
                case NPCID.Angler:
                    Item.bait = 15;
                    break;

                case NPCID.None:
                    return;
            }
        }

        public override void PostUpdate()
        {
            if (AssociatedNpcId != NPCID.Guide || !Item.lavaWet || NPC.AnyNPCs(NPCID.WallofFlesh))
                return;

            NPC.SpawnWOF(Item.position);
            Item.TurnToAir();
        }

        public override bool CanUseItem(Player player)
        {
            //mirroring vanilla checks
            bool inRange = player.position.X / 16 - Player.tileRangeX - Item.tileBoost <= Player.tileTargetX
                && (player.position.X + player.width) / 16 + Player.tileRangeX + Item.tileBoost - 1 >= Player.tileTargetX
                && player.position.Y / 16 - Player.tileRangeY - Item.tileBoost <= Player.tileTargetY
                && (player.position.Y + player.height) / 16 + Player.tileRangeY + Item.tileBoost - 2 >= Player.tileTargetY;

            return inRange && !WorldGen.SolidTile((int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16) && NPC.CountNPCS(AssociatedNpcId) < 5;
        }

        public override bool? UseItem(Player player) => true;

        public static void RegisterItems(Mod mod)
        {
            CaughtTownies = new Dictionary<int, int>();

            Add("观察者", ModContent.NPCType<NPCs.Town.Watcher>(), "魔眼之神已经陨落了……");
            Add("异变", ModContent.NPCType<NPCs.Town.Mutation>(), "我想我还是要保持我的初心，找我那个大哥打一架");
            Add("疯子", ModContent.NPCType<NPCs.Town.Madman>(), "……");
        }

        public static void Add(string internalName, int id, string quote, Mod mod = null)
        {
            if (mod == null)
                mod = ModLoader.GetMod("AdvancedModDLC");

            CaughtNPCItem item = new(internalName, id, quote);
            mod.AddContent(item);
            CaughtTownies.Add(id, item.Type);
        }

        //        public static void AddAutomatic(string name, int id, Mod mod = null) => Add($"Caught{name}", id, "", mod);
    }

    public class CaughtGlobalNPC : GlobalNPC
    {
        public override void SetDefaults(NPC npc)
        {
            if (CaughtNPCItem.CaughtTownies.ContainsKey(npc.type) && ModContent.GetInstance<AdvancedConfig>().CanCatchNPC)
            {
                npc.catchItem = (short)CaughtNPCItem.CaughtTownies.FirstOrDefault(x => x.Key.Equals(npc.type)).Value;
                Main.npcCatchable[npc.type] = true;
            }
        }
    }
}
