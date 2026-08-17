using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpFAI.Events;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Tests;

public class LevelTests
{
    [Test]
    public void TestProcessLevel()
    {
        Level level = @"D:\ADOFAILevels\enough\level.adofai";
        double bpm = 279;
        int i = 1;
        level.angleData.Add(0);
        level.AddEvent(new SetSpeed()
        {
            Floor = i,
            SpeedType = EventEnums.SpeedType.Bpm,
            BeatsPerMinute = bpm
        });
        bpm += 3;
        while (bpm < 316.0)
        {
            level.angleData.Add(0);
            i++;
            if (i % 16 != 0) continue; 
            level.AddEvent(new SetSpeed()
            {
                Floor = i,
                SpeedType = EventEnums.SpeedType.Bpm,
                BeatsPerMinute = bpm
            });
            bpm += 2;
        }
        level.Save();
        Assert.Pass();
    }
}