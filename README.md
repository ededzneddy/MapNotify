# MapNotify
Notifications about map mods, quantity, packsize and other useful information for immediate parsing

Also covers Heists, Watchstones and Maven Invitations, including instantly displaying which monsters you have killed for the uncharted realms series.

Holding alt over an elder guardian map will also display that information.

![Image](https://i.imgur.com/sST1Zxi.png)

![Image](https://i.imgur.com/4GY3sNf.png)

![Image](https://i.imgur.com/xYNbWjJ.png)


# Includes
**Map Completion:** Lists if a map has been Bonus, Awakening and Normal completed and if a map is actively witnessed by the Maven.

**Rarity, Tier and Map Name:** as it sounds.

**Mods Count:** as it sounds.

**Quantity Percentage:** as it sounds.

**Pack Size Percentage:** as it sounds.

**Mod Warnings:** Customisable in ModWarnings.txt, HeistWarnings.txt, SextantWarnings.txt and WatchstoneWarnings.txt. Ships with what you'd expected such as Elemental Reflect, Physical Reflect, No Regen, Enfeeble, Elemental Weakness, Vulnerability, Temporal Chains, Twinned warnings and good quantity and farming mods for watchstones.

**Maven Bosses:** Lists what bosses are witnessed for quest and uncharted realms Maven Invitations. Also includes mod warnings for the latter type if you configure them in ModWarnings.txt.

**Border on Maps:** Shows a border on map based on Warning or Bad Mods setting. If you are using Warning setting, modify ModWarnings.txt. If you are using Bad Mods only, use BadModWarnings.txt. Customizable Border Color, Border Thickness TBA.

Find modifiers at https://www.poewiki.net/wiki/List_of_modifiers_for_maps_(high_tier)

#########################################################################

# MapMods (formerly MapNotify)

An ExileCore plugin for Path of Exile 2 that shows map mod warnings, borders and tooltips when hovering maps in your inventory, stash and market windows.

Originally by [Lachrymatory](https://github.com/Lachrymatory), edited by [Xcesius](https://github.com/Xcesius/MapNotify/) and [Rushtothesun](https://github.com/Rushtothesun/MapNotify/).

---

## Features

- **Mod warnings** — hover any map to see which mods match your configured warnings
- **Inventory & stash borders** — colour-coded borders on maps with warning or bricked mods
- **8-mod split border** — top half shows 8-mod colour, bottom half shows warning/bricked colour
- **Profile system** — save and load named sets of mod selections
- **In-game mod selection UI** — click mods to enable warnings, toggle brick state per mod
- **Memory map support** — separate category for memory influenced map exclusive mods
- **Faustus market border** — optional border support in the Faustus purchase window
- **Debug tab** — hover a map to see raw mod names with copy buttons for easy JSON editing

---

## Changelog

### Current (fork rewrite)

**UI**
- Replaced flat settings panel with native ImGui tab bar (Mods / Profiles / Border / Display / Debug)
- Mods tab: collapsible groups, clickable rows to enable warnings, brick toggle button per mod
- Profiles tab: named profiles with Load / Save / Delete, active profile highlighted
- Border tab: toggles for warning/bricked/8-mod borders, colour pickers
- Display tab: toggles for all tooltip settings
- Debug tab: raw mod name display with per-mod and copy-all buttons

**Mod Selection**
- Replaced txt file warning system with JSON-driven mod selection (map_mods_data.json)
- All 137+ map mods loaded from JSON at startup, grouped by category
- Two warning states per mod: Warning (orange border) and Bricked (red border)
- Mod selections saved per profile
- Memory Maps category added for memory influenced map exclusive mods
- Custom mod names supported per profile

**Borders**
- Borders now driven entirely by the profile system
- Warning mods → warning colour, bricked mods → bricked colour, 8-mod → 8-mod colour
- Split border when both 8-mod and warning/bricked are present
- Added Faustus market window border support (off by default)
- Added guild stash border support

**Performance**
- Faustus item scan cached every 500ms via TimeCache
- Border mod lookup cached as flat list, only rebuilds when mod selections change
- Removed recursive per-frame LINQ queries from border drawing

**Removed**
- Heist contract and blueprint support
- Watchstone, sextant and heist warning txt files
- NinjaPricer padding settings
- Old txt file warning system (ModWarnings.txt, etc.) — replaced by profile system
- Map name display (not relevant in current patch)

---

## Installation

1. Copy the `MapNotify` folder to `Plugins/Source/`
2. Rebuild in ExileApi
3. The plugin will create `config/MapNotify/` on first run with a Default profile
4. Open plugin settings → Mods tab → click mods to enable warnings
5. Save your selections to a profile in the Profiles tab

---

## Adding Custom Mods

1. Open the Debug tab and hover a map in your inventory
2. Copy the `RawName` of the mod you want to track
3. Add an entry to `Source/MapNotify/data/map_mods_data.json`:

```json
{
    "Mod type": "YourModRawName",
    "Name": "Display Name",
    "Effect": "What the mod does"
}
```

4. Reload the plugin

---

## Grouping Custom Mods

Groups are defined by prefix rules in `Settings/ModDataLoader.cs`. Add a new entry to the `Groups` array before the `"Other"` entries:

```csharp
("YourModPrefix", "Your Group Name"),
```
