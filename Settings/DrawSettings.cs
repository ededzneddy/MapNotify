using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.Shared.Nodes;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using nuVector2 = System.Numerics.Vector2;
using nuVector4 = System.Numerics.Vector4;

namespace MapNotify
{
    partial class MapNotify : BaseSettingsPlugin<MapNotifySettings>
    {
        // ── State ─────────────────────────────────────────────────────────────
        private string _modSearch     = "";
        private bool   _confirmDelete = false;
        private string _deleteTarget  = "";
        private string _newProfName   = "";
        private readonly Dictionary<string, bool> _groupOpen = new();

        // ── Helpers ───────────────────────────────────────────────────────────
        public static List<string> hoverMods = new();

        private static void HelpMarker(string desc)
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(?)");
            if (!ImGui.IsItemHovered()) return;
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35f);
            ImGui.TextUnformatted(desc);
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        private static bool Toggle(string label, ToggleNode node)
        {
            var v = node.Value;
            ImGui.Checkbox(label, ref v);
            node.Value = v;
            return v;
        }

        private static int IntSlider(string label, RangeNode<int> node)
        {
            var v = node.Value;
            ImGui.SliderInt(label, ref v, node.Min, node.Max);
            return v;
        }

        private static void DebugHover()
        {
            var uiHover = ingameState.UIHover;
            if (uiHover == null || !uiHover.IsVisible) return;
            var icon   = uiHover.AsObject<NormalInventoryItem>();
            if (icon == null) return;
            var entity = icon.Item;
            if (entity == null || entity.Address == 0 || !entity.IsValid) return;
            var mods = entity.GetComponent<Mods>();
            if (mods == null) { hoverMods.Clear(); return; }
            if (mods.ItemMods.Count == 0) { hoverMods.Clear(); return; }
            hoverMods.Clear();
            foreach (var mod in mods.ItemMods)
                hoverMods.Add($"{mod.RawName} : {mod.Value1}, {mod.Value2}, {mod.Value3}, {mod.Value4}");
        }

