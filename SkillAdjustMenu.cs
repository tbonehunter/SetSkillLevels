// SkillAdjustMenu.cs
//
// Standalone skill-level menu. The IncreaseSkill method is based on
// CJBCheatsMenu.Framework.Cheats.Skills.SkillsCheat.IncreaseSkill,
// modified to keep the menu open for non-profession level increases.
//
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace SetSkillLevels;

internal class SkillAdjustMenu : IClickableMenu
{
    // ---- Skill definitions (same order as CJB) ----
    private static readonly (string Key, int Id)[] Skills =
    {
        ("skill.farming",  0),
        ("skill.mining",   3),
        ("skill.foraging", 2),
        ("skill.fishing",  1),
        ("skill.combat",   4),
    };

    // ---- XP tables ----
    /// <summary>Cumulative XP required to reach each level (index = level).</summary>
    internal static readonly int[] CumulativeXpAtLevel =
        { 0, 100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000 };

    // ---- Layout ----
    private const int MenuWidth    = 600;
    private const int HeaderHeight = 96;
    private const int RowHeight    = 72;
    private const int FooterHeight = 48;
    private const int ContentLeft  = 48;
    private const int BtnScale     = 4;
    private const int BtnW         = 7 * BtnScale;  // 28
    private const int BtnH         = 8 * BtnScale;  // 32

    private static readonly Rectangle PlusSrc  = new(184, 345, 7, 8);
    private static readonly Rectangle MinusSrc = new(177, 345, 7, 8);

    private readonly Rectangle[] PlusBounds  = new Rectangle[5];
    private readonly Rectangle[] MinusBounds = new Rectangle[5];

    private readonly ModConfig              Config;
    private readonly IMonitor               Monitor;
    private readonly ITranslationHelper     Translation;

    public SkillAdjustMenu(ModConfig config, IMonitor monitor, ITranslationHelper translation)
        : base(
            (Game1.uiViewport.Width  - MenuWidth) / 2,
            (Game1.uiViewport.Height - (HeaderHeight + Skills.Length * RowHeight + FooterHeight)) / 2,
            MenuWidth,
            HeaderHeight + Skills.Length * RowHeight + FooterHeight,
            showUpperRightCloseButton: true)
    {
        Config      = config;
        Monitor     = monitor;
        Translation = translation;
        RepositionElements();
        Game1.playSound("bigSelect");
    }

    private void RepositionElements()
    {
        int plusX  = xPositionOnScreen + MenuWidth - ContentLeft - BtnW;
        int minusX = plusX - BtnW - 12;
        for (int i = 0; i < Skills.Length; i++)
        {
            int rowMidY = yPositionOnScreen + HeaderHeight + i * RowHeight + (RowHeight - BtnH) / 2;
            PlusBounds[i]  = new Rectangle(plusX,  rowMidY, BtnW, BtnH);
            MinusBounds[i] = new Rectangle(minusX, rowMidY, BtnW, BtnH);
        }
        initializeUpperRightCloseButton();
    }

    // ---- Input ----

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (upperRightCloseButton?.containsPoint(x, y) == true && readyToClose())
        {
            exitThisMenu(playSound);
            return;
        }

