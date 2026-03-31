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
            ("ModPowerCharges",               "Uber Mods"),
            ("PossessedBoss",                 "Uber Mods"),
            ("ModFrenzyCharges",              "Uber Mods"),
            ("ModEnduranceCharges",           "Uber Mods"),
            ("ModDamageAsChaos",              "Uber Mods"),
            ("ModMonsterLife",                "Uber Mods"),
            ("ModEleDamagePenetration",       "Uber Mods"),
            ("ModAOEPlayer",                  "Uber Mods"),
            ("RaresLastGasp",                 "Uber Mods"),
            ("Juggernaut",                    "Uber Mods"),
            ("MonsterAilments",               "Uber Mods"),
            ("LifeAsEnergyShield",            "Uber Mods"),
            ("LabyrinthHazards",              "Uber Mods"),
            ("MonsterSuppress",               "Uber Mods"),
            ("ModMinusMax",                   "Uber Mods"),
            ("ModChain",                      "Uber Mods"),
            ("ResistancePDR",                 "Uber Mods"),
            ("AuraAffectMonsters",            "Uber Mods"),
            ("FlaskMeteor",                   "Uber Mods"),
            ("ModProjAOE",                    "Uber Mods"),
            ("PlayersMarked",                 "Uber Mods"),
            ("GlobalDefences",                "Uber Mods"),
            ("Exarch",                        "Uber Mods"),
            ("DamageAsElemental",             "Uber Mods"),
            ("MavenFollower",                 "Uber Mods"),
            ("Volatiles",                     "Uber Mods"),
            ("Vines",                         "Uber Mods"),
            ("ElderNovas",                    "Uber Mods"),
            ("MonsterAttackBlock",            "Uber Mods"),
            ("ModCrit",                       "Uber Mods"),
            ("ModRareMonsters",               "Uber Mods"),
            ("Sawblades",                     "Uber Mods"),
            ("ShaperTouched",                 "Uber Mods"),
            ("ShrineBuff",                    "Uber Mods"),
            ("BuffExpiry",                    "Uber Mods"),
            ("ModTurbo",                      "Uber Mods"),
            ("DrowningOrbs",                  "Uber Mods"),
            ("LessFlaskEffect",               "Uber Mods"),
            ("ModCurses",                     "Uber Mods"),
            ("ModPoison",                     "Uber Mods"),
            ("MonsterDebuffSpeed",            "Uber Mods"),
            ("CritDefence",                   "Uber Mods"),
            ("RareFractures",                 "Uber Mods"),
            ("LessPlayerLeech",               "Uber Mods"),
            ("ModReflect",                    "Uber Mods"),
            ("ModMonsterDamage",              "Uber Mods"),




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