        // ── Brick pill (custom — kept minimal) ───────────────────────────────
        private static bool BrickPill(string id, bool bricked)
        {
            // Simple colored button — on = red "BRICK", off = grey "brick"
            if (bricked)
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        new nuVector4(0.55f, 0.08f, 0.08f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new nuVector4(0.70f, 0.12f, 0.12f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new nuVector4(0.40f, 0.06f, 0.06f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Text,          new nuVector4(1.00f, 0.60f, 0.60f, 1f));
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Button,        new nuVector4(0.14f, 0.14f, 0.18f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new nuVector4(0.22f, 0.22f, 0.28f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new nuVector4(0.10f, 0.10f, 0.14f, 1f));
                ImGui.PushStyleColor(ImGuiCol.Text,          new nuVector4(0.35f, 0.35f, 0.40f, 1f));
            }
            bool clicked = ImGui.SmallButton(bricked ? "BRICK##" + id : "brick##" + id);
            ImGui.PopStyleColor(4);
            if (clicked) return !bricked;
            return bricked;
        }

        // ── DrawSettings entry point ──────────────────────────────────────────
        public override void DrawSettings()
        {
            // Status line
            int warnCount  = Settings.EnabledMods.Count(kv => kv.Value);
            int brickCount = Settings.BrickedMods.Count(kv => kv.Value);
            ImGui.TextDisabled($"{warnCount} warning{(warnCount != 1 ? "s" : "")}   {brickCount} bricked   |   {Settings.ActiveProfile.Value}");
            ImGui.Separator();

            if (!ImGui.BeginTabBar("##mapmods_tabs")) return;

            if (ImGui.BeginTabItem("Mods"))    { TabMods();     ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Profiles")){ TabProfiles(); ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Border"))  { TabBorder();   ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Display")) { TabDisplay();  ImGui.EndTabItem(); }
            if (ImGui.BeginTabItem("Debug"))   { TabDebug();    ImGui.EndTabItem(); }

            ImGui.EndTabBar();
        }

        // ── Tab: Mods ─────────────────────────────────────────────────────────
        private void TabMods()
        {
            if (_modEntries == null || _modEntries.Count == 0)
            {
                ImGui.TextColored(new nuVector4(1f, 0.4f, 0.1f, 1f), "map_mods_data.json not loaded.");
                return;
            }

            // Search
            ImGui.Spacing();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 60f);
            ImGui.InputTextWithHint("##ms", "Search mods...", ref _modSearch, 128);
            ImGui.SameLine();
            if (ImGui.Button("Clear")) _modSearch = "";
            ImGui.Spacing();

            var search = _modSearch.Trim().ToLowerInvariant();
            var grouped = _modEntries
                .Where(e => string.IsNullOrEmpty(search)
                    || e.Name.ToLowerInvariant().Contains(search)
                    || e.Effect.ToLowerInvariant().Contains(search)
                    || e.ModType.ToLowerInvariant().Contains(search))
                .GroupBy(e => ModDataLoader.GetGroup(e.ModType))
                .OrderBy(g => g.Key == "Other" ? "zzz" : g.Key);

            foreach (var group in grouped)
            {
                int activeInGroup = group.Count(e =>
                    Settings.EnabledMods.TryGetValue(e.ModType, out var v) && v);

                // TreeNodeEx with OpenOnArrow so clicking a row inside doesn't collapse the group
                string headerLabel = activeInGroup > 0
                    ? $"{group.Key}  ({activeInGroup} active)##grp_{group.Key}"
                    : $"{group.Key}##grp_{group.Key}";

                if (!_groupOpen.ContainsKey(group.Key))
                    _groupOpen[group.Key] = false;
                bool open = _groupOpen[group.Key];

                // Draw header as a button so clicks only toggle, never conflict with children
                ImGui.PushStyleColor(ImGuiCol.Button,        new nuVector4(0.15f, 0.15f, 0.18f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new nuVector4(0.20f, 0.20f, 0.25f, 1f));
                ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new nuVector4(0.12f, 0.12f, 0.15f, 1f));
                ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new nuVector2(0f, 0.5f));
                string arrow = open ? "v " : "> ";
                if (ImGui.Button(arrow + headerLabel, new nuVector2(ImGui.GetContentRegionAvail().X, 0)))
                    _groupOpen[group.Key] = !open;
                ImGui.PopStyleVar();
                ImGui.PopStyleColor(3);
                if (!open) continue;

                foreach (var entry in group)
                {
                    bool isEnabled = Settings.EnabledMods.TryGetValue(entry.ModType, out var en) && en;
                    bool isBricked = isEnabled && Settings.BrickedMods.TryGetValue(entry.ModType, out var br) && br;

                    // Highlight row if active
                    if (isBricked)
                        ImGui.PushStyleColor(ImGuiCol.Header, new nuVector4(0.45f, 0.06f, 0.06f, 0.5f));
                    else if (isEnabled)
                        ImGui.PushStyleColor(ImGuiCol.Header, new nuVector4(0.45f, 0.18f, 0.06f, 0.35f));

                    // Fixed-height selectable — 34px fits two lines cleanly
                    float lineH = ImGui.GetTextLineHeight();
                    float rowH  = lineH * 2f + 8f;
                    float availW = ImGui.GetContentRegionAvail().X;
                    float selectW = isEnabled ? availW - 60f : availW;

                    ImGui.SetNextItemAllowOverlap();
                    bool rowClicked = ImGui.Selectable(
                        $"##sel_{entry.ModType}",
                        isEnabled || isBricked,
                        ImGuiSelectableFlags.DontClosePopups,
                        new nuVector2(selectW, rowH));

                    if (isBricked || isEnabled) ImGui.PopStyleColor();

                    if (rowClicked)
                    {
                        if (isEnabled)
                        {
                            Settings.EnabledMods[entry.ModType] = false;
                            Settings.BrickedMods.Remove(entry.ModType);
                        }
                        else
                            Settings.EnabledMods[entry.ModType] = true;
                    }

                    // Overlay text — positioned within the selectable bounds
                    var selMin = ImGui.GetItemRectMin();
                    var dl = ImGui.GetWindowDrawList();
                    var nameCol = isBricked
                        ? new nuVector4(1f, 0.50f, 0.50f, 1f)
                        : isEnabled
                            ? new nuVector4(0.90f, 0.90f, 0.90f, 1f)
                            : new nuVector4(0.65f, 0.65f, 0.68f, 1f);
                    dl.AddText(selMin + new nuVector2(4f, 3f),
                        ImGui.GetColorU32(nameCol), entry.Name);
                    dl.AddText(selMin + new nuVector2(4f, 3f + lineH),
                        ImGui.GetColorU32(new nuVector4(0.32f, 0.32f, 0.36f, 1f)),
                        entry.Effect.Length > 100 ? entry.Effect[..100] + "..." : entry.Effect);

                    // Brick button — same line after selectable
                    if (isEnabled)
                    {
                        ImGui.SameLine();
                        bool newBrick = BrickPill(entry.ModType, isBricked);
                        if (newBrick != isBricked)
                            Settings.BrickedMods[entry.ModType] = newBrick;
                    }
                }
                ImGui.Spacing();
            }
        }

