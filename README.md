# SharpFAI

ADOFAI Level Parse Library - A C# library for parsing and manipulating ADOFAI (A Dance of Fire and Ice) level files.

ADOFAI关卡解析库 - 用于解析和操作ADOFAI（冰与火之舞）关卡文件的C#库。

## Building / 构建

`Thrust` 是 git 子模块，克隆后需要先初始化：

```bash
git clone --recurse-submodules https://github.com/adofaiex/SharpFAI.git
# 或者已经克隆过的话
git submodule update --init --recursive
dotnet build SharpFAI.slnx
```

`Thrust` is a git submodule, so initialise it after cloning:

```bash
git clone --recurse-submodules https://github.com/adofaiex/SharpFAI.git
# or, in an existing clone
git submodule update --init --recursive
dotnet build SharpFAI.slnx
```

## Features / 功能特性

- Parse ADOFAI level files (.adofai) / 解析ADOFAI关卡文件(.adofai)
- Manipulate level settings and events / 操作关卡设置和事件
- Calculate note timings / 计算音符时间
- Add decorations and text / 添加装饰和文本
- Export modified levels / 导出修改后的关卡
- Create and manipulate floor objects / 创建和操作地板对象
- Support for all ADOFAI event types / 支持所有ADOFAI事件类型
- Handle path data conversion to angle data / 处理路径数据到角度数据的转换
- Batch event operations / 批量事件操作
- Visual effects removal / 视觉效果移除
- Level creation from scratch / 从零创建关卡

## Installation / 安装

```bash
dotnet add package SharpFAI
```

## Player and Editor Audio / 播放器与编辑器音频

在「设置 → 音频」启用「不加载音频」，下次加载关卡时跳过音乐解码和打击音合成。关卡仍可无声预览，视频导出也不包含音轨。播放器命令行（含截图和导出）可使用 `--no-audio`。

Enable **Settings → Audio → Skip audio on next level load** to skip music decoding and hitsound synthesis. Silent playback and video export remain available. Use `--no-audio` for Player command-line runs, including screenshots and exports.

## Usage / 使用方法

```csharp
using SharpFAI.Serialization;
using SharpFAI.Util;

var level = new Level(pathToLevel:"path/to/level.adofai");

// Get level settings / 获取关卡设置
var bpm = level.GetSetting<double>("bpm");
var artist = level.GetSetting<string>("artist");

// Add events / 添加事件
level.AddEvent(10, EventType.Twirl);

// Calculate note times / 计算音符时间
var noteTimes = level.GetNoteTimes();

// Save modified level / 保存修改后的关卡
level.Save("modified-level.adofai");

// Create a new level / 创建新关卡
var level2 = Level.CreateNewLevel();
level2.Save("new-level.adofai");

// Remove visual effects / 移除视觉效果
level.RemoveVFXs(includeDecorations: true);

// Get floor index by time / 通过时间获取地板索引
int floorIndex = level.GetFloorIndexByNoteTime(5.0); // 5 seconds
```

## API Documentation / API文档

The main entry point for the library is the `Level` class which provides methods for loading, manipulating and saving ADOFAI levels.

库的主要入口点是 [Level](Serialization/Level.cs) 类，它提供了加载、操作和保存 ADOFAI 关卡的方法。

### Key Methods / 主要方法

#### Level Creation / 关卡创建
- `Level(string pathToLevel)` - Load level from file / 从文件加载关卡
- `Level(Dictionary<string, object> levelInfo)` - Initialize from level info dictionary / 从关卡信息字典初始化
- `CreateNewLevel(string savePath)` - Create a new level with default settings / 创建带有默认设置的新关卡

#### Settings Management / 设置管理
- `GetSetting<T>(string setting)` - Get setting value / 获取设置值
- `PutSetting<T>(string setting, T value)` - Set setting value / 设置设置值
- `HasSetting(string setting)` - Check if setting exists / 检查设置是否存在
- `RemoveSettings(params string[] settingsToRemove)` - Remove multiple settings / 移除多个设置

#### Event Management / 事件管理
- `AddEvent(int floor, EventType type, JObject data)` - Add event to floor / 向砖块添加事件
- `AddEvent(BaseEvent eventInfo)` - Add event from event object / 从事件对象添加事件
- `GetEvents(int floor, EventType type)` - Get events of specific type on floor / 获取特定地板上特定类型的事件
- `GetFloorEvents(int floor)` - Get all events on a floor / 获取特定地板上的所有事件
- `GetEvents(EventType type)` - Get all events of specific type / 获取特定类型的所有事件
- `GetEventsIf(Func<BaseEvent, bool> condition)` - Get events matching condition / 获取满足条件的事件
- `RemoveEventsIf(Func<BaseEvent, bool> condition)` - Remove events matching condition / 移除满足条件的事件

