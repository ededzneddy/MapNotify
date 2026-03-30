# CLAUDE.md — MapMods

## How This File Works

This file is a living reference document. Every time we work on this plugin — whether we fix a bug, add a feature, learn something new about ExileApi, or make a design decision — the relevant information gets recorded here. That way, future sessions always have full context without needing to re-explain anything.

**Rules:**
- Anything we discover, fix, or decide about **MapNotify/MapMods** goes in this file.
- If we start work on a **different plugin**, that plugin gets its **own CLAUDE.md** in its own folder — same format, same idea.
- Each plugin's CLAUDE.md is self-contained — it only covers that plugin.

---


## Plugin Context

MapMods is an ExileCore plugin for Path of Exile that shows map mod warnings, colour-coded borders, and a hover tooltip when the player mouses over maps in their inventory, stash, guild stash, and Faustus market window. It replaces the original MapNotify txt-file system with a JSON-driven mod list, a profile system, and an auto-discovery "seen mods" system.

**Main class**: `MapNotify` inherits `BaseSettingsPlugin<MapNotifySettings>` — split across `MapNotify.cs` and `EvaluateItem.cs` via `partial class`
**Settings class**: `MapNotifySettings` implements `ISettings`
**Key classes**: `ItemDetails`, `StyledText`, `MapModEntry`, `SeenModsManager`, `ProfileManager`, `ModDataLoader`

---

## Architecture

### Lifecycle Methods Used

| Method | Purpose |
|---|---|
| `Initialise()` | Wires `TimeCache` for inventory/stash/faustus items, loads `SeenModsManager`, loads `ModDataLoader` seed, loads profiles |
| `Render()` | Hover tooltip via `RenderItem()`, border drawing via `DrawMapBorders()` for inventory/stash/guild stash/faustus, flushes `SeenModsManager` dirty writes |
| `DrawSettings()` | Custom tabbed ImGui UI via `DrawSettings.cs` partial |

### SeenModsManager — Removed (deferred)

`SeenModsManager.cs` and `SeenModEntry` have been removed. The feature auto-discovered new mods on hover and wrote them to `seen_mods.json`, merging with the seed `map_mods_data.json`. It was removed because the JSON-only approach is simpler and sufficient for now.

**What replaced it:** `ModDataLoader.Load()` now populates `entry.Group` via `GetGroup()` before returning, so the returned list is fully ready to use. `Initialise()` applies `CustomModNames` overrides inline, then assigns directly to `_modEntries`. The hover recording (`RecordMod`) and `FlushIfDirty()` calls were also removed.

**If re-adding later:** Restore `SeenModsManager.cs`, add `_seenMods` field back to `MapNotify.cs`, wire `Load()` + `ToModEntries()` in `Initialise()`, call `RecordMod()` in `EvaluateItem.Update()`, and flush in `Render()`. Note: if a `ModState` enum refactor happens, `profiles.json` will need to be deleted.

---

### No Tick() — Known Limitation

Border drawing runs in `Render()` via `TimeCache`. The correct pattern (studied from `WheresMyShitMapsAt`) is to move scanning to `Tick()` with a double-buffer `HighlightCache` keyed by `item.Item.Address`. This is a planned refactor — see Performance section.

---

## Data Flow

### Mod Entry Pipeline

```
map_mods_data.json (seed)
        ↓ ModDataLoader.Load()
        ↓ SeenModsManager.Load(seedEntries)  — merges seed into seen_mods.json
        ↓ SeenModsManager.ToModEntries()
_modEntries : List<MapModEntry>              — used by EvaluateItem for warning matching
```

On hover → `EvaluateItem.Update()` → `SeenModsManager.RecordMod(mod.RawName)` → if new: adds to seen_mods.json, rebuilds `_modEntries`

### Warning/Border State

Four separate dictionaries in `MapNotifySettings`:
- `EnabledMods : Dictionary<string, bool>` — is this mod type selected as a warning
- `BrickedMods : Dictionary<string, bool>` — is this mod type marked bricked (red border, highest priority)
- `GoodMods : Dictionary<string, bool>` — is this mod type marked good (blue border, middle priority)
- `CustomModNames : Dictionary<string, string>` — user rename overrides

