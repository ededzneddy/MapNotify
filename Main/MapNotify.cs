using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using MapKey = ExileCore.PoEMemory.Components.MapKey;
using nuVector2 = System.Numerics.Vector2;
using nuVector4 = System.Numerics.Vector4;

namespace MapNotify;

public partial class MapNotify : BaseSettingsPlugin<MapNotifySettings>
{
    private RectangleF windowArea;
    private static GameController gameController;
    private static IngameState ingameState;
    private CachedValue<List<NormalInventoryItem>> _inventoryItems;
    private CachedValue<(int stashIndex, List<NormalInventoryItem>)> _stashItems;
    private CachedValue<List<NormalInventoryItem>> _faustusItems;

    // Profile system (added)
    public static ProfileManager _profileManager;
    public static MapNotifySettings LiveSettings;
    public static string _newProfileNameBuffer = "";
    public static List<MapModEntry> _modEntries = new();

    public MapNotify()
    {
    }

    private List<NormalInventoryItem> GetInventoryItems()
    {
        var result = new List<NormalInventoryItem>();
        if (ingameState.IngameUi.InventoryPanel.IsVisible)
        {
            var items = ingameState.IngameUi.InventoryPanel[InventoryIndex.PlayerInventory].VisibleInventoryItems;
            if (items != null)
                result.AddRange(items.Where(item => item?.Item != null && item.Item.HasComponent<MapKey>()));
        }

        return result;
    }

    private (int stashIndex, List<NormalInventoryItem>) GetStashItems()
    {
        var result = new List<NormalInventoryItem>();
        var stashIndex = -1;
        if (ingameState.IngameUi.StashElement.IsVisible && ingameState.IngameUi.StashElement.VisibleStash != null)
        {
            stashIndex = ingameState.IngameUi.StashElement.IndexVisibleStash;
            var items = ingameState.IngameUi.StashElement.VisibleStash.VisibleInventoryItems;
            if (items != null)
                result.AddRange(items.Where(item => item?.Item != null && item.Item.HasComponent<MapKey>()));
        }

        return (stashIndex, result);
    }

    private List<NormalInventoryItem> GetFaustusItems()
    {
        var result = new List<NormalInventoryItem>();
        try
        {
            var purchaseWindow = ingameState.IngameUi.PurchaseWindow;
            if (purchaseWindow?.IsVisible == true)
                result.AddRange(FindMapItems(purchaseWindow));
        }
        catch { }
        return result;
    }

    public override bool Initialise()
    {
        base.Initialise();
        Name = "Map Mod Notifications";
        windowArea = GameController.Window.GetWindowRectangle();
        gameController = GameController;
        ingameState = gameController.IngameState;
        _inventoryItems = new TimeCache<List<NormalInventoryItem>>(GetInventoryItems, Settings.InventoryCacheInterval);
        _stashItems = new TimeCache<(int stashIndex, List<NormalInventoryItem>)>(GetStashItems, Settings.StashCacheInterval);
        _faustusItems = new TimeCache<List<NormalInventoryItem>>(GetFaustusItems, 500);
        // Init profiles (added)
        LiveSettings = Settings;
        _modEntries = ModDataLoader.Load(DirectoryFullName);
        _profileManager = new ProfileManager(ConfigDirectory);
        var activeProf = Settings.ActiveProfile.Value;
        if (!_profileManager.Profiles.ContainsKey(activeProf)) activeProf = "Default";
        _profileManager.LoadProfile(activeProf, Settings);
        Settings.ActiveProfile.Value = activeProf;

        return true;
    }

