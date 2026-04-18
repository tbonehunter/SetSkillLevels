// ModConfig.cs
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SetSkillLevels;

/// <summary>The mod configuration, persisted to config.json.</summary>
internal class ModConfig
{
    /// <summary>
    /// Whether the mod is allowed to decrease a skill below its current level.
    /// Regression can cause significant in-game side effects. Default is false.
    /// </summary>
    public bool AllowSkillRegression { get; set; } = false;

    /// <summary>
    /// When regression is applied, whether professions unlocked at stripped tiers
    /// (levels 5 and 10) are automatically removed. Default is false.
    /// </summary>
    public bool StripInvalidProfessions { get; set; } = false;

    /// <summary>The key that opens the in-game Skill Adjuster menu.</summary>
    public KeybindList OpenMenuKey { get; set; } = new KeybindList(SButton.OemPlus);
}
