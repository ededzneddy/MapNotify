using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System.Collections.Generic;
using Vector4 = System.Numerics.Vector4;

namespace MapNotify
{
    public class MapNotifySettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new(true);

        // Cache intervals
        public RangeNode<int> InventoryCacheInterval { get; set; } = new(50, 1, 2000);
        public RangeNode<int> StashCacheInterval { get; set; } = new(500, 1, 2000);

        // Display
        public ToggleNode ShowModCount          { get; set; } = new(true);
        public ToggleNode ShowQuantityPercent   { get; set; } = new(true);
        public ToggleNode ShowPackSizePercent   { get; set; } = new(true);
        public ToggleNode ColorQuantityPercent  { get; set; } = new(true);
        public RangeNode<int> ColorQuantity     { get; set; } = new(100, 0, 220);
        public ToggleNode AlwaysShowTooltip     { get; set; } = new(true);
        public ToggleNode ShowModWarnings       { get; set; } = new(true);
        public ToggleNode HorizontalLines       { get; set; } = new(true);

        // Border
        public ToggleNode MapBorderStyle        { get; set; } = new(false);
        public RangeNode<int> BorderDeflation   { get; set; } = new(4, 0, 50);
        public RangeNode<int> MapQuantSetting   { get; set; } = new(100, 0, 220);
        public RangeNode<int> MapPackSetting    { get; set; } = new(100, 0, 220);
        public ToggleNode BoxForMapWarnings     { get; set; } = new(true);
        public ToggleNode BoxForMapBadWarnings  { get; set; } = new(true);
        public Vector4 Bricked                  { get; set; } = new(1f, 0f, 0f, 1f);
        public Vector4 MapBorderWarnings        { get; set; } = new(0f, 1f, 0f, 1f);
        public Vector4 EightModBorder           { get; set; } = new(1f, 0.65f, 0f, 1f);
        public RangeNode<int> BorderThicknessMap{ get; set; } = new(2, 1, 6);

        // Bad mod file selector (legacy)


        // Profile system
        public TextNode ActiveProfile           { get; set; } = new("Default");

        public ToggleNode ShowBorderInFaustus { get; set; } = new(false);

        // Mod selection — Dictionary<modType, enabled>
        // Persisted as a flat dictionary so any mod from the JSON can be toggled
        public Dictionary<string, bool> EnabledMods { get; set; } = new();

        // Which enabled mods are considered bricked (overrides warning color with bricked color)
        public Dictionary<string, bool> BrickedMods { get; set; } = new();

        // User-defined display names for mods (overrides default Name from JSON)
        public Dictionary<string, string> CustomModNames { get; set; } = new();
    }
}
