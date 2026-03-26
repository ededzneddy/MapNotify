using ExileCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MapNotify
{
    public class MapNotifyProfile
    {
        public string Name                      { get; set; } = "Default";
        public DateTime LastSaved               { get; set; } = DateTime.UtcNow;
        public Dictionary<string, bool> Mods   { get; set; } = new();
        public Dictionary<string, bool> Bricked { get; set; } = new();
        public Dictionary<string, string> CustomNames { get; set; } = new();
    }

    public class ProfileManager
    {
        private readonly string _path;
        public Dictionary<string, MapNotifyProfile> Profiles { get; private set; } = new();
        public List<string> ProfileNames => Profiles.Keys.OrderBy(k => k).ToList();

        public ProfileManager(string configDir)
        {
            _path = Path.Combine(configDir, "profiles.json");
            Load();
            if (!Profiles.ContainsKey("Default"))
            {
                Profiles["Default"] = new MapNotifyProfile { Name = "Default" };
                Save();
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                Profiles = JsonConvert.DeserializeObject<Dictionary<string, MapNotifyProfile>>(
                    File.ReadAllText(_path)) ?? new Dictionary<string, MapNotifyProfile>();
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"[MapMods] Profile load failed: {ex.Message}");
                Profiles = new Dictionary<string, MapNotifyProfile>();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonConvert.SerializeObject(Profiles, Formatting.Indented));
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"[MapMods] Profile save failed: {ex.Message}");
            }
        }

        public void SaveProfile(string name, MapNotifySettings s)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = Profiles.GetValueOrDefault(name) ?? new MapNotifyProfile();
            p.Name      = name;
            p.LastSaved = DateTime.UtcNow;
            p.Mods      = new Dictionary<string, bool>(s.EnabledMods);
            p.Bricked      = new Dictionary<string, bool>(s.BrickedMods);
            p.CustomNames  = new Dictionary<string, string>(s.CustomModNames);
            Profiles[name] = p;
            Save();
        }

        public bool LoadProfile(string name, MapNotifySettings s)
        {
            if (!Profiles.TryGetValue(name, out var p)) return false;
            s.EnabledMods = new Dictionary<string, bool>(p.Mods);
            s.BrickedMods     = new Dictionary<string, bool>(p.Bricked ?? new Dictionary<string, bool>());
            s.CustomModNames  = new Dictionary<string, string>(p.CustomNames ?? new Dictionary<string, string>());
            return true;
        }

        public void DeleteProfile(string name)
        {
            if (name == "Default") return;
            Profiles.Remove(name);
            Save();
        }

        public void DuplicateProfile(string source, string newName)
        {
            if (!Profiles.TryGetValue(source, out var src)) return;
            Profiles[newName] = new MapNotifyProfile
            {
                Name      = newName,
                LastSaved = DateTime.UtcNow,
                Mods      = new Dictionary<string, bool>(src.Mods),
                Bricked      = new Dictionary<string, bool>(src.Bricked ?? new Dictionary<string, bool>()),
                CustomNames  = new Dictionary<string, string>(src.CustomNames ?? new Dictionary<string, string>())
            };
            Save();
        }
    }
}
