using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace EditorSpeedSplits.Splits;

internal class LevelSplits
{
    public string levelName;
    public float totalTime;
    public bool completed = true;
    
    public List<EditorSplit> splits;
    public int GotCPs => splits != null ? completed ? splits.Count - 1 : splits.Count : 0;
}