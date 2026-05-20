using System.Collections.Generic;
using System.IO;

namespace EditorSpeedSplits.Splits;

internal static class SplitRecorder
{
    public static readonly List<EditorSplit> Splits = [];

    public static LevelSplits PreviousLevelSplits;

    public static void Clear()
    {
        Splits.Clear();
    }

    public static void Add(EditorSplit split)
    {
        Splits.Add(split);
    }

    public static void SaveBestSplits(string levelName, float bestTime, List<EditorSplit> bestSplits, bool completed)
    {
        LevelSplits levelSplits = new()
        {
            levelName = levelName,
            totalTime = bestTime,
            completed = completed,
            splits = [.. bestSplits]
        };

        var identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');

        Plugin.Instance.PersonalBestSplitsStorage.SaveToJson(identifier, levelSplits);

        Plugin.logger.LogInfo($"Saved best splits for level {levelName} to storage.");
    }

    public static LevelSplits LoadBestSplits(string levelName)
    {
        var identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');

        if (!Plugin.Instance.PersonalBestSplitsStorage.JsonFileExists(identifier))
            return null;

        Plugin.logger.LogInfo($"Loading best splits for level {levelName} from storage.");

        return Plugin.Instance.PersonalBestSplitsStorage.LoadFromJson<LevelSplits>(identifier);
    }

    public static void DeleteBestSplits(string levelName)
    {
        var identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');
        if (!Plugin.Instance.PersonalBestSplitsStorage.JsonFileExists(identifier)) return;
        Plugin.Instance.PersonalBestSplitsStorage.DeleteJsonFile(identifier);
        Plugin.logger.LogInfo($"Deleted best splits for level {levelName} from storage.");
    }

    public static bool HasSplits(string levelName)
    {
        var identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
            .Replace(Path.AltDirectorySeparatorChar, '_');

        if (!Plugin.Instance.PersonalBestSplitsStorage.JsonFileExists(identifier))
            return false;

        Plugin.logger.LogInfo($"Best splits for level {levelName} exist in storage.");

        return true;
    }
}