using BetterAmongUs.Data.Json;
using BetterAmongUs.Helpers;

namespace BetterAmongUs.Data;

/// <summary>
/// Manages data storage, settings, and ban lists for the BetterAmongUs mod.
/// </summary>
internal static class BetterDataManager
{
    /// <summary>
    /// The main data file containing outfit presets and cheat detection data.
    /// </summary>
    internal static BetterDataFile BetterDataFile = new();

    /// <summary>
    /// The game settings file with compressed storage.
    /// </summary>
    internal static BetterGameSettingsFile BetterGameSettingsFile = new();

    /// <summary>
    /// Legacy data file path (BetterData.json).
    /// </summary>
    internal static string dataPath_Legacy = GetFilePath("BetterData");

    /// <summary>
    /// Current data file path (BetterDataV2.json).
    /// </summary>
    internal static string dataPath = GetFilePath("BetterDataV2");

    /// <summary>
    /// Root directory for BetterAmongUs data.
    /// </summary>
    internal static string filePathFolder = Path.Combine(BAUPlugin.GetGamePathToAmongUs(), $"Better_Data");

    /// <summary>
    /// Directory for save information files.
    /// </summary>
    internal static string filePathFolderSaveInfo = Path.Combine(filePathFolder, $"SaveInfo");

    /// <summary>
    /// Directory for settings files.
    /// </summary>
    internal static string filePathFolderSettings = Path.Combine(filePathFolder, $"Settings");

    /// <summary>
    /// Directory for game replay files.
    /// </summary>
    internal static string filePathFolderReplays = Path.Combine(filePathFolder, $"Replays");

    /// <summary>
    /// Legacy settings file path.
    /// </summary>
    internal static string SettingsFile_Legacy = Path.Combine(filePathFolderSettings, "Settings.dat");

    /// <summary>
    /// Current compressed settings file path.
    /// </summary>
    internal static string SettingsFile => Path.Combine(filePathFolderSettings, $"Preset-{BAUPlugin.SettingsPreset?.Value ?? 0}.dat");





    /// <summary>
    /// Gets a file path by name in the Among Us data directory.
    /// </summary>
    /// <param name="name">The name of the file (without extension).</param>
    /// <returns>The full file path with .json extension.</returns>
    internal static string GetFilePath(string name)
    {
        return Path.Combine(BAUPlugin.GetDataPathToAmongUs(), $"{name}.json");
    }

    /// <summary>
    /// Initializes the data manager, loading files and ensuring required directories exist.
    /// </summary>
    internal static void Initialize()
    {
        LoadLegacyData();
        BetterDataFile.Init();
        BetterGameSettingsFile.Init();


    }

    /// <summary>
    /// Converts and loads legacy data if it exists.
    /// </summary>
    private static void LoadLegacyData()
    {
        if (File.Exists(SettingsFile_Legacy))
        {
            BAUPlugin.SettingsPreset.Value = 1;
            File.Move(SettingsFile_Legacy, SettingsFile);
        }
    }

    /// <summary>
    /// Saves a setting with the specified ID.
    /// </summary>
    /// <param name="id">The setting identifier.</param>
    /// <param name="input">The setting value to save.</param>
    internal static void SaveSetting(int id, object? input)
    {
        BetterGameSettingsFile.Settings[id] = input;
        BetterGameSettingsFile.Save();
    }

    /// <summary>
    /// Checks if a setting can be loaded as the specified type.
    /// </summary>
    /// <typeparam name="T">The type to check against.</typeparam>
    /// <param name="id">The setting identifier.</param>
    /// <returns>True if the setting exists and can be cast to type T, false otherwise.</returns>
    internal static bool CanLoadSetting<T>(int id)
    {
        if (BetterGameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            if (value is T)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads a setting with the specified ID, returning a default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of the setting value.</typeparam>
    /// <param name="id">The setting identifier.</param>
    /// <param name="Default">The default value to return and save if the setting doesn't exist.</param>
    /// <returns>The setting value or the default value if not found.</returns>
    internal static T? LoadSetting<T>(int id, T? Default = default)
    {
        if (BetterGameSettingsFile.Settings.TryGetValue(id, out var value))
        {
            if (value is T castValue)
            {
                return castValue;
            }
        }

        SaveSetting(id, Default);
        return Default;
    }






}