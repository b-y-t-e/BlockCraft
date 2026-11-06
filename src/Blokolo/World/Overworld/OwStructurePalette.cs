namespace Blokolo.World.Structures18;
public static class Blocks
{
 public static readonly Block[] ById = Enumerable.Range(0,256).Select(i=>new Block(i)).ToArray();
 public static readonly Block air = ById[B.Air];
 public static readonly Block bookshelf = ById[B.Bookshelf];
 public static readonly Block carrots = ById[B.Carrots];
 public static readonly Block cobblestone = ById[B.Cobblestone];
 public static readonly Block crafting_table = ById[B.CraftingTable];
 public static readonly Block dirt = ById[B.Dirt];
 public static readonly Block double_stone_slab = ById[B.DoubleStoneSlab];
 public static readonly Block end_portal_frame = ById[B.EndPortalFrame];
 public static readonly Block farmland = ById[B.Farmland];
 public static readonly Block flowing_lava = ById[B.Lava];
 public static readonly Block flowing_water = ById[B.Water];
 public static readonly Block furnace = ById[B.Furnace];
 public static readonly Block glass_pane = ById[B.GlassPane];
 public static readonly Block gold_block = ById[B.GoldBlock];
 public static readonly Block gravel = ById[B.Gravel];
 public static readonly Block iron_bars = ById[B.IronBars];
 public static readonly Block iron_door = ById[B.IronDoor];
 public static readonly Block ladder = ById[B.Ladder];
 public static readonly Block log = ById[B.OakLog];
 public static readonly Block log2 = ById[B.AcaciaLog];
 public static readonly Block mob_spawner = ById[B.MobSpawner];
 public static readonly Block monster_egg = ById[B.MonsterEgg];
 public static readonly Block oak_door = ById[B.OakDoor];
 public static readonly Block oak_fence = ById[B.Fence];
 public static readonly Block oak_stairs = ById[B.OakStairs];
 public static readonly Block obsidian = ById[B.Obsidian];
 public static readonly Block planks = ById[B.OakPlanks];
 public static readonly Block potatoes = ById[B.Potatoes];
 public static readonly Block prismarine = ById[B.Prismarine];
 public static readonly Block sandstone = ById[B.Sandstone];
 public static readonly Block sandstone_stairs = ById[B.SandstoneStairs];
 public static readonly Block sapling = ById[B.OakSapling];
 public static readonly Block sea_lantern = ById[B.SeaLantern];
 public static readonly Block sponge = ById[B.Sponge];
 public static readonly Block stone_brick_stairs = ById[B.StoneBrickStairs];
 public static readonly Block stone_button = ById[B.StoneButton];
 public static readonly Block stone_slab = ById[B.StoneSlab];
 public static readonly Block stone_stairs = ById[B.CobbleStairs];
 public static readonly Block stonebrick = ById[B.StoneBricks];
 public static readonly Block torch = ById[B.Torch];
 public static readonly Block water = ById[B.Water];
 public static readonly Block web = ById[B.Cobweb];
 public static readonly Block wheat = ById[B.Wheat];
 public static readonly Block wooden_pressure_plate = ById[B.WoodPressurePlate];
 public static readonly Block wool = ById[B.Wool];
}
public static class Items
{
 public const int apple = Blokolo.Items.I.Apple;
 public const int book = Blokolo.Items.I.Book;
 public const int bread = Blokolo.Items.I.Bread;
 public const int coal = Blokolo.Items.I.Coal;
 public const int compass = Blokolo.Items.I.Compass;
 public const int diamond = Blokolo.Items.I.Diamond;
 public const int diamond_horse_armor = Blokolo.Items.I.DiamondHorseArmor;
 public const int ender_pearl = Blokolo.Items.I.EnderPearl;
 public const int gold_ingot = Blokolo.Items.I.GoldIngot;
 public const int golden_apple = Blokolo.Items.I.GoldenApple;
 public const int golden_horse_armor = Blokolo.Items.I.GoldHorseArmor;
 public const int iron_boots = Blokolo.Items.I.IronBoots;
 public const int iron_chestplate = Blokolo.Items.I.IronChest;
 public const int iron_helmet = Blokolo.Items.I.IronHelmet;
 public const int iron_horse_armor = Blokolo.Items.I.IronHorseArmor;
 public const int iron_ingot = Blokolo.Items.I.IronIngot;
 public const int iron_leggings = Blokolo.Items.I.IronLegs;
 public const int iron_pickaxe = Blokolo.Items.I.IronPickaxe;
 public const int iron_sword = Blokolo.Items.I.IronSword;
 public const int map = Blokolo.Items.I.EmptyMap;
 public const int paper = Blokolo.Items.I.Paper;
 public const int redstone = Blokolo.Items.I.Redstone;
 public const int saddle = Blokolo.Items.I.Saddle;
 public static readonly BookLoot enchanted_book = new();
 public class BookLoot
 {
  public WeightedRandomChestContent getRandom(JRandom r, int min = 1, int max = 1, int weight = 1) { var es = Enum.GetValues<Blokolo.Items.Ench>(); var e = es[r.NextInt(es.Length)]; int level = 1 + r.NextInt(Blokolo.Items.EnchantHelper.MaxLevel(e)); return new WeightedRandomChestContent(Blokolo.Items.I.EnchantedBook,0,min,max,weight) { Enchantment = e, Level = level }; }
 }
}
