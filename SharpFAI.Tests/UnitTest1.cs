namespace SharpFAI.Tests;

using Newtonsoft.Json.Linq;
using SharpFAI.Serialization;
using SharpFAI.Events;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }

    /// <summary>
    /// 测试 JSON 值复制 vs 引用修改
    /// </summary>
    [Test]
    public void TestJsonValueCopyVsReference()
    {
        // 演示值复制问题
        JObject root = new JObject();
        JToken val = 2;
        root["val"] = val;
        val = 4;  // 修改 val 变量
        Assert.That(root["val"].Value<int>(), Is.EqualTo(2), "值复制：修改变量不影响 root");

        // 正确的引用修改方式
        root["val"] = JToken.FromObject(4);
        Assert.That(root["val"].Value<int>(), Is.EqualTo(4), "直接修改 root 中的值");
    }

    /// <summary>
    /// 测试 Level 的 angleData 引用同步
    /// </summary>
    [Test]
    public void TestLevelAngleDataReference()
    {
        Level level = Level.CreateNewLevel();

        // 修改 angleData
        level.angleData.Add(JToken.FromObject(45.0));
        level.angleData.Add(JToken.FromObject(90.0));

        // 验证 root 中的数据也被更新
        var rootAngleData = level.root["angleData"];
        Assert.That(rootAngleData.Count(), Is.EqualTo(12), "angleData 应该有 12 个元素（初始 10 + 新增 2）");
        Assert.That(rootAngleData[10].Value<double>(), Is.EqualTo(45.0), "第 11 个元素应该是 45.0");
        Assert.That(rootAngleData[11].Value<double>(), Is.EqualTo(90.0), "第 12 个元素应该是 90.0");
    }

    /// <summary>
    /// 测试 Level 的 actions 引用同步
    /// </summary>
    [Test]
    public void TestLevelActionsReference()
    {
        Level level = Level.CreateNewLevel();

        // 验证 actions 不为空
        Assert.That(level.actions, Is.Not.Null, "actions 应该被初始化");

        // 添加事件
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 240 });

        // 验证 root 中的数据也被更新
        var rootActions = level.root["actions"];
        Assert.That(rootActions, Is.Not.Null, "root 中的 actions 应该不为空");
        Assert.That(rootActions.Count(), Is.EqualTo(2), "actions 应该有 2 个事件");
        Assert.That(rootActions[0]?["floor"]?.Value<int>(), Is.EqualTo(0), "第一个事件的 floor 应该是 0");
        Assert.That(rootActions[1]?["floor"]?.Value<int>(), Is.EqualTo(5), "第二个事件的 floor 应该是 5");
    }

    /// <summary>
    /// 测试 Level 的 decorations 引用同步
    /// </summary>
    [Test]
    public void TestLevelDecorationsReference()
    {
        Level level = Level.CreateNewLevel();

        // 验证 decorations 不为空
        Assert.That(level.decorations, Is.Not.Null, "decorations 应该被初始化");

        // 添加装饰
        level.AddDecoration(0, EventType.AddDecoration, "test-tag");
        level.AddDecoration(10, EventType.AddText, "text-tag");

        // 验证 root 中的数据也被更新
        var rootDecorations = level.root["decorations"];
        Assert.That(rootDecorations, Is.Not.Null, "root 中的 decorations 应该不为空");
        Assert.That(rootDecorations.Count(), Is.EqualTo(2), "decorations 应该有 2 个装饰");
        Assert.That(rootDecorations[0]?["tag"]?.Value<string>(), Is.EqualTo("test-tag"), "第一个装饰的 tag 应该是 test-tag");
        Assert.That(rootDecorations[1]?["tag"]?.Value<string>(), Is.EqualTo("text-tag"), "第二个装饰的 tag 应该是 text-tag");
    }

    /// <summary>
    /// 测试 Level 的 ToString 方法包含所有修改
    /// </summary>
    [Test]
    public void TestLevelToStringIncludesAllChanges()
    {
        Level level = Level.CreateNewLevel();

        // 进行多个修改
        level.angleData.Add(JToken.FromObject(45.0));
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddDecoration(0, EventType.AddDecoration, "test");

        // 转换为 JSON 字符串
        string json = level.ToString(false);
        JObject parsed = JObject.Parse(json);

        // 验证所有修改都在 JSON 中
        Assert.That(parsed["angleData"]?.Count(), Is.EqualTo(11), "angleData 应该有 11 个元素（初始 10 + 新增 1）");
        Assert.That(parsed["actions"]?.Count(), Is.EqualTo(1), "actions 应该有 1 个事件");
        Assert.That(parsed["decorations"]?.Count(), Is.EqualTo(1), "decorations 应该有 1 个装饰");
    }

    /// <summary>
    /// 测试 Level 的 root 属性直接返回引用
    /// </summary>
    [Test]
    public void TestLevelRootDirectReference()
    {
        Level level = Level.CreateNewLevel();

        // 获取 root 两次
        JObject root1 = level.root;
        JObject root2 = level.root;

        // 应该是同一个对象
        Assert.That(ReferenceEquals(root1, root2), Is.True, "root 属性应该返回同一个对象引用");

        // 修改 angleData 后，root 中的数据应该立即更新
        level.angleData.Add(JToken.FromObject(45.0));
        Assert.That(root1["angleData"]?.Count(), Is.EqualTo(11), "root 中的 angleData 应该立即更新（初始 10 + 新增 1）");
    }

    /// <summary>
    /// 测试 AddEvent 重载方法（使用 BaseEvent）
    /// </summary>
    [Test]
    public void TestAddEventWithBaseEvent()
    {
        Level level = Level.CreateNewLevel();

        // 使用 BaseEvent 添加事件
        var setSpeed = new SetSpeed
        {
            Floor = 0,
            BeatsPerMinute = 150,
            SpeedType = EventEnums.SpeedType.Bpm
        };
        level.AddEvent(setSpeed);

        // 验证事件被添加
        Assert.That(level.actions?.Count(), Is.EqualTo(1), "应该有 1 个事件");
        Assert.That(level.root["actions"]?.Count(), Is.EqualTo(1), "root 中的 actions 应该同步更新");
    }

    /// <summary>
    /// 测试 GetEvents 方法（按类型查询）
    /// </summary>
    [Test]
    public void TestGetEventsByType()
    {
        Level level = Level.CreateNewLevel();

        // 添加多个不同类型的事件
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 240 });
        level.AddEvent(10, EventType.ScaleRadius, new JObject { ["scale"] = 150 });

        // 查询 SetSpeed 事件
        var speedEvents = level.GetEvents(EventType.SetSpeed);
        Assert.That(speedEvents?.Count(), Is.EqualTo(2), "应该有 2 个 SetSpeed 事件");

        // 查询 ScaleRadius 事件
        var scaleEvents = level.GetEvents(EventType.ScaleRadius);
        Assert.That(scaleEvents?.Count(), Is.EqualTo(1), "应该有 1 个 ScaleRadius 事件");
    }

    /// <summary>
    /// 测试 GetFloorEvents 方法
    /// </summary>
    [Test]
    public void TestGetFloorEvents()
    {
        Level level = Level.CreateNewLevel();

        // 在同一楼层添加多个事件
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddEvent(5, EventType.ScaleRadius, new JObject { ["scale"] = 150 });
        level.AddEvent(10, EventType.SetSpeed, new JObject { ["bpm"] = 240 });

        // 获取楼层 5 的事件
        var floorEvents = level.GetFloorEvents(5);
        Assert.That(floorEvents?.Count(), Is.EqualTo(2), "楼层 5 应该有 2 个事件");

        // 获取楼层 10 的事件
        var floor10Events = level.GetFloorEvents(10);
        Assert.That(floor10Events?.Count(), Is.EqualTo(1), "楼层 10 应该有 1 个事件");
    }

    /// <summary>
    /// 测试 HasEvents 方法
    /// </summary>
    [Test]
    public void TestHasEvents()
    {
        Level level = Level.CreateNewLevel();

        // 初始状态，楼层 0 没有事件
        Assert.That(level.HasEvents(0), Is.False, "楼层 0 初始应该没有事件");

        // 添加事件后
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        Assert.That(level.HasEvents(0), Is.True, "楼层 0 应该有事件");

        // 检查特定类型的事件（使用 GetFloorEvents 和 GetEvents 的组合）
        var floorEvents = level.GetFloorEvents(0);
        var hasSetSpeed = floorEvents.Any(e => e["eventType"]?.Value<string>() == EventType.SetSpeed.ToString());
        Assert.That(hasSetSpeed, Is.True, "楼层 0 应该有 SetSpeed 事件");

        var scaleEvents = floorEvents.Where(e => e["eventType"]?.Value<string>() == EventType.ScaleRadius.ToString());
        Assert.That(scaleEvents.Any(), Is.False, "楼层 0 应该没有 ScaleRadius 事件");
    }

    /// <summary>
    /// 测试 RemoveFloorEvents 方法
    /// </summary>
    [Test]
    public void TestRemoveFloorEvents()
    {
        Level level = Level.CreateNewLevel();

        // 添加多个事件
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 240 });
        level.AddEvent(5, EventType.ScaleRadius, new JObject { ["scale"] = 150 });

        // 移除楼层 5 的 1 个 SetSpeed 事件
        level.RemoveFloorEvents(5, EventType.SetSpeed, 1);

        // 验证移除结果
        var floorEvents = level.GetFloorEvents(5);
        Assert.That(floorEvents?.Count(), Is.EqualTo(2), "应该剩余 2 个事件");

        var speedEvents = level.GetEvents(EventType.SetSpeed);
        Assert.That(speedEvents?.Count(), Is.EqualTo(1), "应该剩余 1 个 SetSpeed 事件");
    }

    /// <summary>
    /// 测试 RemoveEventsIf 方法
    /// </summary>
    [Test]
    public void TestRemoveEventsIf()
    {
        Level level = Level.CreateNewLevel();

        // 添加多个事件
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });
        level.AddEvent(5, EventType.SetSpeed, new JObject { ["bpm"] = 240 });
        level.AddEvent(10, EventType.ScaleRadius, new JObject { ["scale"] = 150 });

        // 移除所有 floor >= 5 的事件
        level.RemoveEventsIf(e => e.Floor >= 5);

        // 验证移除结果
        Assert.That(level.actions?.Count(), Is.EqualTo(1), "应该只剩 1 个事件");
        Assert.That(level.actions?[0]?["floor"]?.Value<int>(), Is.EqualTo(0), "剩余事件的 floor 应该是 0");
    }

    /// <summary>
    /// 测试 GetSetting 和 PutSetting 方法
    /// </summary>
    [Test]
    public void TestGetAndPutSetting()
    {
        Level level = Level.CreateNewLevel();

        // 获取初始设置
        int initialBpm = level.GetSetting<int>("bpm");
        Assert.That(initialBpm, Is.EqualTo(100), "初始 BPM 应该是 100");

        // 修改设置
        level.PutSetting("bpm", 200);
        int newBpm = level.GetSetting<int>("bpm");
        Assert.That(newBpm, Is.EqualTo(200), "修改后 BPM 应该是 200");

        // 验证 root 中的设置也被更新
        int rootBpm = level.root["settings"]?["bpm"]?.Value<int>() ?? 0;
        Assert.That(rootBpm, Is.EqualTo(200), "root 中的 BPM 应该同步更新");
    }

    /// <summary>
    /// 测试 HasSetting 方法
    /// </summary>
    [Test]
    public void TestHasSetting()
    {
        Level level = Level.CreateNewLevel();

        // 检查存在的设置
        Assert.That(level.HasSetting("bpm"), Is.True, "应该有 bpm 设置");
        Assert.That(level.HasSetting("version"), Is.True, "应该有 version 设置");

        // 检查不存在的设置
        Assert.That(level.HasSetting("nonexistent"), Is.False, "不应该有 nonexistent 设置");
    }

    /// <summary>
    /// 测试 RemoveSettings 方法
    /// </summary>
    [Test]
    public void TestRemoveSettings()
    {
        Level level = Level.CreateNewLevel();

        // 验证初始状态
        Assert.That(level.HasSetting("offset"), Is.True, "初始应该有 offset 设置");
        Assert.That(level.HasSetting("pitch"), Is.True, "初始应该有 pitch 设置");

        // 移除设置
        level.RemoveSettings("offset", "pitch");

        // 验证移除结果
        Assert.That(level.HasSetting("offset"), Is.False, "应该移除 offset 设置");
        Assert.That(level.HasSetting("pitch"), Is.False, "应该移除 pitch 设置");
        Assert.That(level.HasSetting("bpm"), Is.True, "应该保留 bpm 设置");
    }

    /// <summary>
    /// 测试 AddTextToDecorations 方法
    /// </summary>
    [Test]
    public void TestAddTextToDecorations()
    {
        Level level = Level.CreateNewLevel();

        // 添加文本装饰
        level.AddTextToDecorations(0, "Hello World", "greeting");

        // 验证装饰被添加
        Assert.That(level.decorations?.Count(), Is.EqualTo(1), "应该有 1 个装饰");
        Assert.That(level.decorations?[0]?["decText"]?.Value<string>(), Is.EqualTo("Hello World"), "文本应该是 'Hello World'");
        Assert.That(level.decorations?[0]?["tag"]?.Value<string>(), Is.EqualTo("greeting"), "标签应该是 'greeting'");
    }

    /// <summary>
    /// 测试 Level 的隐式转换操作符
    /// </summary>
    [Test]
    public void TestLevelImplicitConversion()
    {
        Level level = Level.CreateNewLevel();
        level.AddEvent(0, EventType.SetSpeed, new JObject { ["bpm"] = 120 });

        // 隐式转换为 JObject
        JObject converted = level;
        Assert.That(converted, Is.Not.Null, "转换结果不应该为空");
        Assert.That(converted["actions"]?.Count(), Is.EqualTo(1), "转换后的 JObject 应该包含事件");
    }
}