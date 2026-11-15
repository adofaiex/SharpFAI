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
        Level level = @"C:\Users\StArray\Downloads\hangazi Frums_-_HALL\level.adofai";
        level.RemoveVFXs();
        level.Save();
        Assert.Pass();
    }
}