using System.Collections.Generic;
using System.IO;

namespace EditorSpeedSplits.Splits
{
    internal static class SplitRecorder
    {
        public static readonly List<EditorSplit> Splits = new();

        public static void Clear()
        {
            Splits.Clear();
        }

        public static void Add(EditorSplit split)
        {
            Splits.Add(split);
        }

        public static void SaveBestSplits(string levelName, float bestTime)
        {
            LevelSplits levelSplits = new LevelSplits
            {
                levelName = levelName,
                totalTime = bestTime,
                splits = new List<EditorSplit>(Splits)
            };

            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            Plugin.Instance.personalBestSplitsStorage.SaveToJson(identifier, levelSplits);

            Plugin.logger.LogInfo($"Saved best splits for level {levelName} to storage.");
        }

        public static LevelSplits LoadBestSplits(string levelName)
        {
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            if (!Plugin.Instance.personalBestSplitsStorage.JsonFileExists(identifier))
                return null;

            Plugin.logger.LogInfo($"Loading best splits for level {levelName} from storage.");

            return Plugin.Instance.personalBestSplitsStorage.LoadFromJson<LevelSplits>(identifier);
        }

        public static void DeleteBestSplits(string levelName)
        {
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');
            if (Plugin.Instance.personalBestSplitsStorage.JsonFileExists(identifier))
            {
                Plugin.Instance.personalBestSplitsStorage.DeleteJsonFile(identifier);
                Plugin.logger.LogInfo($"Deleted best splits for level {levelName} from storage.");
            }
        }
    }
}
