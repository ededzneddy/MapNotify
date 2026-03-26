using ExileCore;
using System.Collections.Generic;

namespace MapNotify
{
    // Legacy txt-file warning system — kept as a stub so existing config files
    // are not deleted. The new profile/EnabledMods system is used by default.
    // These dictionaries are only used if EnabledMods is empty (fresh install).
    public partial class MapNotify : BaseSettingsPlugin<MapNotifySettings>
    {
        public Dictionary<string, StyledText> LoadConfigs()
            => new Dictionary<string, StyledText>();

        public Dictionary<string, StyledText> LoadConfigBadMod()
            => new Dictionary<string, StyledText>();

        public void ResetConfigs()
            => DebugWindow.LogMsg("[MapMods] Txt warning files removed from active use. Use the Mods tab instead.");
    }
}
