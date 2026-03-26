using ExileCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace MapNotify
{
    // Mirrors the structure of map_mods_data.json
    public class MapModEntry
    {
        [JsonProperty("Mod type")]  public string ModType { get; set; }
        [JsonProperty("Name")]      public string Name    { get; set; }
        [JsonProperty("Effect")]    public string Effect  { get; set; }
    }

    public static class ModDataLoader
    {
        // Groups defined by mod type prefix rules
        // Order matters — first match wins
        private static readonly (string prefix, string group)[] Groups =
        {
            ("MonsterElementalReflection",    "Reflect & Regen"),
            ("MonsterPhysicalReflection",     "Reflect & Regen"),
            ("PlayerNoLifeESRegen",           "Reflect & Regen"),
            ("PlayerReducedRegen",            "Reflect & Regen"),
			("MonsterLifeLeechImmunity",      "Reflect & Regen"),

            ("PlayerCurseVu",                 "Curses"),
			("PlayerCurseTe",                 "Curses"),
			("PlayerCurseEn",                 "Curses"),
			("PlayerCurseEl",                 "Curses"),

            ("PlayerMaxResists",              "Player Debuffs"),
            ("PlayersReducedAuraEffect",      "Player Debuffs"),
            ("PlayersLessCooldown",           "Player Debuffs"),
            ("PlayersLessAoERadius",          "Player Debuffs"),
            ("PlayersGainReduced",            "Player Debuffs"),
            ("PlayersBuffsExpire",            "Player Debuffs"),
            ("PlayersBlockAndArmour",         "Player Debuffs"),
            ("PlayersAccuracy",               "Player Debuffs"),
			("PlayersEvasionAndUnlucky",      "Player Debuffs"),

            ("TwoBosses",                     "Bosses"),
			("BossAreaOfEffect",              "Bosses"),
            ("DangerousBoss",                 "Bosses"),
            ("MassiveBoss",                   "Bosses"),
            ("MapBossPossessed",              "Bosses"),
			("BossSurrounded",                "Bosses"),

            ("MapMonsterLife",                "Monster Buffs"),
            ("MapMonsterDamage",              "Monster Buffs"),
            ("MonsterFast",                   "Monster Buffs"),
            ("MonsterCritical",               "Monster Buffs"),
            ("MonsterPhysicalResistance",     "Monster Buffs"),
            ("MonstersAllResistances",        "Monster Buffs"),
            ("MonsterFireResistance",         "Monster Buffs"),
            ("MonsterColdResistance",         "Monster Buffs"),
            ("MonsterLightningResistance",    "Monster Buffs"),
            ("MonsterAreaOfEffect",           "Monster Buffs"),
            ("MonsterMultipleProjectiles",    "Monster Buffs"),
            ("MonstersCantBeSlowed",          "Monster Buffs"),
            ("MonstersMaximumLife",           "Monster Buffs"),
            ("MonsterChain",                  "Monster Buffs"),
            ("MonstersAvoidPoison",           "Monster Buffs"),
            ("MonsterStatusAilmentImmunity",  "Monster Buffs"),
			("Hexproof",                      "Monster Buffs"),
            ("PlayerElementalEquilibrium",    "Monster Buffs"),
			("MonstersChanceToInflict",       "Monster Buffs"),
			("Poisoning",                     "Monster Buffs"),
			
            ("MonsterFireDamage",             "Monster Damage Types"),
            ("MonsterColdDamage",             "Monster Damage Types"),
            ("MonsterLightningDamage",        "Monster Damage Types"),
            ("MonsterChaosDamage",            "Monster Damage Types"),

            ("MonstersFrenzy",                "Monster Mechanics"),
            ("MonstersEndurance",             "Monster Mechanics"),
            ("MonstersPower",                 "Monster Mechanics"),
            ("MonstersStealCharges",          "Monster Mechanics"),
            ("MonstersCurseEffectOnSelf",     "Monster Mechanics"),
            ("MonstersHinder",                "Monster Mechanics"),
            ("MonstersMaim",                  "Monster Mechanics"),
            ("MonstersImpale",                "Monster Mechanics"),
            ("MonstersBlind",                 "Monster Mechanics"),
            ("MonstersChanceToSuppress",      "Monster Mechanics"),
			("NemesisModOnRares",             "Monster Mechanics"),
			("BloodlinesModOnMagics",         "Monster Mechanics"),
			("MonstersAllDamage",             "Monster Mechanics"),
			("MonstersBaseSelf",              "Monster Mechanics"),
			("MonsterCannotBeStunned",        "Monster Mechanics"),
			
            ("BurningGround",                 "Ground Effects"),
            ("ChilledGround",                 "Ground Effects"),
            ("ShockedGround",                 "Ground Effects"),
            ("DesecratedGround",              "Ground Effects"),
            ("ConsecratedGround",             "Ground Effects"),

            // Memory map exclusive mods
            ("ModPowerCharges",               "Memory Maps"),
            ("PossessedBoss",                 "Memory Maps"),
            ("ModFrenzyCharges",              "Memory Maps"),
            ("ModEnduranceCharges",           "Memory Maps"),
            ("ModDamageAsChaos",              "Memory Maps"),
            ("ModMonsterLife",                "Memory Maps"),
            ("ModEleDamagePenetration",       "Memory Maps"),
            ("ModAOEPlayer",                  "Memory Maps"),
            ("RaresLastGasp",                 "Memory Maps"),
            ("Juggernaut",                    "Memory Maps"),
            ("MonsterAilments",               "Memory Maps"),
            ("LifeAsEnergyShield",            "Memory Maps"),
            ("LabyrinthHazards",              "Memory Maps"),
            ("MonsterSuppress",               "Memory Maps"),
            ("ModMinusMax",                   "Memory Maps"),
            ("ModChain",                      "Memory Maps"),
            ("ResistancePDR",                 "Memory Maps"),
            ("AuraAffectMonsters",            "Memory Maps"),
            ("FlaskMeteor",                   "Memory Maps"),
            ("ModProjAOE",                    "Memory Maps"),
            ("PlayersMarked",                 "Memory Maps"),
            ("GlobalDefences",                "Memory Maps"),
            ("Exarch",                        "Memory Maps"),
            ("DamageAsElemental",             "Memory Maps"),
            ("MavenFollower",                 "Memory Maps"),
            ("Volatiles",                     "Memory Maps"),
            ("Vines",                         "Memory Maps"),
            ("ElderNovas",                    "Memory Maps"),
            ("MonsterAttackBlock",            "Memory Maps"),
            ("ModCrit",                       "Memory Maps"),
            ("ModRareMonsters",               "Memory Maps"),
            ("Sawblades",                     "Memory Maps"),
            ("ShaperTouched",                 "Memory Maps"),
            ("ShrineBuff",                    "Memory Maps"),
            ("BuffExpiry",                    "Memory Maps"),
            ("ModTurbo",                      "Memory Maps"),
            ("DrowningOrbs",                  "Memory Maps"),
            ("LessFlaskEffect",               "Memory Maps"),
            ("ModCurses",                     "Memory Maps"),
            ("ModPoison",                     "Memory Maps"),
            ("MonsterDebuffSpeed",            "Memory Maps"),
            ("CritDefence",                   "Memory Maps"),
            ("RareFractures",                 "Memory Maps"),
            ("LessPlayerLeech",               "Memory Maps"),
            ("ModReflect",                    "Memory Maps"),
            ("ModMonsterDamage",              "Memory Maps"),




        };

        public static string GetGroup(string modType)
        {
            foreach (var (prefix, group) in Groups)
                if (modType.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return group;
            return "Other";
        }

        public static List<MapModEntry> Load(string directoryFullName)
        {
            try
            {
                var path = Path.Combine(directoryFullName, "data", "map_mods_data.json");
                if (!File.Exists(path))
                {
                    DebugWindow.LogError($"[MapMods] map_mods_data.json not found at {path}");
                    return new List<MapModEntry>();
                }
                var result = JsonConvert.DeserializeObject<List<MapModEntry>>(File.ReadAllText(path))
                             ?? new List<MapModEntry>();
                DebugWindow.LogMsg($"[MapMods] Loaded {result.Count} mods from {path}");
                return result;
            }
            catch (System.Exception ex)
            {
                DebugWindow.LogError($"[MapMods] Failed to load map_mods_data.json: {ex.Message}");
                return new List<MapModEntry>();
            }
        }
    }
}
