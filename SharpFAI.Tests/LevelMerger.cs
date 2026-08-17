using SharpFAI.Serialization;

namespace SharpFAI.Tests;

public class LevelMerger
{
    List<Level> levels;
    Level level;
    public LevelMerger(Level first)
    {
        this.level = first;
        this.levels = new List<Level>();
    }
    public void AddLevel(Level level,int skipStart=0,int skipEnd=0)
    {
        if (skipStart > 0)
        {
            level.RemoveAtStart(skipStart);
        }

        if (skipEnd > 0)
        {
            level.RemoveAtEnd(skipEnd);
        }
        levels.Add(level);
    }
    public void Merge()
    { 
        int offset = level.angleData.Count;
        for (int i = 0; i < levels.Count; i++)
        {
            var curlevel = levels[i];
            var events = curlevel.DeserializeEvents();
            events.Select(e =>
            {
                e.Floor += offset;
                return e;
            }).ToList().ForEach(level.AddEvent);
            level.angleData.Add(curlevel.angleData.Select(a=>a.ToObject<double>()));
            offset += curlevel.angleData.Count;
        }
    }
}