        for (int i = 0; i < Skills.Length; i++)
        {
            if (PlusBounds[i].Contains(x, y))
            {
                IncreaseSkill(Skills[i].Id);
                return;
            }
            if (MinusBounds[i].Contains(x, y))
            {
                DecreaseSkill(Skills[i].Id, Skills[i].Key);
                return;
            }
        }
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (readyToClose()) exitThisMenu(playSound);
    }

    public override void receiveKeyPress(Keys key)
    {
        if (Game1.options.menuButton.Contains(new InputButton(key)) && readyToClose())
            exitThisMenu();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        xPositionOnScreen = (Game1.uiViewport.Width  - MenuWidth) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height)    / 2;
        RepositionElements();
    }

    // ---- Drawing ----

    public override void draw(SpriteBatch b)
    {
        if (!Game1.options.showClearBackgrounds)
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * 0.5f);

        Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

        string title = Translation.Get("menu.title");
        Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
            new Vector2(
                xPositionOnScreen + (MenuWidth - (int)Game1.dialogueFont.MeasureString(title).X) / 2,
                yPositionOnScreen + 52),
            Game1.textColor);

        for (int i = 0; i < Skills.Length; i++)
        {
            int currentLevel = Game1.player.GetSkillLevel(Skills[i].Id);
            int textY = yPositionOnScreen + HeaderHeight + i * RowHeight
                        + (RowHeight - Game1.dialogueFont.LineSpacing) / 2;

            string label = $"{Translation.Get(Skills[i].Key)} Lvl: {currentLevel}";
            Utility.drawTextWithShadow(b, label, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + ContentLeft, textY),
                Game1.textColor);

            // + button (disabled at max level)
            bool plusDisabled = currentLevel >= 10;
            Utility.drawWithShadow(b, Game1.mouseCursors,
                new Vector2(PlusBounds[i].X, PlusBounds[i].Y),
                PlusSrc,
                plusDisabled ? Color.Gray * 0.5f : Color.White,
                0f, Vector2.Zero, BtnScale, false, 0.87f);

            // - button (disabled at level 0 or when regression not allowed)
            bool minusDisabled = currentLevel <= 0 || !Config.AllowSkillRegression;
            Utility.drawWithShadow(b, Game1.mouseCursors,
                new Vector2(MinusBounds[i].X, MinusBounds[i].Y),
                MinusSrc,
                minusDisabled ? Color.Gray * 0.5f : Color.White,
                0f, Vector2.Zero, BtnScale, false, 0.87f);
        }

        base.draw(b);
        drawMouse(b);
    }

    // ---- Skill increment: based on CJBCheatsMenu SkillsCheat logic ----

    /// <summary>
    /// Exact XP values from CJBCheatsMenu.Framework.Cheats.Skills.SkillsCheat.GetExperiencePoints.
    /// Returns incremental XP needed to advance from the given level to level+1.
    /// </summary>
    private static int GetExperiencePoints(int level)
    {
        if (level < 0 || level > 9)
            return 0;
        return new int[] { 100, 280, 390, 530, 850, 1150, 1500, 2100, 3100, 5000 }[level];
    }

    /// <summary>
    /// Increase a skill by one level. For profession levels (5 and 10), closes the menu
    /// and shows the vanilla LevelUpMenu for profession selection. For all other levels,
    /// stays in the Skill Adjuster menu so the player can continue clicking.
    /// </summary>
    private void IncreaseSkill(int skillId)
    {
        int currentLevel = Game1.player.GetSkillLevel(skillId);
        if (currentLevel >= 10)
            return;

        int expToNext = GetExperiencePoints(currentLevel);
        IList<Point> newLevels = (IList<Point>)Game1.player.newLevels;
        int wasNewLevels = newLevels.Count;
        Game1.player.gainExperience(skillId, expToNext);

        if (newLevels.Count > wasNewLevels)
            newLevels.RemoveAt(newLevels.Count - 1);

        int newLevel = Game1.player.GetSkillLevel(skillId);
        Monitor.Log($"Increased skill {skillId} to {newLevel}.", LogLevel.Info);

        // Profession levels (5 and 10) require the vanilla LevelUpMenu for profession selection
        if (newLevel == 5 || newLevel == 10)
        {
            Game1.exitActiveMenu();
            Game1.activeClickableMenu = new LevelUpMenu(skillId, newLevel);
        }
        else
        {
            // Non-profession level: stay in this menu, just play a sound
            Game1.playSound("newArtifact");
        }
    }

    // ---- Skill decrement ----

    /// <summary>Decrease a skill by one level, setting XP and level directly.</summary>
    private void DecreaseSkill(int skillId, string skillKey)
    {
        int currentLevel = Game1.player.GetSkillLevel(skillId);
        if (currentLevel <= 0)
            return;

        if (!Config.AllowSkillRegression)
        {
            string skillName = Translation.Get(skillKey);
            Game1.addHUDMessage(new HUDMessage(Translation.Get("hud.regression-blocked", new { skill = skillName })));
            return;
        }

        int newLevel = currentLevel - 1;
        Game1.player.experiencePoints[skillId] = CumulativeXpAtLevel[newLevel];
        SetSkillLevel(Game1.player, skillId, newLevel);

        if (Config.StripInvalidProfessions)
            StripProfessionsAboveLevel(skillId, newLevel);

        Monitor.Log($"Decreased skill {skillId} to {newLevel}.", LogLevel.Info);
        Game1.playSound("cancel");
    }

    // ---- Helpers (internal static for use by ModEntry sync check) ----

    /// <summary>Compute the skill level implied by a cumulative XP value.</summary>
    internal static int LevelFromXp(int xp)
    {
        for (int i = CumulativeXpAtLevel.Length - 1; i >= 0; i--)
        {
            if (xp >= CumulativeXpAtLevel[i])
                return i;
        }
        return 0;
    }

    /// <summary>Directly set a skill's level field on the farmer.</summary>
    internal static void SetSkillLevel(Farmer player, int skillId, int level)
    {
        switch (skillId)
        {
            case 0: player.farmingLevel.Value  = level; break;
            case 1: player.fishingLevel.Value  = level; break;
            case 2: player.foragingLevel.Value = level; break;
            case 3: player.miningLevel.Value   = level; break;
            case 4: player.combatLevel.Value   = level; break;
        }
    }

    /// <summary>
    /// Remove professions that the player no longer qualifies for after regression.
    /// Level 5 professions: skillId*6 and skillId*6+1.
    /// Level 10 professions: skillId*6+2 through skillId*6+5.
    /// Mirrors CJBCheatsMenu's health-bonus handling for Fighter (24) and Defender (27).
    /// </summary>
    private static void StripProfessionsAboveLevel(int skillId, int newLevel)
    {
        int baseId = skillId * 6;
        Farmer player = Game1.player;
        bool changed = false;

        if (newLevel < 10)
        {
            for (int p = baseId + 2; p <= baseId + 5; p++)
            {
                if (player.professions.Contains(p))
                {
                    int healthBonus = p switch { 27 => 25, _ => 0 };
                    player.health -= healthBonus;
                    player.maxHealth -= healthBonus;
                    player.professions.Remove(p);
                    changed = true;
                }
            }
        }

        if (newLevel < 5)
        {
            for (int p = baseId; p <= baseId + 1; p++)
            {
                if (player.professions.Contains(p))
                {
                    int healthBonus = p switch { 24 => 15, _ => 0 };
                    player.health -= healthBonus;
                    player.maxHealth -= healthBonus;
                    player.professions.Remove(p);
                    changed = true;
                }
            }
        }

        if (changed)
            LevelUpMenu.RevalidateHealth(player);
    }
}