#### File Operations / 文件操作
- `Save(string newLevelPath, bool indent)` - Save level to file / 保存关卡到文件
- `ToString(bool indent)` - Convert level to JSON string / 将关卡转换为JSON字符串
- `GetAudioPath()` - Get absolute path to audio file / 获取音频文件的绝对路径

#### Utility Methods / 实用方法
- `DeserializeEvents(bool includeDecorations)` - Deserialize events to objects / 将事件反序列化为对象
- `InitAngleData()` - Initialize angle data from path data / 从路径数据初始化角度数据

Some public methods include bilingual XML documents (English/Chinese) to support Intellisense.

部分公共方法都包含双语XML文档（英文/中文）以支持IntelliSense。

### Core Classes / 核心类

#### Level Class / Level类

**Constructors / 构造函数**
- `Level(string pathToLevel)` - Initialize level from file path / 从文件路径初始化关卡
- `Level(Dictionary<string, object> levelInfo)` - Initialize from level info dictionary / 从关卡信息字典初始化
- `CreateNewLevel(string savePath)` - Create a new level with default settings / 创建带有默认设置的新关卡

**Settings Management / 设置管理**
- `GetSetting<T>(string setting)` - Get setting value / 获取设置值
- `PutSetting<T>(string setting, T value)` - Set setting value / 设置设置值
- `HasSetting(string setting)` - Check if setting exists / 检查设置是否存在
- `RemoveSettings(params string[] settingsToRemove)` - Remove multiple settings / 移除多个设置
- `SetSong(string songPath)` - Set level song / 设置关卡歌曲
- `GetAudioPath()` - Get absolute path to audio file / 获取音频文件的绝对路径

**Event Management / 事件管理**
- `AddEvent(int floor, EventType type, JObject data)` - Add event to floor / 向砖块添加事件
- `AddEvent(BaseEvent eventInfo)` - Add event from event object / 从事件对象添加事件
- `GetEvents(int floor, EventType type)` - Get events of specific type on floor / 获取特定地板上特定类型的事件
- `GetFloorEvents(int floor)` - Get all events on a floor / 获取特定地板上的所有事件
- `GetEvents(EventType type)` - Get all events of specific type / 获取特定类型的所有事件
- `GetEventsIf(Func<BaseEvent, bool> condition)` - Get events matching condition / 获取满足条件的事件
- `HasEvents(int floor)` - Check if floor has events / 检查地板是否有事件
- `HasEvents(int floor, EventType type)` - Check if floor has specific event type / 检查地板是否有特定类型的事件
- `RemoveEventsIf(Func<BaseEvent, bool> condition)` - Remove events matching condition / 移除满足条件的事件
- `RemoveFloorEvents(int floor, EventType type, int count)` - Remove events from floor / 从地板移除事件
- `DeserializeEvents(bool includeDecorations)` - Deserialize events to objects / 将事件反序列化为对象

**Decoration Management / 装饰管理**
- `AddTextToDecorations(int floor, string text, string tag, bool relativeToScreen, JObject data)` - Add text decoration / 添加文本装饰
- `AddDecoration(int floor, EventType type, string tag, bool relativeToScreen, JObject data)` - Add decoration / 添加装饰

**Serialization / 序列化**
- `Save(string newLevelPath, bool indent)` - Save level to file / 保存关卡到文件
- `ToString(bool indent)` - Convert level to JSON string / 将关卡转换为JSON字符串

#### LevelSetting Class / LevelSetting类

Provides preset level settings through `PresetSettings` static properties.

通过 `PresetSettings` 静态属性提供预设的关卡设置。

### Event Types / 事件类型

The library supports ~~all~~ ADOFAI event types including:

库支持部分ADOFAI事件类型，包括：

- `SetSpeed` - Set BPM or speed multiplier / 设置BPM或速度倍率
- `Twirl` - Rotation event / 旋转事件
- `Hold` - Long press event / 长按事件
- `MoveCamera` - Camera movement / 摄像机移动
- `Pause` - Pause event / 暂停事件
- `FreeRoam` - Free roam mode / 自由漫游模式
- `PositionTrack` - Track positioning / 轨道定位
- `MultiPlanet` - Multiple planets / 多行星
- `Bookmark` - Bookmark marker / 书签标记