        // ── Tab: Profiles ─────────────────────────────────────────────────────
        private void TabProfiles()
        {
            var pm = _profileManager;
            if (pm == null) { ImGui.TextDisabled("Profile manager not initialised."); return; }

            ImGui.Spacing();
            ImGui.TextDisabled("Active: " + Settings.ActiveProfile.Value);
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var name in pm.ProfileNames)
            {
                bool isActive = name == Settings.ActiveProfile.Value;

                if (isActive)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(1f, 0.65f, 0.2f, 1f));
                    ImGui.TextUnformatted(">> " + name);
                    ImGui.PopStyleColor();
                }
                else
                {
                    ImGui.TextUnformatted("   " + name);
                }

                ImGui.SameLine(200f);

                // Load button (acts as activate)
                if (!isActive)
                {
                    if (ImGui.SmallButton($"Load##{name}"))
                    {
                        pm.LoadProfile(name, Settings);
                        Settings.ActiveProfile.Value = name;
                    }
                    ImGui.SameLine();
                }

                if (ImGui.SmallButton($"Save##{name}"))
                    pm.SaveProfile(name, Settings);
                HelpMarker("Overwrites this profile with current mod selections");

                if (name != "Default")
                {
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(0.75f, 0.2f, 0.2f, 1f));
                    if (ImGui.SmallButton($"Del##{name}"))
                    { _confirmDelete = true; _deleteTarget = name; }
                    ImGui.PopStyleColor();
                }

                ImGui.Separator();
            }

            if (_confirmDelete)
            {
                ImGui.Spacing();
                ImGui.TextColored(new nuVector4(1f, 0.3f, 0.3f, 1f), $"Delete \"{_deleteTarget}\"?");
                ImGui.SameLine();
                if (ImGui.SmallButton("Yes"))
                {
                    if (Settings.ActiveProfile.Value == _deleteTarget)
                    { pm.LoadProfile("Default", Settings); Settings.ActiveProfile.Value = "Default"; }
                    pm.DeleteProfile(_deleteTarget);
                    _confirmDelete = false; _deleteTarget = "";
                }
                ImGui.SameLine();
                if (ImGui.SmallButton("Cancel##cxd")) { _confirmDelete = false; _deleteTarget = ""; }
                ImGui.Spacing();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextDisabled("New profile");
            ImGui.SetNextItemWidth(180f);
            ImGui.InputTextWithHint("##np", "Profile name...", ref _newProfName, 64);
            ImGui.SameLine();
            bool canCreate = !string.IsNullOrWhiteSpace(_newProfName)
                             && !pm.Profiles.ContainsKey(_newProfName);
            if (!canCreate) ImGui.BeginDisabled();
            if (ImGui.SmallButton("Create"))
            {
                pm.SaveProfile(_newProfName, Settings);
                Settings.ActiveProfile.Value = _newProfName;
                _newProfName = "";
            }
            if (!canCreate) ImGui.EndDisabled();
        }

        // ── Tab: Border ───────────────────────────────────────────────────────
        private void TabBorder()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Enable Borders");
            ImGui.Separator();
            Toggle("Warning mods border", Settings.BoxForMapWarnings);
            Toggle("Bricked mods border", Settings.BoxForMapBadWarnings);
            Toggle("Use Box style (off = Frame)", Settings.MapBorderStyle);

            ImGui.Spacing();
            ImGui.TextDisabled("Colors");
            ImGui.Separator();
            Settings.MapBorderWarnings = ColorEdit("Warning color##wc", Settings.MapBorderWarnings);
            Settings.Bricked           = ColorEdit("Bricked color##bc", Settings.Bricked);
            Settings.EightModBorder    = ColorEdit("8-Mod color##ec",   Settings.EightModBorder);

            ImGui.Spacing();
            ImGui.TextDisabled("Size");
            ImGui.Separator();
            Settings.BorderDeflation.Value    = IntSlider("Deflation##bd",  Settings.BorderDeflation);
            Settings.BorderThicknessMap.Value = IntSlider("Thickness##bt",  Settings.BorderThicknessMap);

            ImGui.Spacing();
            ImGui.TextDisabled("Quantity / Pack Size Thresholds");
            ImGui.Separator();
            ImGui.TextDisabled("Text turns red below these values.");
            Settings.MapQuantSetting.Value = IntSlider("Quantity##qt",  Settings.MapQuantSetting);
            Settings.MapPackSetting.Value  = IntSlider("Pack size##ps", Settings.MapPackSetting);
        }

        private static nuVector4 ColorEdit(string label, nuVector4 value)
        {
            ImGui.ColorEdit4(label, ref value,
                ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.AlphaBar);
            return value;
        }

        // ── Tab: Display ──────────────────────────────────────────────────────
        private void TabDisplay()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Tooltip");
            ImGui.Separator();
            Toggle("Always show tooltip",        Settings.AlwaysShowTooltip);
            HelpMarker("Shows tooltip even when there are no warnings");
            Toggle("Show mod warnings",          Settings.ShowModWarnings);
            Toggle("Show mod count",             Settings.ShowModCount);
            Toggle("Show quantity %",            Settings.ShowQuantityPercent);
            Toggle("Show pack size %",           Settings.ShowPackSizePercent);
            Toggle("Horizontal separator lines", Settings.HorizontalLines);

            ImGui.Spacing();
            ImGui.TextDisabled("Windows");
            ImGui.Separator();
            Toggle("Show border in Faustus market", Settings.ShowBorderInFaustus);

            ImGui.Spacing();
            ImGui.TextDisabled("Quantity Color");
            ImGui.Separator();
            ImGui.TextDisabled("Turns red below threshold, green above.");
            Toggle("Enable quantity color", Settings.ColorQuantityPercent);
            Settings.ColorQuantity.Value = IntSlider("Threshold##cq", Settings.ColorQuantity);

            ImGui.Spacing();
            ImGui.TextDisabled("Cache Intervals");
            ImGui.Separator();
            ImGui.TextDisabled("Reload plugin after changing.");
            Settings.InventoryCacheInterval.Value = IntSlider("Inventory (ms)##ic", Settings.InventoryCacheInterval);
            Settings.StashCacheInterval.Value     = IntSlider("Stash (ms)##sc",     Settings.StashCacheInterval);

            ImGui.Spacing();
            ImGui.TextDisabled("Tooltip Position");
            ImGui.Separator();
            Settings.TooltipOffsetX.Value = IntSlider("Horizontal offset##tx", Settings.TooltipOffsetX);
            Settings.TooltipOffsetY.Value = IntSlider("Vertical offset##ty",   Settings.TooltipOffsetY);
        }

        // ── Tab: Debug ────────────────────────────────────────────────────────
        private void TabDebug()
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Hover a map in inventory to see its raw mod names.");
            ImGui.TextDisabled("Use these to add entries to map_mods_data.json.");
            ImGui.Separator();
            ImGui.Spacing();
            DebugHover();
            if (hoverMods.Count > 0)
            {
                if (ImGui.Button("Copy All"))
                    ImGui.SetClipboardText(string.Join(Environment.NewLine, hoverMods));
                ImGui.Spacing();
                foreach (var mod in hoverMods)
                {
                    ImGui.TextColored(new nuVector4(1f, 0.45f, 0.1f, 1f), mod);
                    ImGui.SameLine();
                    ImGui.PushStyleColor(ImGuiCol.Text, new nuVector4(0.4f, 0.4f, 0.45f, 1f));
                    if (ImGui.SmallButton($"Copy##{mod}"))
                        ImGui.SetClipboardText(mod.Split(' ')[0]); // copies just the RawName
                    ImGui.PopStyleColor();
                }
            }
            else
                ImGui.TextDisabled("(no map item hovered)");
        }
    }
}