**Border priority:** Bricked > Good > Warning. Bricked and Good are mutually exclusive — toggling one clears the other.
All three dicts are saved/loaded per profile via `ProfileManager`.

**Default colors:** Warning = blue/cyan `(0, 0.6, 1, 1)`, Good = green `(0, 1, 0, 1)`, Bricked = red `(1, 0, 0, 1)`.

**Tooltip text color:** `EvaluateItem.cs` reads colors directly from `LiveSettings` (Bricked/GoodModBorder/MapBorderWarnings) so tooltip text always matches border colors. Hardcoded colors removed.

### Border Priority

```
Bricked > Warning > 8-Mod (standalone)
```

Split border when 8-Mod + Warning/Bricked both present: top half = 8-mod colour, bottom half = warning/bricked colour.

---

## Key Files

| File | Role |
|---|---|
| `Main/MapNotify.cs` | Lifecycle, RenderItem tooltip, DrawMapBorders, FindMapItems, Render loop |
| `Main/EvaluateItem.cs` | `ItemDetails` class, `Update()` — reads mod stats, fires warnings, records seen mods |
| `Settings/MapNotifySettings.cs` | All settings nodes |
| `Settings/DrawSettings.cs` | Full ImGui tab UI — Mods/Profiles/Border/Display/Debug |
| `Settings/ModDataLoader.cs` | Loads `map_mods_data.json`, `GetGroup()` prefix matching, group definitions |
| `Settings/SeenModsManager.cs` | Live seen-mods list, load/merge/save to `seen_mods.json`, `RecordMod()` |
| `Settings/ProfileManager.cs` | Save/load/delete named profiles to `config/MapNotify/profiles.json` |
| `Settings/ConfigFiles.cs` | Stub — old txt file system removed, kept for compile compat |
| `data/map_mods_data.json` | Seed mod list with hand-edited display names |

---

## ExileApi Specifics Learned

### Component Names (PoE2)

| Old (PoE1) | Current (PoE2) |
|---|---|
| `Map` | `MapKey` |
| `HeistBlueprint`, `HeistContract` | Removed entirely |

Always use `entity.HasComponent<MapKey>()` to identify maps. `MapKey` also exposes `.Tier`.

### Mod Access Patterns

```csharp
// Raw internal mod type string — use for matching in our system
mod.RawName   // e.g. "MapPlayerCurseVulnerability2"

// Human-readable display name — used by WheresMyShitMapsAt Contains() matching
mod.Name      // e.g. "Players are Cursed with Vulnerability"

// Stat values — for quantity/packsize/rarity/currency/scarabs
mod.ModRecord.StatNames  // IEnumerable of KeyValuePair<string, ...>
mod.Values[index]        // int value at stat index
```

**Stat keys confirmed via DebugWindow.LogMsg:**
- `map_pack_size_+%` — pack size
- `map_item_drop_quantity_+%` — item quantity
- `map_item_drop_rarity_+%` — item rarity
- `map_currency_drop_chance_+%_final_from_uber_mod` — currency (memory/uber mods only)
- `map_scarab_drop_chance_+%_final_from_uber_mod` — scarabs (memory/uber mods only)

**Pattern to read a stat:**
```csharp
void UpdateValueIfStatExists(string key, Action<int> updateAction)
{
    var index = mod.ModRecord.StatNames
        .Select((value, index) => new { value, index })
        .FirstOrDefault(pair => pair.value.Key == key)?.index ?? -1;
    if (index != -1)
        updateAction(mod.Values[index]);
}
```

### Rarity Filtering

```csharp
var rarity = entity.GetComponent<Mods>()?.ItemRarity;
if (rarity == null || rarity == ItemRarity.Normal || rarity == ItemRarity.Unique) return;
```

Normal maps have no mods worth showing. Unique maps are excluded by choice.

### InventoryItems Null Safety

`VisibleInventoryItems` can return null during stash tab switches. Always null-check:
```csharp
var items = stash.VisibleInventoryItems;
if (items != null)
    result.AddRange(items.Where(item => item?.Item != null && item.Item.HasComponent<MapKey>()));
```

### Window Access

