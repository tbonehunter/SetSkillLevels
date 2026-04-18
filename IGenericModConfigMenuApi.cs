// IGenericModConfigMenuApi.cs
using System;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SetSkillLevels;

/// <summary>The GMCM mod API, extended to include integer number options.</summary>
public interface IGenericModConfigMenuApi
{
    /// <summary>Register a mod whose config can be edited through the menu.</summary>
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

    /// <summary>Add a section title at the current position in the form.</summary>
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string>? tooltip = null);

    /// <summary>Add a paragraph of text at the current position in the form.</summary>
    void AddParagraph(IManifest mod, Func<string> text);

    /// <summary>Add a boolean toggle at the current position in the form.</summary>
    void AddBoolOption(
        IManifest mod,
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null);

    /// <summary>Add an integer number field with +/- buttons at the current position in the form.</summary>
    void AddNumberOption(
        IManifest mod,
        Func<int> getValue,
        Action<int> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        int? min = null,
        int? max = null,
        int? interval = null,
        Func<int, string>? formatValue = null,
        string? fieldId = null);

    /// <summary>Add a keybind-list field at the current position in the form.</summary>
    void AddKeybindList(
        IManifest mod,
        Func<KeybindList> getValue,
        Action<KeybindList> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null);
}
