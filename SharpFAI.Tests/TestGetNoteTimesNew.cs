using SharpFAI.Events;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Tests;

/// <summary>
/// 验证 GetNoteTimesNew / FloorTimingCalculator 与游戏源码 entryTime 计算逻辑同步
/// Verifies GetNoteTimesNew is synced with the game's entryTime calculation
/// </summary>
public class TestGetNoteTimesNew
{
    /// <summary>
    /// 默认关卡（10 个 angle 0、bpm 100）带官方默认倒计时 countdownTicks = 4，
    /// 所以第 1 块在 (4-1) 拍倒计时 + 180° 行程之后，即 1800 + 600 = 2400 ms。
    /// 之后每块行程 180° = 600 ms。
    /// </summary>
    [Test]
    public void TestDefaultLevel()
    {
        var level = Level.CreateNewLevel();
        var times = level.GetNoteTimesNew();

        Assert.That(times.Count, Is.EqualTo(11), "应有 angleData.Count + 1 = 11 个方块时间");

        // scrLevelMaker.CalculateFloorEntryTimes: countdownTicks - 1 beats of lead-in, then travel
        double[] expected = { 0, 2400, 3000, 3600, 4200, 4800, 5400, 6000, 6600, 7200, 7800 };
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(times[i].Item1, Is.EqualTo(expected[i]).Within(0.001),
                $"方块 {i} 的 entryTime 应为 {expected[i]} ms");
            Assert.That(times[i].Item2, Is.False, $"默认关卡方块 {i} 不应有 hold 标记");
        }
    }

    /// <summary>
    /// 在方块 2 添加 SetSpeed Multiplier 2，方块 2 及之后速度为 2，行程时间减半。
    /// 前两块各 600 ms，倒计时 3 拍 = 1800 ms，之后每块 300 ms。
    /// </summary>
    [Test]
    public void TestSetSpeedMultiplier()
    {
        var level = Level.CreateNewLevel();
        level.AddEvent(new SetSpeed
        {
            Floor = 2,
            SpeedType = EventEnums.SpeedType.Multiplier,
            BpmMultiplier = 2
        });

        var times = level.GetNoteTimesNew();

        double[] expected = { 0, 2400, 3000, 3300, 3600, 3900, 4200, 4500, 4800, 5100, 5400 };
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.That(times[i].Item1, Is.EqualTo(expected[i]).Within(0.001),
                $"SetSpeed(2x@2) 后方块 {i} 的 entryTime 应为 {expected[i]} ms");
        }
    }

    /// <summary>
    /// addOffset=true 应在原时间上加上关卡 offset 设置（毫秒）
    /// </summary>
    [Test]
    public void TestAddOffset()
    {
        var level = Level.CreateNewLevel();
        level.PutSetting("offset", 500);

        var baseTimes = level.GetNoteTimesNew(false);
        var offsetTimes = level.GetNoteTimesNew(true);

        Assert.That(offsetTimes.Count, Is.EqualTo(baseTimes.Count), "带偏移结果数量应与不带偏移一致");
        for (int i = 0; i < baseTimes.Count; i++)
        {
            Assert.That(offsetTimes[i].Item1 - baseTimes[i].Item1, Is.EqualTo(500).Within(0.001),
                $"方块 {i} 带偏移减去不带偏移应为 500 ms");
            Assert.That(offsetTimes[i].Item2, Is.EqualTo(baseTimes[i].Item2), "hold 标记不受 offset 影响");
        }
    }

    /// <summary>
    /// Hold 事件应设置对应方块的 hold 标记，并增加行程时间（duration * 2 拍）
    /// </summary>
    [Test]
    public void TestHoldFlagAndTiming()
    {
        var level = Level.CreateNewLevel();
        level.AddEvent(new Hold { Floor = 1, Duration = 1 });

        var times = level.GetNoteTimesNew();

        Assert.That(times[1].Item2, Is.True, "方块 1 应有 hold 标记");
        Assert.That(times[0].Item2, Is.False, "方块 0 不应有 hold 标记");

        // 方块 1 的 hold=1 → 行程增加 2 拍 = 1.2s
        // entryTime[1] = 2400（倒计时 3 拍 + 首块行程）
        // entryTime[2] = 2400 + 600(floor1 行程) + 1200(hold) = 4200
        Assert.That(times[2].Item1, Is.EqualTo(4200).Within(0.001),
            "Hold duration=1 应使到达下一方块的时间增加 2 拍 (1200 ms)");
    }

    /// <summary>
    /// FloorTimingCalculator.Calculate 应返回与 GetNoteTimesNew 一致的进块时间（秒）
    /// </summary>
    [Test]
    public void TestFloorTimingCalculatorEntryTimes()
    {
        var level = Level.CreateNewLevel();
        var floors = FloorTimingCalculator.Calculate(level);

        Assert.That(floors.Count, Is.EqualTo(11), "应有 11 个 FloorData");
        Assert.That(floors[0].EntryTime, Is.EqualTo(0).Within(1e-9), "方块 0 的 entryTime 应为 0");
        // 游戏源码 scrLevelMaker.CalculateFloorEntryTimes：第 1 块 = (countdownTicks-1) 拍 + 首块行程
        // float 精度常量（如 4.712389f）使结果与理想值有 ~2e-8 秒偏差，此处保持一致
        Assert.That(floors[1].EntryTime, Is.EqualTo(2.4).Within(1e-6), "方块 1 的 entryTime 应为 2.4 秒");
        Assert.That(floors[0].EntryAngle, Is.EqualTo(4.71238899230957).Within(1e-9), "方块 0 的 entryAngle 应为 3π/2");
    }
}