| Window | ExileApi Property |
|---|---|
| Player inventory | `IngameUi.InventoryPanel[InventoryIndex.PlayerInventory]` |
| Regular stash | `IngameUi.StashElement.VisibleStash.VisibleInventoryItems` |
| Guild stash | `IngameUi.GuildStashElement.VisibleStash.VisibleInventoryItems` |
| Faustus (town/map) | `IngameUi.PurchaseWindow.TabContainer.VisibleStash.VisibleInventoryItems` |
| Faustus (hideout) | `IngameUi.PurchaseWindowHideout.TabContainer.VisibleStash.VisibleInventoryItems` |
| Atlas panel | `IngameUi.AtlasPanel` — child path varies by patch, not reliable |

**Learned from Ninja Price (Get-Chaos-Value):** `PurchaseWindow` has a `TabContainer` with a `VisibleStash` that exposes `VisibleInventoryItems` directly — no recursive element search needed. MapNotify's current `FindMapItems()` recursive search is unnecessary and should be replaced. There are also TWO separate purchase windows: `PurchaseWindow` (used in towns/maps) and `PurchaseWindowHideout` (used in hideout) — both need to be checked.

**Guild stash + regular stash elegant pattern from Ninja Price:**
```csharp
// Automatically picks whichever stash is open — regular or guild
var stashPanel = (IngameUi.StashElement, IngameUi.GuildStashElement) switch
{
    ({ IsVisible: false }, { IsVisible: true, IsValid: true } gs) => gs,
    var (s, _) => s
};
// Then use stashPanel.VisibleStash.VisibleInventoryItems normally
```
This is cleaner than the current separate null-checks for each stash type.

**Faustus direct access (replaces recursive FindMapItems):**
```csharp
var purchaseItems = IngameUi.PurchaseWindow?.TabContainer?.VisibleStash is { IsVisible: true, VisibleInventoryItems: { Count: > 0 } items }
    ? items.Where(i => i?.Item?.HasComponent<MapKey>() == true).ToList()
    : null;
// Also check PurchaseWindowHideout with the same pattern
```

**`GetFaustusItems()` — current implementation (direct path, no recursion):**
```csharp
// Town / map Faustus
var pw = ingameState.IngameUi.PurchaseWindow;
if (pw?.IsVisible == true)
{
    var items = pw.TabContainer?.VisibleStash?.VisibleInventoryItems;
    if (items != null)
        result.AddRange(items.Where(i => i?.Item != null && i.Item.HasComponent<MapKey>()));
}
// Hideout Faustus
var pwh = ingameState.IngameUi.PurchaseWindowHideout;
if (pwh?.IsVisible == true)
{
    var items = pwh.TabContainer?.VisibleStash?.VisibleInventoryItems;
    if (items != null)
        result.AddRange(items.Where(i => i?.Item != null && i.Item.HasComponent<MapKey>()));
}
```
Result is cached in `_faustusItems` (500ms `TimeCache`). `Render()` checks `Settings.ShowBorderInFaustus` then iterates `_faustusItems.Value`. The old `FindMapItems()` recursive search has been removed.

### DevTree Usage

To find UI element paths for new windows:
1. Open DevTree plugin
2. Open the target window in-game
3. Click "Debug UI Hover Item" while hovering an item in the window
4. Read `PathFromRoot` and `Type` from the properties panel
5. `Type: InventoryItem` confirms it's a `NormalInventoryItem` castable element

Child index paths from DevTree (e.g. `PurchaseWindow)105->8->1->1->0`) can change between patches — prefer recursive search for windows without a dedicated ExileApi property.

---

## UI Architecture (DrawSettings.cs)

Native ImGui throughout — matches ExileCore plugin style.

| Tab | Contents |
|---|---|
| Mods | Search bar, `Button`-based collapsible group headers (`_groupOpen` dict), `Selectable` rows with brick toggle |
| Profiles | List with Load/Save/Del per profile, new profile input |
| Border | Checkboxes, colour pickers, sliders |
| Display | Checkboxes for tooltip fields, offset sliders |
| Debug | Raw mod name display with Copy/Copy All buttons |

**Group collapse fix**: `CollapsingHeader` and `Selectable` conflict — clicking a row can collapse the header. Fix: use `ImGui.Button` with `ButtonTextAlign = (0, 0.5f)` for headers, store open state in `Dictionary<string, bool> _groupOpen`. Button and Selectable are separate widgets with no interaction.

