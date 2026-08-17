using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpFAI.Events;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Tests;

/// <summary>
/// 中旋（midspin）方块带事件时的时间计算验证
/// 期望值均为按游戏源码（scnLevelMaker.InstantiateFloatFloors / scnGame.ApplyCoreEventsToFloors /
/// scrLevelMaker.CalculateFloorEntryTimes）逐行手工推演，bpm=100（1拍=0.6s），不含倒计时
/// </summary>
public class TestMidspinEvents
{
    private static Level MakeLevel(params double[] angles)
    {
        JObject root = new();
        root["angleData"] = new JArray(angles);
        root["settings"] = JObject.FromObject(new
        {
            version = 14,
            author = "test",
            bpm = 100,
            offset = 0,
            pitch = 100
        });
        root["actions"] = new JArray();
        root["decorations"] = new JArray();
        return new(JsonConvert.DeserializeObject<Dictionary<string, object>>(root.ToString()));
    }

    private static void AssertTimes(List<(double, bool)> times, double[] expectedMs, string name)
    {
        Assert.That(times.Count, Is.EqualTo(expectedMs.Length), $"{name}: 数量应为 {expectedMs.Length}");
        for (int i = 0; i < expectedMs.Length; i++)
        {
            Assert.That(times[i].Item1, Is.EqualTo(expectedMs[i]).Within(0.5),
                $"{name}: 方块 {i} 应为 {expectedMs[i]} ms，实际 {times[i].Item1} ms");
        }
    }

