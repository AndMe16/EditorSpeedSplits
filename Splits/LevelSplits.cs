using System.Collections.Generic;

namespace EditorSpeedSplits.Splits
{
    internal class LevelSplits
    {
        public string levelName;

        public float totalTime;

        public bool completed = true;

        public int gotCPs = 1000;

        public int totalCPs = 1000;

        public bool fromReplay = false;

        public List<EditorSplit> splits;
    }
}
