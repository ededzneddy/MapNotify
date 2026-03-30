using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using nuVector4 = System.Numerics.Vector4;

namespace MapNotify
{
    public partial class MapNotify : BaseSettingsPlugin<MapNotifySettings>
    {
        public static nuVector4 GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Rare:
                    return new nuVector4(0.99f, 0.99f, 0.46f, 1f);
                case ItemRarity.Magic:
                    return new nuVector4(0.68f, 0.68f, 1f, 1f);
                case ItemRarity.Unique:
                    return new nuVector4(1f, 0.50f, 0.10f, 1f);
                default:
                    return new nuVector4(1F, 1F, 1F, 1F);
            }
        }

        public static readonly List<string> ModNameBlacklist = new List<string>(){
            "AfflictionMapDeliriumStacks",
            "AfflictionMapReward",
            "InfectedMap",
            "MapForceCorruptedSideArea",
            "MapGainsRandomZanaMod",
            "MapDoesntConsumeSextantCharge",
            "MapEnchant",
            "Enchantment",
            "MapBossSurroundedByTormentedSpirits",
            "MapZanaSubAreaMissionDetails",
        };

        public class StyledText
        {
            public string Text { get; set; }
            public Vector4 Color { get; set; }
            public bool Bricking { get; set; }
        }

        public class ItemDetails
        {
            public ItemDetails(NormalInventoryItem Item, Entity Entity)
            {
                this.Item = Item;
                this.Entity = Entity;
                ActiveWarnings = new List<StyledText>();
                Update();
            }

            public NormalInventoryItem Item { get; }
            public Entity Entity { get; }
            public List<StyledText> ActiveWarnings { get; set; }
            public nuVector4 ItemColor { get; set; }
            public string MapName { get; set; }
            public string ClassID { get; set; }
            public int PackSize { get; set; }
            public int Quantity { get; set; }
            public int Currency { get; set; }
            public int Scarabs  { get; set; }
            public int MapDrop  { get; set; }
            public int ModCount { get; set; }
            public bool NeedsPadding { get; set; }
            public bool Bricked { get; set; }
            public bool Corrupted { get; set; }
            public bool IsEightMod { get; set; }
            public int Tier { get; set; }

            public void Update()
            {
                var BaseItem = gameController.Files.BaseItemTypes.Translate(Entity.Path);
                var ItemName = BaseItem.BaseName;
                ClassID = BaseItem.ClassName;

                var packSize = 0;
                var quantity = Entity.GetComponent<Quality>()?.ItemQuality ?? 0;
                var currency = 0;
                var scarabs  = 0;
                var mapDrop  = 0;
                var settings = MapNotify.LiveSettings ?? new MapNotifySettings();
                var mapComponent = Entity.GetComponent<MapKey>() ?? null;
                Tier = mapComponent?.Tier ?? -1;
                NeedsPadding = Tier != -1;
                Bricked = false;
                Corrupted = Entity.GetComponent<Base>()?.isCorrupted ?? false;

                var modsComponent = Entity.GetComponent<Mods>() ?? null;
                ModCount = modsComponent?.ItemMods.Count() ?? 0;

                // Build map name: [T13] Ravaged Morass
                if (mapComponent != null && modsComponent != null)
                {
                    var rareName = modsComponent.UniqueName;
                    if (!string.IsNullOrEmpty(rareName))
                        MapName = $"[T{mapComponent.Tier}] {rareName}";
                    else
                        MapName = $"[T{mapComponent.Tier}]";
                }
                else
                {
                    MapName = ItemName;
                }

                if (modsComponent != null && ModCount > 0)
                {
                    if (modsComponent != null && modsComponent.ItemRarity != ItemRarity.Unique)
                    {
                        var realModCount = 0;
                        foreach (var mod in modsComponent.ItemMods.Where(x =>
                                                !x.Group.Contains("MapAtlasInfluence")))
                        {
                            if (ModNameBlacklist.Any(m => mod.RawName.Contains(m)))
                            {
                                ModCount--;
                                continue;
                            }

                            realModCount++;

                            UpdateValueIfStatExists("map_pack_size_+%",                          x => packSize += x);
                            UpdateValueIfStatExists("map_item_drop_quantity_+%",                  x => quantity += x);
                            UpdateValueIfStatExists("map_currency_drop_chance_+%_final_from_uber_mod", x => currency += x);
                            UpdateValueIfStatExists("map_scarab_drop_chance_+%_final_from_uber_mod",   x => scarabs  += x);
                            UpdateValueIfStatExists("map_map_item_drop_chance_+%_final_from_uber_mod", x => mapDrop  += x);

                            // Profile-driven warnings
                            if (MapNotify.LiveSettings?.EnabledMods != null && MapNotify._modEntries != null)
                            {
                                foreach (var entry in MapNotify._modEntries)
                                {
                                    if (!MapNotify.LiveSettings.EnabledMods.TryGetValue(entry.ModType, out var enabled) || !enabled)
                                        continue;
                                    if (!mod.RawName.Contains(entry.ModType, System.StringComparison.OrdinalIgnoreCase))
                                        continue;
                                    if (ActiveWarnings.Any(w => w.Text == entry.Name))
                                        continue;

                                    bool isBricked = MapNotify.LiveSettings.BrickedMods.TryGetValue(entry.ModType, out var br) && br;
                                    bool isGood    = !isBricked && (MapNotify.LiveSettings.GoodMods?.TryGetValue(entry.ModType, out var gd) == true && gd);
                                    if (isBricked) Bricked = true;

                                    // Pull colors from settings so tooltip always matches border colors
                                    var s = MapNotify.LiveSettings;
                                    SharpDX.Vector4 textColor;
                                    if (isBricked)
                                        textColor = new SharpDX.Vector4(s.Bricked.X, s.Bricked.Y, s.Bricked.Z, s.Bricked.W);
                                    else if (isGood)
                                        textColor = new SharpDX.Vector4(s.GoodModBorder.X, s.GoodModBorder.Y, s.GoodModBorder.Z, s.GoodModBorder.W);
                                    else
                                        textColor = new SharpDX.Vector4(s.MapBorderWarnings.X, s.MapBorderWarnings.Y, s.MapBorderWarnings.Z, s.MapBorderWarnings.W);

                                    ActiveWarnings.Add(new StyledText
                                    {
                                        Text     = entry.Name,
                                        Color    = textColor,
                                        Bricking = isBricked
                                    });
                                }
                            }


                            void UpdateValueIfStatExists(string key, Action<int> updateAction)
                            {
                                var index = mod.ModRecord.StatNames
                                    .Select((value, index) => new { value, index })
                                    .FirstOrDefault(pair => pair.value.Key == key)?.index ?? -1;

                                if (index != -1)
                                {
                                    updateAction(mod.Values[index]);
                                }
                            }
                        }

                        IsEightMod = realModCount >= 8;
                    }

                    Quantity = quantity;
                    PackSize = packSize;
                    Currency = currency;
                    Scarabs  = scarabs;
                    MapDrop  = mapDrop;

                    if (ClassID.Contains("MapFragment"))
                    {
                        MapName = ItemName;
                        NeedsPadding = true;
                    }

                    ItemColor = GetRarityColor(modsComponent?.ItemRarity ?? ItemRarity.Normal);
                }
            }
        }
    }
}