    /// <summary>
    /// [0, 999, 180, 0, 0] 无事件基准
    /// 构建: f0: entry270/exit90, f1(midspin): entry270/exit270, f2: entry90/exit-90,
    ///       f3: entry90/exit90, f4: entry270/exit90, f5: entry270/exit450
    /// 行程: f0=180°(0.6s), f1=0, f2=180°(0.6s), f3=回头(1.2s), f4=180°(0.6s)
    /// </summary>
    [Test]
    public void TestMidspinBaseline()
    {
        var level = MakeLevel(0, 999, 180, 0, 0);
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 1200, 2400, 3000], "基准");
    }

    /// <summary>
    /// [0, 999, 180, 0, 0] + SetSpeed Multiplier 2 在中旋 f1 上
    /// 游戏: num43=360(中旋entry==exit), num38=200/100=2, f1.speed=2 并持续到后续方块
    /// 行程: f0=0.6s(speed1), f1=0, f2=180°/speed2=0.3s, f3=回头2拍/speed2=0.6s, f4=0.3s
    /// </summary>
    [Test]
    public void TestSetSpeedMultiplierOnMidspin()
    {
        var level = MakeLevel(0, 999, 180, 0, 0);
        level.AddEvent(new SetSpeed
        {
            Floor = 1,
            SpeedType = EventEnums.SpeedType.Multiplier,
            BpmMultiplier = 2
        });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 900, 1500, 1800], "中旋变速");
    }

    /// <summary>
    /// [0, 999, 180, 0, 0] + SetSpeed Bpm 200 在中旋 f1 上
    /// 游戏: num48=200, num38=2，与 Multiplier 2 等价
    /// </summary>
    [Test]
    public void TestSetSpeedBpmOnMidspin()
    {
        var level = MakeLevel(0, 999, 180, 0, 0);
        level.AddEvent(new SetSpeed
        {
            Floor = 1,
            SpeedType = EventEnums.SpeedType.Bpm,
            BeatsPerMinute = 200
        });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 900, 1500, 1800], "中旋Bpm变速");
    }

    /// <summary>
    /// [0, 999, 90, 0] + Twirl 在中旋 f1 上（非对称角度暴露方向差异）
    /// 构建: f2: entry90/exit0, f3: entry180/exit90
    /// 无 Twirl: f2=270°(0.9s), f3=270°(0.9s) → [0,600,600,1500,2400]
    /// 有 Twirl@1: f2/f3 方向反转 → 90°(0.3s) → [0,600,600,900,1200]
    /// </summary>
    [Test]
    public void TestTwirlOnMidspin()
    {
        var level = MakeLevel(0, 999, 90, 0);
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 1500, 2400], "无Twirl对照");

        level = MakeLevel(0, 999, 90, 0);
        level.AddEvent(new Twirl { Floor = 1 });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 900, 1200], "中旋Twirl");
    }

    /// <summary>
    /// [0, 999, 180, 0, 0] + Hold duration=1 在中旋 f1 上
    /// 游戏: Hold 不检查 midSpin，f1.holdLength=1，中旋行程0后追加 2拍=1.2s
    /// → times[2] = 600 + 1200 = 1800，后续整体后移
    /// </summary>
    [Test]
    public void TestHoldOnMidspin()
    {
        var level = MakeLevel(0, 999, 180, 0, 0);
        level.AddEvent(new Hold { Floor = 1, Duration = 1 });

        var times = level.GetNoteTimesNew();
        AssertTimes(times, [0, 600, 1800, 2400, 3600, 4200], "中旋Hold");
        Assert.That(times[1].Item2, Is.True, "中旋 f1 应有 hold 标记");
    }

    /// <summary>
    /// [0, 999, 999, 180, 0] 连续中旋
    /// f1、f2 均为 0 行程，f3 回头 2 拍，f4 回头 2 拍
    /// </summary>
    [Test]
    public void TestConsecutiveMidspins()
    {
        var level = MakeLevel(0, 999, 999, 180, 0);
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 600, 1800, 3000], "连续中旋");
    }

    /// <summary>
    /// [0, 999, 180] + MultiPlanet(3) 在中旋后的 f2 上
    /// 游戏: prevfloor(f1) 是 midSpin → f1.numPlanets=3（中旋自身 num3=0 无影响）
    /// f2 行星3: num3 = π/3 - (2π+π/3) = -2π → mod 后与 2 星一致 → 180°(0.6s)
    /// </summary>
    [Test]
    public void TestMultiPlanetAfterMidspin()
    {
        var level = MakeLevel(0, 999, 180);
        level.AddEvent(new MultiPlanet { Floor = 2, Planets = EventEnums.PlanetCount.ThreePlanets });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 1200], "中旋后多行星");
    }

    /// <summary>
    /// [0, 999, 90, 0] + Twirl 在中旋前一块 f0 上（对照：Twirl 位置不同结果不同）
    /// f0 上的 Twirl 使 f0 行程反向: mod(-180·-1,360)=180° 不变（对称）；
    /// 但 f2/f3 方向反转 → [0,600,600,900,1200]（与 Twirl@1 相同，因 f0 行程对称）
    /// </summary>
    [Test]
    public void TestTwirlBeforeMidspin()
    {
        var level = MakeLevel(0, 999, 90, 0);
        level.AddEvent(new Twirl { Floor = 0 });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 900, 1200], "中旋前Twirl");
    }

    /// <summary>
    /// [0, 0] + SetSpeed Mult2 angleOffset=90 在 f1 上（普通块的途中分段变速）
    /// 游戏: num43=180, num46 = 90°@1x(0.3s) + 90°@2x(0.15s) = 0.45s
    /// flag13=true → f1.speed = 60/0.45·(180/180)/100 = 4/3 → f1 行程 = 0.45s
    /// → [0, 600, 1050]
    /// </summary>
    [Test]
    public void TestAngleOffsetSetSpeedNormalFloor()
    {
        var level = MakeLevel(0, 0);
        level.AddEvent(new SetSpeed
        {
            Floor = 1,
            SpeedType = EventEnums.SpeedType.Multiplier,
            BpmMultiplier = 2,
            AngleOffset = 90
        });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 1050], "普通块angleOffset变速");
    }

    /// <summary>
    /// [0, 999, 180, 0] + SetSpeed Mult2 angleOffset=90 在中旋 f1 上
    /// 游戏: 中旋 entry==exit → num43=360, num52=true → flag13 → f1.speed=speed2（中旋行程0无影响）
    /// 后续速度 = num38 = 2: f2=180°@2x=0.3s, f3=回头2拍@2x=0.6s
    /// → [0, 600, 600, 900, 1500]
    /// </summary>
    [Test]
    public void TestAngleOffsetSetSpeedOnMidspin()
    {
        var level = MakeLevel(0, 999, 180, 0);
        level.AddEvent(new SetSpeed
        {
            Floor = 1,
            SpeedType = EventEnums.SpeedType.Multiplier,
            BpmMultiplier = 2,
            AngleOffset = 90
        });
        AssertTimes(level.GetNoteTimesNew(), [0, 600, 600, 900, 1500], "中旋angleOffset变速");
    }
}
