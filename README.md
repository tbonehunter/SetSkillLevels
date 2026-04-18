# README.md
# Set Skill Levels

A [Stardew Valley](https://www.stardewvalley.net/) mod that lets you adjust your skill levels up or down through a simple in-game menu. Built for SMAPI 4.0+.

> **Attribution:** The core skill-increase logic in this mod is a direct transcription of
> [CJBCheatsMenu](https://www.nexusmods.com/stardewvalley/mods/4) by CJBok and Pathoschild.
> Specifically, the `gainExperience` → suppress `newLevels` → show `LevelUpMenu` pattern and
> the XP tables are taken from `CJBCheatsMenu.Framework.Cheats.Skills.SkillsCheat`.
> Profession-stripping logic mirrors `CJBCheatsMenu.Framework.Cheats.Skills.ProfessionsCheat`.
> Full credit to those authors for the proven approach.

## Features

- **Increase any skill** (Farming, Mining, Foraging, Fishing, Combat) one level at a time using **+** buttons. Each increase triggers the vanilla profession-choice screen at levels 5 and 10, exactly as if you leveled up naturally.
- **Decrease any skill** one level at a time using **−** buttons (disabled by default — see *Configuration*).
- **XP / Level mismatch detection** — on save load, the mod scans all five skills. If XP values and skill levels are out of sync (e.g. from a previous mod issue), a popup lets you choose how to fix it:
  - Keep skill levels, adjust XP to match
  - Keep XP values, adjust skill levels to match
  - Ignore
- **GMCM integration** — configure all options through [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) or by editing `config.json` directly.
- **Compatible** with CJB Cheats Menu, CheatAnon, SpaceCore, and other common mods. Tested running concurrently without errors.

## Requirements

- [Stardew Valley](https://www.stardewvalley.net/) 1.6+
- [SMAPI](https://smapi.io/) 4.0+
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional, for in-game config)

## Installation

1. Download `SetSkillLevels 1.0.0.zip` from the [releases](https://github.com/tbonehunter/SetSkillLevels) page.
2. Extract it into your `Stardew Valley/Mods` folder so you have `Mods/SetSkillLevels/`.
3. Launch the game through SMAPI.

## Usage

1. Load a save.
2. Press the **+** key (default keybind) to open the Skill Adjuster menu.
3. Click the **+** button next to a skill to increase it by one level. You'll see the vanilla level-up / profession screen.
4. Click the **−** button next to a skill to decrease it by one level (requires `AllowSkillRegression` enabled).
5. Close the menu with the X button, right-click, or Escape.

## Configuration

Edit `config.json` or use GMCM in-game:

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AllowSkillRegression` | bool | `false` | Enables the − buttons. When disabled, attempting to decrease a skill shows a HUD warning. |
| `StripInvalidProfessions` | bool | `false` | When a skill is regressed below level 5 or 10, automatically removes professions unlocked at those tiers. Health bonuses from Fighter and Defender are adjusted accordingly. |
| `OpenMenuKey` | keybind | `OemPlus` (+) | The key that opens the Skill Adjuster menu. |

### Regression warnings

Lowering skill levels can have significant gameplay consequences:

- Crafting recipes tied to a skill level may no longer be available.
- If `StripInvalidProfessions` is enabled, professions and their passive bonuses are removed.
- If `StripInvalidProfessions` is disabled, you keep professions despite not meeting the level requirement — this may cause unexpected behavior.

## How it works

**Increasing** a skill uses the same method as CJB Cheats Menu:
1. Call `Farmer.gainExperience()` with the exact XP needed to reach the next level.
2. Suppress the entry added to `Farmer.newLevels` (prevents the overnight level-up popup).
3. Immediately show `LevelUpMenu` so the player can pick professions at levels 5 and 10.

**Decreasing** a skill directly writes:
1. `Farmer.experiencePoints[skillId]` to the cumulative XP for the target level.
2. The skill level field (`farmingLevel`, `fishingLevel`, etc.) to the new level.
3. Optionally strips professions and adjusts Fighter/Defender health bonuses.

**Mismatch detection** on save load compares each skill's level field against the level implied by its cumulative XP. If they differ, a question dialogue offers three resolution options.

## Credits

- **CJBok** and **Pathoschild** — [CJB Cheats Menu](https://www.nexusmods.com/stardewvalley/mods/4) for the skill-increase pattern and profession toggle logic that this mod is built upon.
- **Pathoschild** — [SMAPI](https://smapi.io/) and the Mod Build Config package.
- **spacechase0** — [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) API.

## License

This mod is provided as-is for personal use. The CJB-derived code is used with attribution; see the CJB Cheats Menu license for terms governing that code.