**Text overlap fix**: `Selectable` must be given explicit height for two-line rows:
```csharp
float rowH = ImGui.GetTextLineHeight() * 2f + 8f;
ImGui.Selectable($"##sel_{id}", selected, ImGuiSelectableFlags.DontClosePopups, new nuVector2(w, rowH));
// Then overlay text with DrawList.AddText at selMin + offsets
```

---

## Performance

### Current Approach
- `TimeCache<List<NormalInventoryItem>>` for inventory (50ms) and stash (500ms)
- `RefreshBorderCache()` — rebuilds flat `List<string>` of enabled/bricked mod types only when dictionary counts change. Avoids per-frame LINQ on `EnabledMods`/`BrickedMods`
- `SeenModsManager.FlushIfDirty()` — file write only on new mod discovery, not per-frame
- Faustus cached every 500ms via `TimeCache`

### Known Performance Issues
- `DrawMapBorders()` runs in `Render()` — should move to `Tick()` with double-buffer cache
- `ItemDetails.Update()` runs every frame on hover — acceptable since it only runs on the single hovered item, but `GetComponent<>` calls in render path violate ExileApi best practices

### Planned Refactor (from WheresMyShitMapsAt study)
```
Tick():
  - Scan inventory/stash/faustus for map items
  - Run mod matching against cached enabled/bricked lists
  - Write results to _updatingCache (Dictionary<long, BorderInfo> keyed by item.Address)
  - Atomic swap: (_activeCache, _updatingCache) = (_updatingCache, _activeCache)

Render():
  - Read _activeCache (no LINQ, no GetComponent)
  - Draw borders from pre-computed BorderInfo
```

---

## Mod Grouping System

Groups defined in `ModDataLoader.cs` `Groups` array — ordered prefix matching:
```csharp
("MapPlayerCurseVulnerability", "Curses"),
("ModPowerCharges",             "Memory Maps"),
// ...
("MapHexproof",                 "Other"),  // catch-all at bottom
```

`GetGroup(modType)` returns first matching group or `"Other"`. Groups appear alphabetically in UI with `"Other"` always last (`OrderBy(g => g.Key == "Other" ? "zzz" : g.Key)`).

---

## Profile System

Profiles saved to `config/MapNotify/profiles.json` as a flat dictionary keyed by name. Each profile stores `EnabledMods`, `BrickedMods`, `CustomModNames`. Default profile always exists. Activating a profile calls `ProfileManager.LoadProfile()` which overwrites the three settings dictionaries.

**Breaking change**: If `SeenModEntry.ModState` enum refactor happens, `profiles.json` must be deleted and recreated.

---

## Planned Features (deferred)

| Feature | Status | Notes |
|---|---|---|
| Good/Want mod state | **Built** | Blue border by default. `GoodMods` dict in settings + profile. `BoxForMapGoodMods` toggle + `GoodModBorder` color in Border tab. `GoodPill` button in Mods tab. Mutually exclusive with Bricked. |
| Rarity/Currency/Scarabs/Maps display | **Built** | `Currency`, `Scarabs`, `MapDrop` on `ItemDetails`. Stat keys: `map_currency_drop_chance_+%_final_from_uber_mod`, `map_scarab_drop_chance_+%_final_from_uber_mod`, `map_map_item_drop_chance_+%_final_from_uber_mod` (confirmed via Debug tab). All shown in gold on tooltip, SameLine together. Toggleable via `ShowCurrencyPercent`, `ShowScarabPercent`, `ShowMapDropPercent`. |
| ModState enum refactor | Planned | Collapse `EnabledMods`+`BrickedMods`+`CustomModNames` into single `SeenModEntry` field |
| Tick()+HighlightCache border refactor | Planned | Move border scanning off render thread, double-buffer pattern from WheresMyShitMapsAt |

---

## Build

- NO manual build — Loader.exe auto-compiles from `Plugins/Source/`
- Target framework: `net10.0-windows`, OutputType: Library
- Output path: `Plugins/Temp/MapNotify/`
- Data files: `data/map_mods_data.json` copied via csproj `<Content CopyToOutputDirectory="PreserveNewest">`
- Config written to: `config/MapNotify/` (profiles.json, seen_mods.json)  