    public void RenderItem(NormalInventoryItem Item, Entity Entity)
    {
        var pushedColors = 0;
        var entity = Entity;
        var item = Item;
        if (entity.Address != 0 && entity.IsValid)
        {
            var baseType = gameController.Files.BaseItemTypes.Translate(entity.Path);
            var classID = baseType.ClassName ?? string.Empty;

            if (!entity.HasComponent<MapKey>()) return;

            // Only show tooltip for Magic, Rare and Unique maps
            var rarity = entity.GetComponent<Mods>()?.ItemRarity;
            if (rarity == null || rarity == ItemRarity.Normal || rarity == ItemRarity.Unique) return;

            var ItemDetails = Entity.GetHudComponent<ItemDetails>();
            if (ItemDetails == null)
            {
                ItemDetails = new ItemDetails(Item, Entity);
                Entity.SetHudComponent(ItemDetails);
            }
            else
            {
                // Re-evaluate every frame so toggle changes take effect immediately
                ItemDetails.ActiveWarnings.Clear();
                ItemDetails.Update();
            }
            if (Settings.AlwaysShowTooltip || ItemDetails.ActiveWarnings.Count > 0)
            {
                var boxOrigin = new nuVector2(MouseLite.GetCursorPositionVector().X + 50, MouseLite.GetCursorPositionVector().Y + 80);

                var _opened = true;
                pushedColors += 1;
                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFF3F3F3F);
                if (ImGui.Begin($"{entity.Address}", ref _opened,
                        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize |
                        ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavInputs))
                {
                    ImGui.BeginGroup();
                    ImGui.TextColored(ItemDetails.ItemColor, $"{ItemDetails.MapName}");

                    // Quantity and Packsize
                    {
                        var qCol = new nuVector4(1f, 1f, 1f, 1f);
                        if (Settings.ColorQuantityPercent)
                            if (ItemDetails.Quantity < Settings.ColorQuantity) qCol = new nuVector4(1f, 0.4f, 0.4f, 1f);
                            else
                                qCol = new nuVector4(0.4f, 1f, 0.4f, 1f);
                        if (Settings.ShowQuantityPercent && ItemDetails.Quantity != 0 && Settings.ShowPackSizePercent && ItemDetails.PackSize != 0)
                        {
                            ImGui.TextColored(qCol, $"{ItemDetails.Quantity}%% Quant");
                            ImGui.SameLine();
                            ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{ItemDetails.PackSize}%% Pack Size");
                        }
                        else if (Settings.ShowQuantityPercent && ItemDetails.Quantity != 0)
                            ImGui.TextColored(qCol, $"{ItemDetails.Quantity}%% Quantity");
                        else if (Settings.ShowPackSizePercent && ItemDetails.PackSize != 0)
                            ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{ItemDetails.PackSize}%% Pack Size");

                        if (Settings.HorizontalLines && ItemDetails.ActiveWarnings.Count > 0 && (Settings.ShowModCount || Settings.ShowModWarnings))
                        {
                            ImGui.Separator();
                        }
                    }

                    // Count Mods
                    if (Settings.ShowModCount && ItemDetails.ModCount != 0)
                        ImGui.TextColored(new nuVector4(1f, 1f, 1f, 1f), $"{ItemDetails.ModCount} Mods");

                    // Mod warnings
                    if (Settings.ShowModWarnings)
                        foreach (var StyledText in ItemDetails.ActiveWarnings.OrderBy(x => x.Color.ToString()).ToList())
                            ImGui.TextColored(SharpToNu(StyledText.Color), StyledText.Text);


                    ImGui.EndGroup();

                    // Detect and adjust for edges
                    var size = ImGui.GetWindowSize();
                    if (boxOrigin.X + size.X > windowArea.Width)
                        ImGui.SetWindowPos(new nuVector2(boxOrigin.X - (boxOrigin.X + size.X - windowArea.Width) - 4, boxOrigin.Y + 24), ImGuiCond.Always);
                    else
                        ImGui.SetWindowPos(boxOrigin, ImGuiCond.Always);
                }
                ImGui.End();
                ImGui.PopStyleColor(pushedColors);
            }
        }
    }

    // Cached flat lists for border checking — avoids LINQ per-frame per-item
    private List<string> _cachedEnabledTypes  = new();
    private List<string> _cachedBrickedTypes  = new();
    private int          _lastEnabledCount    = -1;
    private int          _lastBrickedCount    = -1;

    private void RefreshBorderCache()
    {
        int ec = LiveSettings?.EnabledMods?.Count ?? 0;
        int bc = LiveSettings?.BrickedMods?.Count  ?? 0;
        if (ec == _lastEnabledCount && bc == _lastBrickedCount) return;
        _cachedEnabledTypes = LiveSettings?.EnabledMods?
            .Where(kv => kv.Value).Select(kv => kv.Key).ToList() ?? new();
        _cachedBrickedTypes = LiveSettings?.BrickedMods?
            .Where(kv => kv.Value).Select(kv => kv.Key).ToList() ?? new();
        _lastEnabledCount = ec;
        _lastBrickedCount = bc;
    }

    private void DrawMapBorders(NormalInventoryItem item)
    {
        if (!item.Item.HasComponent<MapKey>())
            return;
        RefreshBorderCache();

        var rect = item.GetClientRectCache;
        double deflatePercent = Settings.BorderDeflation;
        var deflateWidth = (int)(rect.Width * (deflatePercent / 100.0));
        var deflateHeight = (int)(rect.Height * (deflatePercent / 100.0));
        rect.Inflate(-deflateWidth, -deflateHeight);

        var itemDetails = item.Item.GetHudComponent<ItemDetails>() ?? new ItemDetails(item, item.Item);
        item.Item.SetHudComponent(itemDetails);

        var is8Mod    = itemDetails.IsEightMod;
        var thickness = Settings.BorderThicknessMap;

        // Determine border color from the new system
        SharpDX.Color? modColor = null;

        if (LiveSettings?.EnabledMods != null)
        {
            var mods = item.Item.GetComponent<Mods>()?.ItemMods
                .Where(x => !x.Group.Contains("MapAtlasInfluence"))
                .ToList();

            if (mods != null)
            {
                // Check bricked first — highest priority
                if (Settings.BoxForMapBadWarnings)
                {
                    bool hasBricked = mods.Any(mod =>
                        _cachedBrickedTypes.Any(t =>
                            mod.RawName.Contains(t, StringComparison.OrdinalIgnoreCase)));
                    if (hasBricked)
                        modColor = Settings.Bricked.ToSharpColor();
                }

                // Then check warnings
                if (modColor == null && Settings.BoxForMapWarnings)
                {
                    bool hasWarning = mods.Any(mod =>
                        _cachedEnabledTypes.Any(t =>
                            mod.RawName.Contains(t, StringComparison.OrdinalIgnoreCase)));
                    if (hasWarning)
                        modColor = Settings.MapBorderWarnings.ToSharpColor();
                }
            }
        }


        // Draw border — 8-mod splits with warning color if both present
        if (is8Mod && modColor.HasValue)
        {
            var eightModColor = Settings.EightModBorder.ToSharpColor();
            var midY = rect.Top + rect.Height / 2;
            Graphics.DrawLine(new nuVector2(rect.Left,  rect.Top),    new nuVector2(rect.Right, rect.Top),    thickness, eightModColor);
            Graphics.DrawLine(new nuVector2(rect.Left,  rect.Top),    new nuVector2(rect.Left,  midY),        thickness, eightModColor);
            Graphics.DrawLine(new nuVector2(rect.Right, rect.Top),    new nuVector2(rect.Right, midY),        thickness, eightModColor);
            Graphics.DrawLine(new nuVector2(rect.Left,  midY),        new nuVector2(rect.Left,  rect.Bottom), thickness, modColor.Value);
            Graphics.DrawLine(new nuVector2(rect.Right, midY),        new nuVector2(rect.Right, rect.Bottom), thickness, modColor.Value);
            Graphics.DrawLine(new nuVector2(rect.Left,  rect.Bottom), new nuVector2(rect.Right, rect.Bottom), thickness, modColor.Value);
        }
        else if (is8Mod)
        {
            if (Settings.MapBorderStyle) Graphics.DrawBox(rect,   Settings.EightModBorder.ToSharpColor(), thickness);
            else                         Graphics.DrawFrame(rect, Settings.EightModBorder.ToSharpColor(), thickness);
        }
        else if (modColor.HasValue)
        {
            if (Settings.MapBorderStyle) Graphics.DrawBox(rect,   modColor.Value, thickness);
            else                         Graphics.DrawFrame(rect, modColor.Value, thickness);
        }
    }

    private static IEnumerable<NormalInventoryItem> FindMapItems(ExileCore.PoEMemory.Element element, int depth = 0)
    {
        if (element == null || depth > 6) yield break;
        var invItem = element.AsObject<NormalInventoryItem>();
        if (invItem?.Item != null && invItem.Item.HasComponent<MapKey>())
        {
            yield return invItem;
            yield break;
        }
        foreach (var child in element.Children)
        {
            foreach (var found in FindMapItems(child, depth + 1))
                yield return found;
        }
    }

    public override void Render()
    {
        var uiHover = ingameState.UIHover;
        if (ingameState.UIHover?.IsVisible ?? false)
        {
            var hoverItem = uiHover.AsObject<NormalInventoryItem>();
            if (hoverItem?.Item?.Path != null)
                RenderItem(hoverItem, hoverItem.Item);
        }

        if (ingameState.IngameUi.InventoryPanel.IsVisible)
        {
            foreach (var item in _inventoryItems.Value)
                DrawMapBorders(item);
        }

        // Regular stash
        if (ingameState.IngameUi.StashElement.IsVisible
            && ingameState.IngameUi.StashElement.VisibleStash != null
            && ingameState.IngameUi.StashElement.IndexVisibleStash == _stashItems.Value.stashIndex)
        {
            foreach (var item in _stashItems.Value.Item2)
                DrawMapBorders(item);
        }

        // Faustus market
        if (Settings.ShowBorderInFaustus)
        try
        {
            var purchaseWindow = ingameState.IngameUi.PurchaseWindow;
            if (purchaseWindow?.IsVisible == true)
            {
                foreach (var item in FindMapItems(purchaseWindow))
                    DrawMapBorders(item);
            }
        }
        catch { }
    }
}
