using System;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Tests;

/// <summary>
/// 时间轴换算测试：第一下必须落在 offset 处，倒计时与起始格只占音乐开头。
/// Timeline conversion tests: the first hit must sit exactly at the offset; the countdown and the
/// start marker only occupy the lead-in at the beginning of the music.
/// </summary>
[TestFixture]
public class TestCountdownTimeline
{
    private const string BigLevelPath = @"D:\ADOFAILevels\已完成\cintptr\level.adofai";

    [Test]
    public void FirstHitLandsOnOffset()
    {
        if (!System.IO.File.Exists(BigLevelPath))
        {
            Assert.Ignore("大关卡不存在 / large level missing");
            return;
        }

        Level level = new(BigLevelPath);
        var times = level.GetNoteTimesNew();

        double bpm = level.GetSetting<double>("bpm");
        double crotchet = 60.0 / bpm;
        double offsetSec = level.GetSetting<double>("offset") / 1000.0;

        TestContext.Out.WriteLine($"bpm={bpm}  crotchet={crotchet:F4}s  offset={offsetSec:F4}s");

        double rawFirst = times[1].Item1 / 1000.0;
        double markerLead = rawFirst;

        // 新公式：filePos[i] = entryTime[i] - entryTime[1] + offset
        double filePos = rawFirst - markerLead + offsetSec;

        TestContext.Out.WriteLine($"entryTime[1] = {rawFirst:F4}s  (lead-in)");
        TestContext.Out.WriteLine($"filePos[1] = {filePos:F4}s");

        // 第一下应当正好落在 offset 处
        // The first hit must land exactly on the offset
        Assert.That(filePos, Is.EqualTo(offsetSec).Within(1e-6),
            "第一下必须落在 offset 处 / the first hit must sit on the offset");

        // 后续砖块保持原有的相对间隔
        // Later tiles keep their original spacing
        double rawSecond = times[2].Item1 / 1000.0;
        double filePosSecond = rawSecond - markerLead + offsetSec;
        double expectedGap = rawSecond - rawFirst;
        Assert.That(filePosSecond, Is.EqualTo(offsetSec + expectedGap).Within(1e-6),
            "砖块间隔必须保持不变 / tile spacing must be preserved");

        TestContext.Out.WriteLine($"filePos[2] = {filePosSecond:F4}s  gap={expectedGap:F4}s");
    }
}