### Utility Classes / 工具类

#### LevelUtils Class / LevelUtils类

Extension methods for level manipulation:

关卡操作的扩展方法：

- `GetNoteTimes(this Level level, bool addOffset)` - Calculate note timings / 计算音符时间
- `GetAllSpeedChange(this Level level)` - Get speed changes / 获取速度变化
- `GenerateGlide(this Level level, int startFloor, Pitch startNote, Pitch endNote, double duration)` - Generate glides / 生成滑音
- `RemoveVFXs(this Level level, bool includeDecorations, bool includeTracks, Action<string> onDelete)` - Remove visual effects / 移除视觉效果
- `AddCube(this Level level, string cubeImage, Tuple<float, float> position, Tuple<float, float> size, int floorCount, int floor, string tag, bool relativeToScreen)` - Add cube decoration with depth effect / 添加具有深度效果的立方体装饰
- `CreateFloors(this Level level, Vector2 startPosition, bool usePositionTrack)` - Create Floor objects from level data / 从关卡数据创建地板对象
- `GetFloorIndexByNoteTime(this Level level, double noteTimeSecond)` - Get floor index by note time / 通过音符时间获取地板索引

#### Floor Class / Floor类

Represents a floor tile with polygon mesh data:

表示带有多边形网格数据的地板瓦片：

- `Floor(double entryAngle, double exitAngle, Vector2 position)` - Constructor / 构造函数
- `GeneratePolygon()` - Generate or return cached polygon mesh / 生成或返回缓存的多边形网格

## Advanced Examples / 高级示例

### Analyzing a Level / 分析关卡

```csharp
var level = new Level("level.adofai");

// Get all speed changes
var speeds = level.GetAllSpeedChange();
Console.WriteLine($"Max BPM: {speeds.Max()}");

// Count events by type
var events = level.DeserializeEvents();
var eventStats = events.GroupBy(e => e.EventType)
    .Select(g => new { Type = g.Key, Count = g.Count() });

foreach (var stat in eventStats)
{
    Console.WriteLine($"{stat.Type}: {stat.Count}");
}
```

### Creating Floor Objects / 创建地板对象

```csharp
var level = new Level("level.adofai");
var floors = level.CreateFloors(usePositionTrack: true);

foreach (var floor in floors)
{
    Console.WriteLine($"Floor {floor.index}: Angle {floor.angle}°, BPM {floor.bpm}");
    var polygon = floor.GeneratePolygon();
    // Use polygon data for rendering
}
```

### Batch Event Operations / 批量事件操作

```csharp
// Remove all camera movements
level.RemoveEventsIf(e => e.EventType == EventType.MoveCamera);

// Find all pause events longer than 2 seconds
var longPauses = level.GetEventsIf(e => 
    e.EventType == EventType.Pause && 
    e.ToEvent<Pause>().Duration > 2.0);
```

## Requirements / 系统要求

- .NET Framework 4.8.1 or .NET 6.0+ / .NET Framework 4.8.1 或 .NET 6.0+
- Newtonsoft.Json 13.0.4+ / Newtonsoft.Json 13.0.4+

## Project Structure / 项目结构

- `Events/` - Contains all event type classes (Twirl, Hold, SetSpeed, etc.) / 包含所有事件类型类
- `Framework/` - Core interfaces for game components / 游戏组件的核心接口
- `Serialization/` - Level serialization/deserialization logic / 关卡序列化/反序列化逻辑
- `Util/` - Utility classes and math functions / 工具类和数学函数
- `Test/` - Unit tests / 单元测试

## License / 许可证

GPL-v3 License

## Author / 作者

StArray

## Contributing / 贡献

Contributions are welcome! Please feel free to submit a Pull Request.

欢迎贡献！请随时提交Pull Request。

## Support / 技术支持

If you encounter any issues or have questions about the library, please open an issue on the GitHub repository or contact the author.

如果您在使用过程中遇到任何问题或有关于库的疑问，请在GitHub仓库上提交问题或联系作者。

## Acknowledgements / 致谢

Special thanks to the ADOFAI community for inspiration and testing.

特别感谢ADOFAI社区提供的灵感和测试支持。

---

*Documentation enhanced by Kiro AI Assistant*