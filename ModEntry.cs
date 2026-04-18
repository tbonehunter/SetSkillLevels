// ModEntry.cs
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SetSkillLevels;

/// <summary>The mod entry point.</summary>
internal class ModEntry : Mod
{
    private ModConfig Config = null!;
    private bool _checkSyncOnNextTick;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded   += OnSaveLoaded;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonPressed   += OnButtonPressed;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        IGenericModConfigMenuApi? gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm is null)
            return;

        gmcm.Register(
            mod:             ModManifest,
            reset:           () => Config = new ModConfig(),
            save:            () => Helper.WriteConfig(Config),
            titleScreenOnly: false
        );

        gmcm.AddSectionTitle(ModManifest, () => Helper.Translation.Get("section.options"));

        gmcm.AddBoolOption(
            mod:      ModManifest,
            getValue: () => Config.AllowSkillRegression,
            setValue: v  => Config.AllowSkillRegression = v,
            name:     () => Helper.Translation.Get("option.allow-regression.name"),
            tooltip:  () => Helper.Translation.Get("option.allow-regression.tooltip")
        );

        gmcm.AddBoolOption(
            mod:      ModManifest,
            getValue: () => Config.StripInvalidProfessions,
            setValue: v  => Config.StripInvalidProfessions = v,
            name:     () => Helper.Translation.Get("option.strip-professions.name"),
            tooltip:  () => Helper.Translation.Get("option.strip-professions.tooltip")
        );

        gmcm.AddKeybindList(
            mod:      ModManifest,
            getValue: () => Config.OpenMenuKey,
            setValue: v  => Config.OpenMenuKey = v,
            name:     () => Helper.Translation.Get("option.open-menu-key.name"),
            tooltip:  () => Helper.Translation.Get("option.open-menu-key.tooltip")
        );
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _checkSyncOnNextTick = true;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!_checkSyncOnNextTick || !Context.IsWorldReady)
            return;

        if (Game1.activeClickableMenu is not null)
            return;

        _checkSyncOnNextTick = false;
        CheckForSkillXpMismatch();
    }

    private void CheckForSkillXpMismatch()
    {
        var mismatches = new List<(int SkillId, string Name, int ActualLevel, int XpLevel)>();

        (string Name, int Id)[] skills =
        {
            ("Farming", 0), ("Fishing", 1), ("Foraging", 2), ("Mining", 3), ("Combat", 4)
        };

        foreach (var (name, id) in skills)
        {
            int actualLevel = Game1.player.GetSkillLevel(id);
            int xp = Game1.player.experiencePoints[id];
            int xpLevel = SkillAdjustMenu.LevelFromXp(xp);

            if (actualLevel != xpLevel)
                mismatches.Add((id, name, actualLevel, xpLevel));
        }

        if (mismatches.Count == 0)
            return;

        string details = string.Join(", ", mismatches.ConvertAll(m =>
            $"{m.Name}: Level {m.ActualLevel} but XP implies Level {m.XpLevel}"));

        string question = Helper.Translation.Get("sync.question", new { details });

        Game1.currentLocation.createQuestionDialogue(
            question,
            new[]
            {
                new Response("keepLevel", Helper.Translation.Get("sync.keep-levels")),
                new Response("keepXp",    Helper.Translation.Get("sync.keep-xp")),
                new Response("ignore",    Helper.Translation.Get("sync.ignore")),
            },
            (who, answer) =>
            {
                switch (answer)
                {
                    case "keepLevel":
                        foreach (var m in mismatches)
                        {
                            Game1.player.experiencePoints[m.SkillId] =
                                SkillAdjustMenu.CumulativeXpAtLevel[m.ActualLevel];
                            Monitor.Log($"Synced {m.Name} XP to match level {m.ActualLevel}.", LogLevel.Info);
                        }
                        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("sync.fixed-xp")));
                        break;

                    case "keepXp":
                        foreach (var m in mismatches)
                        {
                            SkillAdjustMenu.SetSkillLevel(Game1.player, m.SkillId, m.XpLevel);
                            Monitor.Log($"Synced {m.Name} level to {m.XpLevel} (from XP).", LogLevel.Info);
                        }
                        Game1.addHUDMessage(new HUDMessage(Helper.Translation.Get("sync.fixed-levels")));
                        break;

                    case "ignore":
                        Monitor.Log("User chose to ignore skill/XP mismatch.", LogLevel.Info);
                        break;
                }
            }
        );
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Config.OpenMenuKey.JustPressed() && Game1.activeClickableMenu is null)
            Game1.activeClickableMenu = new SkillAdjustMenu(Config, Monitor, Helper.Translation);
    }
}
