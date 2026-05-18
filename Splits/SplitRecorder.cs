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

        public static void SaveBestSplits(string levelName, float bestTime, List<EditorSplit> bestSplits)
        {
            LevelSplits levelSplits = new LevelSplits
            {
                levelName = levelName,
                totalTime = bestTime,
                splits = new List<EditorSplit>(bestSplits)
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

        public static bool HasSplits(string levelName)
        {
            string identifier = levelName.Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            if (!Plugin.Instance.personalBestSplitsStorage.JsonFileExists(identifier))
                return false;

            Plugin.logger.LogInfo($"Best splits for level {levelName} exist in storage.");

            return true;
        }

        public static LevelSplits CreateSplitsFromReplay(ReplayManager.ReplayInfo replay, string fullLevelName)
        {
            LevelSplits levelSplits = new LevelSplits
            {
                levelName = fullLevelName,
                totalTime = replay.Time,
                splits = new List<EditorSplit>()
            };
            for (int i = 0; i < replay.Splits.Count; i++)
            {
                float splitTime = replay.Splits[i];
                float velocity = replay.velocities != null && i < replay.velocities.Count ? replay.velocities[i] : 0f;
                
                EditorSplit split = new EditorSplit
                {
                    index = i + 1,
                    isFinish = false,
                    time = splitTime,
                    velocity = velocity
                };

                levelSplits.splits.Add(split);
            }


            string identifier = fullLevelName.Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_');

            Plugin.Instance.personalBestSplitsStorage.SaveToJson(identifier, levelSplits);
            return levelSplits;
        }
    }
}
