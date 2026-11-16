using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json.Linq;
using SharpFAI.Events;
using SharpFAI.Framework;
using SharpFAI.Serialization;

namespace SharpFAI.Util;

/// <summary>
/// Utility class for ADOFAI level calculations and operations
/// ADOFAI关卡实用工具类
/// </summary>
public static class LevelUtils
{
    private static List<(double,bool)> noteTimesCache = [];
    private static List<(double,bool)> noteTimesCacheWithOffset = [];
    private static List<(double,bool)> noteTimesCacheNew = [];
    private static List<(double,bool)> noteTimesCacheNewWithOffset = [];
    private static List<double> allSpeedChange = [];
    private static Level currentLevel = Level.CreateNewLevel();

    /// <summary>
    /// 进度回调事件（进度百分比 0-100，进度描述文本）
    /// Progress callback event (progress percentage 0-100, progress description text)
    /// </summary>
    public static event Action<int, string> progressCallback;

    private class ParsedChart
    {
        public double angle;
        public double bpm;
        public int direction;
        public double extraHold;
        public double extraBeats;
        public bool midr;
        public int multiPlanet;
    }

    /// <summary>
    /// Check if the level has been parsed before / 检查是否已经解析过
    /// </summary>
    /// <param name="level"></param>
    public static void CheckCache(Level level)
    {
        if (currentLevel != level)
        {
            noteTimesCache = [];
            noteTimesCacheWithOffset = [];
            noteTimesCacheNew = [];
            noteTimesCacheNewWithOffset = [];
            allSpeedChange = [];
            currentLevel = level;
        }
    }

    /// <param name="level">The level to calculate note times for / 要计算音符时间的关卡</param>
    extension(Level level)
    {
        /// <summary>
        /// Calculates note timing for each tile in the level
        /// 计算关卡中每个瓦片的音符时间
        /// </summary>
        /// <param name="addOffset">Whether to add the level settings offset to the times (default: false) / 是否将关卡设置中的偏移添加到时间中（默认：false）</param>
        /// <returns>List of note times in milliseconds / 以毫秒为单位的音符时间列表</returns>
        public List<(double,bool)> GetNoteTimes(bool addOffset = false)
        {
            CheckCache(level);
            if (noteTimesCache.Count > 0 && !addOffset) return noteTimesCache;
            if (noteTimesCacheWithOffset.Count > 0 && addOffset) return noteTimesCacheWithOffset;
            var angleDataList = level.angles;
            List<ParsedChart> parsedChart = [];

            // 初步处理轨道数据
            for (int i = 0; i < angleDataList.Count; i++)
            {
                double angleData = angleDataList[i];
                if (Math.Abs(angleData - 999) < 0.01)
                {
                    parsedChart.Add(new()
                    {
                        angle = Fmod(angleDataList[i - 1] + 180, 360),
                        bpm = 0,
                        direction = 0,
                        extraHold = 0,
                        extraBeats = 0,
                        midr = true,
                        multiPlanet = -1
                    });
                }
                else
                {
                    parsedChart.Add(new()
                    {
                        angle = Fmod(angleData, 360),
                        bpm = 0,
                        direction = 0,
                        extraHold = 0,
                        extraBeats = 0,
                        midr = false,
                        multiPlanet = -1
                    });
                }
            }

            // 添加最后一个标记点
            parsedChart.Add(new()
            {
                angle = 0,
                bpm = 0,
                direction = 0,
                extraHold = 0,
                extraBeats = 0,
                midr = false,
                multiPlanet = -1
            });
            
            double bpm = level.GetSetting<double>("bpm");
            double pitch = level.GetSetting<int>("pitch") / 100.0;
            var levelEvents = level.DeserializeEvents();

            // 处理事件数据
            foreach (var eventValue in levelEvents)
            {
                var o = eventValue;
                if (o != null)
                {
                    int tile = o.Floor;
                    var eventType = o.EventType;

                    var ob = parsedChart[tile];
                    //if (ob == null) continue;
                    switch (eventType)
                    {
                        case EventType.SetSpeed:
                            var eventObj = eventValue.ToEvent<SetSpeed>();
                            if (eventObj.SpeedType == EventEnums.SpeedType.Multiplier)
                            {
                                bpm = eventObj.BpmMultiplier * bpm;
                            }
                            else if (eventObj.SpeedType == EventEnums.SpeedType.Bpm)
                            {
                                bpm = eventObj.BeatsPerMinute * pitch;
                            }

                            ob.bpm = bpm;
                            break;

                        case EventType.Twirl:
                            ob.direction = -1;
                            break;

                        case EventType.Pause:
                            ob.extraBeats = eventValue.ToEvent<Pause>().Duration / 2.0;
                            break;

                        case EventType.Hold:
                            ob.extraHold = eventValue.ToEvent<Hold>().Duration;
                            ob.extraBeats = eventValue.ToEvent<Hold>().Duration;
                            break;

                        case EventType.MultiPlanet:
                            ob.multiPlanet =
                                eventValue.ToEvent<MultiPlanet>().Planets == EventEnums.PlanetCount.ThreePlanets ? 1 : 0;
                            break;

                        case EventType.FreeRoam:
                            ob.extraBeats = (eventValue.ToEvent<FreeRoam>().Duration - 1) / 2.0;
                            break;
                    }

                    parsedChart[tile] = ob;
                }
            }

            double currentBPM = level.GetSetting<double>("bpm") * pitch;
            int direction = 1;

            // 应用全局设置
            foreach (var t in parsedChart)
            {
                var ob = t;
                //if (ob == null) continue;
                // 方向处理
                if (ob.direction == -1)
                {
                    direction *= -1;
                }

                ob.direction = direction;

                // BPM处理
                if (ob.bpm == 0)
                {
                    ob.bpm = currentBPM;
                }
                else
                {
                    currentBPM = ob.bpm;
                }
            }

            List<(double,bool)> noteTime = [];

            double curAngle = 0;
            double curTime = 0;
            bool isMultiPlanet = false;

            foreach (var chartValue in parsedChart)
            {
                var o = chartValue;

                curAngle = Fmod(curAngle - 180, 360);
                double curBPM = o.bpm;
                double destAngle = o.angle;

                double pAngle = Fmod(destAngle - curAngle, 360) <= 0.001 || Fmod(destAngle - curAngle, 360) >= 359.999
                    ? 360
                    : Fmod((curAngle - destAngle) * o.direction, 360);

                pAngle += o.extraBeats * 360;

                // 三球处理逻辑
                double angleTemp = pAngle;
                if (isMultiPlanet)
                {
                    pAngle = pAngle > 60 ? pAngle - 60 : pAngle + 300;
                }

                int multiPlanet = o.multiPlanet;
                if (multiPlanet != -1)
                {
                    isMultiPlanet = multiPlanet == 1;
                    pAngle = isMultiPlanet
                        ? pAngle > 60 ? pAngle - 60 : pAngle + 300
                        : angleTemp;
                }

                // 计算时间
                double deltaTime = o.midr ? 0 : AngleToTime(pAngle, curBPM);
                curTime += deltaTime;

                curAngle = destAngle;
                noteTime.Add((curTime,o.extraHold > 0));
            }

            noteTimesCache = noteTime;
            if (addOffset)
            {
                noteTimesCacheWithOffset = noteTime.Select(t => (t.Item1 + level.GetSetting<int>("offset"), t.Item2)).ToList();
            }

            return noteTime;
            double Fmod(double a, double b) => a - b * Math.Floor(a / b);

            double AngleToTime(double angle, double bpm)
            {
                return angle / 180 * (60 / bpm) * 1000;
            }
        }

        /// <summary>
        /// Calculates note timing for each tile in the level (new implementation synced with the game's entryTime)
        /// 计算关卡中每个瓦片的音符时间（与游戏 entryTime 同步的新实现）
        /// </summary>
        /// <param name="addOffset">Whether to add the level settings offset to the times (default: false) / 是否将关卡设置中的偏移添加到时间中（默认：false）</param>
        /// <returns>List of note times in milliseconds / 以毫秒为单位的音符时间列表</returns>
        /// <remarks>
        /// Synced with scrLevelMaker.CalculateFloorEntryTimes: index i = floor i's entryTime,
        /// where entry 0 is always 0 and countdown is NOT included.
        /// 与游戏 CalculateFloorEntryTimes 同步：下标 i 为方块 i 的 entryTime，
        /// 第 0 项恒为 0（开始标记），不含倒计时。
        /// </remarks>
        public List<(double,bool)> GetNoteTimesNew(bool addOffset = false)
        {
            CheckCache(level);
            if (noteTimesCacheNew.Count > 0 && !addOffset) return noteTimesCacheNew;
            if (noteTimesCacheNewWithOffset.Count > 0 && addOffset) return noteTimesCacheNewWithOffset;
            var floors = FloorTimingCalculator.Calculate(level);
            noteTimesCacheNew = floors.Select(f => (f.EntryTime * 1000.0, f.HoldLength >= 0)).ToList();
            if (addOffset)
            {
                noteTimesCacheNewWithOffset = noteTimesCacheNew
                    .Select(t => (t.Item1 + level.GetSetting<int>("offset"), t.Item2)).ToList();
                return noteTimesCacheNewWithOffset;
            }

            return noteTimesCacheNew;
        }

        /// <summary>
        /// Gets all BPM changes throughout the level
        /// 获取整个关卡中的所有BPM变化
        /// </summary>
        /// <returns>List of speed values for each tile / 每个瓦片的速度值列表</returns>
        public List<double> GetAllSpeedChange()
        {
            CheckCache(level);
            if (allSpeedChange.Count > 0) return allSpeedChange;
            double[] speeds = new double[level.angleData.Count];
            double speed = level.GetSetting<double>("bpm");
            for (int i = 0; i < level.angles.Count; i++)
            {
                if (level.HasEvents(i, EventType.SetSpeed))
                {
                    var a = level.GetEvents(i, EventType.SetSpeed);
                    foreach (var o in a)
                    {
                        if (o["speedType"].ToObject<string>() == "Multiplier")
                        {
                            speed *= o["bpmMultiplier"].ToObject<double>();
                        }
                        else if (o["speedType"].ToObject<string>() == "Bpm")
                        {
                            speed = o["beatsPerMinute"].ToObject<double>();
                        }
                    }
                }

                speeds[i] = speed;
            }

            allSpeedChange = speeds.ToList();
            return speeds.ToList();
        }

        /// <summary>
        /// 生成滑音
        /// Generate a glide
        /// </summary>
        /// <param name="startFloor">开始的砖块 / The starting floor</param>
        /// <param name="startNote">开始的音调 / The starting pitch</param>
        /// <param name="endNote">结束的音调 / The ending</param>
        /// <param name="duration">时长(秒) / Duration (in seconds)</param>
        public void GenerateGlide(int startFloor, Pitch startNote, Pitch endNote, double duration)
        {
            if (startFloor < 0 || startFloor >= level.angles.Count)
                throw new ArgumentOutOfRangeException($"{nameof(startFloor)}:{startFloor}");
            double current = 0;
            double endFrequency = PitchHelper.GetFrequency(endNote);
            List<double> noteTimes = [];
            while (current < duration - 2 / endFrequency)
            {
                double currentPitch = PitchHelper.GetGlidePitch(startNote, endNote, current / duration);
                current += 1 / currentPitch;
                noteTimes.Add(currentPitch * 60);
            }

            noteTimes.Add(1 / (duration - current) * 60);

            for (int i = 0; i < noteTimes.Count; i++)
            {
                if (level.angles.Count - 1 < i)
                {
                    level.angleData.Add(0);
                }

                level.AddEvent(new SetSpeed()
                {
                    Floor = startFloor + i,
                    BeatsPerMinute = noteTimes[i],
                    SpeedType = EventEnums.SpeedType.Bpm
                });
            }
        }

        /// <summary>
        /// remove VFXs
        /// 移除视觉效果
        /// </summary>
        /// <param name="includeDecorations">是否包含装饰 / Whether to include decorations</param>
        /// <param name="includeTracks">是否包含砖块视觉效果 / Whether to include tracks</param> 
        /// <param name="onDelete">删除时的回调（传递被删除项的JSON字符串）；可为 null / Callback invoked on deletion (receives deleted item's JSON string); can be null</param>
        public void RemoveVFXs(bool includeDecorations = false, bool includeTracks = false,
            Action<string> onDelete = null)
        {
            List<EventType> vfxTypes =
            [
                EventType.SetFilter,
                EventType.SetFilterAdvanced,
                EventType.MoveCamera,
                EventType.Flash,
                EventType.Bloom,
                EventType.ScreenScroll,
                EventType.ShakeScreen,
                EventType.ScreenTile,
                EventType.CustomBackground,
                EventType.HallOfMirrors,
                EventType.SetFrameRate
            ];
            EventType[] decorationTypes =
            [
                EventType.AddDecoration,
                EventType.AddText,
                EventType.AddObject,
                EventType.AddParticle,
            ];
            if (includeDecorations)
            {
                vfxTypes.AddRange(
                [
                    EventType.EmitParticle,
                    EventType.SetParticle,
                    EventType.SetObject,
                    EventType.SetText,
                    EventType.SetDefaultText,
                    EventType.MoveDecorations,
                ]);
            }

            if (includeTracks)
            {
                vfxTypes.AddRange([
                    EventType.MoveTrack,
                    EventType.RecolorTrack,
                    EventType.ColorTrack,
                    EventType.AnimateTrack
                ]);
            }

            for (int i = 0; i < level.actions.Count; i++)
            {
                var action = level.actions[i];
                foreach (var type in vfxTypes)
                {
                    if (action["eventType"].ToObject<string>() == type.ToString())
                    {
                        level.actions.RemoveAt(i);
                        onDelete?.Invoke(action.ToString());
                        i--;
                        break;
                    }
                }
            }

            if (includeDecorations)
            {
                for (int i = 0; i < level.decorations.Count; i++)
                {
                    var decoration = level.decorations[i];
                    foreach (var type in decorationTypes)
                    {
                        if (decoration["eventType"].ToObject<string>() == type.ToString())
                        {
                            onDelete?.Invoke(decoration.ToString());
                            level.decorations.RemoveAt(i);
                            i--;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add a cube decorations with depth effect to the level / 向关卡添加一个具有深度效果的立方体装饰
        /// </summary>
        /// <param name="cubeImage">The image path or name of the cube texture / 立方体纹理的图像路径或名称</param>
        /// <param name="position">The initial position (x, y) of the cube / 立方体的初始位置(x,y)</param>
        /// <param name="size">The initial size (width, height) of the cube / 立方体的初始大小(宽度,高度)</param>
        /// <param name="floorCount">The number of depth layers to create (default: 100) / 要创建的深度层数（默认：100）</param>
        /// <param name="floor">The floor number (default: 0) / 地板编号（默认：0）</param>
        /// <param name="tag">The decoration tag (default: empty) / 装饰标签（默认：空）</param>
        /// <param name="relativeToScreen">Whether relative to screen or tile (default: false) / 是否相对于屏幕或瓦片（默认：false）</param>
        /// <remarks>
        /// This method creates a parallax effect by generating multiple layers of the same cube,
        /// each with different scale and parallax values based on their depth.
        /// 此方法通过生成同一立方体的多个层来创建视差效果，
        /// 每层根据其深度具有不同的缩放和视差值。
        /// </remarks>
        public void AddCube(string cubeImage, Tuple<float, float> position,
            Tuple<float, float> size, int floorCount = 100, int floor = 0, string tag = "", bool relativeToScreen = false)
        {
            for (int i = 0; i < floorCount; i++)
            {
                float depthRatio = (float)i / (floorCount - 1);
                float parallaxValue = 100 * depthRatio;
                float scaleMultiplier = 1.0f - depthRatio * 0.5f; // 最远处缩小到50%
                float scaledWidth = size.Item1 * scaleMultiplier;
                float scaledHeight = size.Item2 * scaleMultiplier;
                JObject data = new JObject
                {
                    ["decorationImage"] = cubeImage,
                    ["position"] = new JArray(position.Item1, position.Item2),
                    ["scale"] = new JArray(scaledWidth, scaledHeight),
                    ["parallax"] = new JArray(parallaxValue, parallaxValue), // 水平和垂直使用相同的平行值
                    ["depth"] = i, // 设置深度，确保正确的渲染顺序
                    ["syncFloorDepth"] = false
                };
                level.AddDecoration(floor, tag: tag, relativeToScreen: relativeToScreen, data: data);
            }
        }

        /// <summary>
        /// Creates Floor objects from the level's angle data with positions and properties
        /// 从关卡的角度数据创建Floor对象，包含位置和属性
        /// </summary>
        /// <param name="startPosition">Starting position for the first floor (default: Vector2.Zero) / 第一个地板的起始位置（默认：Vector2.Zero）</param>
        /// <param name="usePositionTrack">Whether to apply PositionTrack events to floor positions / 是否将PositionTrack事件应用于地板位置</param>
        /// <returns>List of Floor objects representing the level's path / 表示关卡路径的Floor对象列表</returns>
        /// <remarks>
        /// This method processes the level's angle data, events (SetSpeed, PositionTrack),
        /// and generates Floor instances with correct positions, angles, BPM, and mesh data.
        /// 此方法处理关卡的角度数据、事件（SetSpeed、PositionTrack），
        /// 并生成具有正确位置、角度、BPM和网格数据的Floor实例。
        /// </remarks>
        public List<Floor> CreateFloors(Vector2 startPosition = default,
            bool usePositionTrack = false)
        {
            List<Floor> floors = new List<Floor>();
            progressCallback?.Invoke(0, "获取打击时间点");
            var noteTimes = level.GetNoteTimes();
            double[] anglesArray = level.angles.ToArray();
            List<bool> midSpins = new();
            for (int i = 0; i < anglesArray.Length; i++)
            {
                // 官方 scrLevelMaker 的循环里 `scrFloor4` 是「当前处理的砖」，
                // `if (num4 == 999f) scrFloor4.midSpin = true;` 直接把标记打在这块砖上，
                // 所以 floorAngles[i] == 999 对应的就是 floors[i].isMidspin。
                // The game's loop uses scrFloor4 as the tile being processed, and
                // `if (num4 == 999f) scrFloor4.midSpin = true;` flags that same tile, so
                // floorAngles[i] == 999 maps straight onto floors[i].isMidspin.
                midSpins.Add(anglesArray[i].Equals(999));
                if (anglesArray[i].Equals(999))
                {
                    anglesArray[i] = anglesArray[i - 1] + 180;
                }
            }

            int n = anglesArray.Length + 1;
            double[] SetSpeedBpm = new Double[n];
            double[] SetSpeedMultiplier = new Double[n];
            bool[] setSpeedIsMultiplier = new Boolean[n];
            for (int i = 0; i < n; i++)
            {
                SetSpeedBpm[i] = 0;
                SetSpeedMultiplier[i] = 0;
                setSpeedIsMultiplier[i] = false;
            }

            Vector2 startPos = new Vector2(startPosition.X, startPosition.Y);
            progressCallback?.Invoke(25, "初始化事件");
            List<BaseEvent> allEvents = level.DeserializeEvents().ToList();
            for (int a = 0; a < allEvents.Count; a++)
            {
                BaseEvent eventObj = allEvents[a];
                int floor = eventObj.Floor;
                if (eventObj is SetSpeed setSpeed)
                {
                    if (setSpeed.SpeedType == EventEnums.SpeedType.Multiplier)
                    {
                        SetSpeedMultiplier[floor] = setSpeed.BpmMultiplier;
                        setSpeedIsMultiplier[floor] = true;
                    }
                    else
                    {
                        SetSpeedBpm[floor] = setSpeed.BeatsPerMinute;
                        setSpeedIsMultiplier[floor] = false;
                    }
                }
            }

            // 1. 并行批量创建Floor及其Mesh
            Floor[] tileArr = new Floor[n];
            Vector2[] posArr = new Vector2[n];
            float[] angle1Arr = new float[n];
            float[] angle2Arr = new float[n];
            progressCallback?.Invoke(50, "初始化位置");

            // 按地板号建索引，避免每格都扫一遍全表（那是 O(格数 × 事件数)）
            // Index by floor index instead of rescanning the whole list per tile
            Dictionary<int, List<BaseEvent>> eventsByFloor = BuildEventsByFloor(allEvents);

            // PositionTrack 同样按地板号索引 / Index the PositionTrack events the same way
            Dictionary<int, List<PositionTrack>> positionTracksByFloor = [];
            foreach (BaseEvent baseEvent in allEvents)
            {
                if (baseEvent is PositionTrack track && !track.EditorOnly)
                {
                    if (!positionTracksByFloor.TryGetValue(track.Floor, out List<PositionTrack>? list))
                    {
                        list = [];
                        positionTracksByFloor[track.Floor] = list;
                    }

                    list.Add(track);
                }
            }

            for (int i = 0; i < n; i++)
            {
                float angle1 = (i == anglesArray.Length) ? (float)anglesArray[i - 1] : (float)anglesArray[i];
                float angle2 = (i == 0) ? 0 : (float)anglesArray[i - 1];
                // 先判断当前floor是否有PositionTrack，若有则修正startPos
                if (usePositionTrack && positionTracksByFloor.TryGetValue(i, out List<PositionTrack>? tracks))
                {
                    foreach (PositionTrack positionTrack in tracks)
                    {
                        Vector2 position = new Vector2(positionTrack.PositionOffset[0].ToFloat(),
                            positionTrack.PositionOffset[1].ToFloat());
                        startPos += new Vector2(position.X * Floor.length * 2, position.Y * Floor.length * 2);
                    }
                }

                posArr[i] = new(startPos.X, startPos.Y);
                angle1Arr[i] = angle1;
                angle2Arr[i] = angle2;
                Vector2 step = new Vector2(Floor.length * 2 * FloatMath.Cos(angle1, true),
                    Floor.length * 2 * FloatMath.Sin(angle1, true));
                startPos += (step);
                Floor tile = new Floor(angle1Arr[i], angle2Arr[i] - 180, posArr[i]);
                if (i == anglesArray.Length)
                {
                    tile.isMidspin = (false);
                }
                else
                {
                    tile.isMidspin = (midSpins[i]);
                }

                tile.angle = i == anglesArray.Length ? (float)anglesArray[i - 1] + 180 : (float)anglesArray[i];
                tileArr[i] = tile;
            }

            progressCallback?.Invoke(75, "初始化变速与链表关系");
            double bpm = level.GetSetting<double>("bpm");
            bool isCW = true;
            for (int i = 0; i < tileArr.Length; i++)
            {
                Floor tile = tileArr[i];
                if (setSpeedIsMultiplier[i] && SetSpeedMultiplier[i] != 0)
                {
                    bpm *= SetSpeedMultiplier[i];
                }
                else if (SetSpeedBpm[i] != 0)
                {
                    bpm = SetSpeedBpm[i];
                }

                tile.bpm = bpm;

                // 事件取自预建的索引 / Events come from the prebuilt index
                eventsByFloor.TryGetValue(i, out List<BaseEvent>? floorEvents);
                tile.events = floorEvents ?? [];

                if (HasEventType(floorEvents, EventType.Twirl))
                {
                    isCW = !isCW;
                }

                tile.isCW = isCW;
                tile.index = i;
                if (i < tileArr.Length - 1)
                {
                    tile.nextFloor = tileArr[i + 1];
                }

                if (i > 0)
                {
                    tile.lastFloor = tileArr[i - 1];
                }

                tile.entryTime = noteTimes[i].Item1;
                tile.renderOrder = -i;
                tile.icon = ResolveFloorIcon(tile.events, tile, i > 0 ? tileArr[i - 1] : null);

                // 逐格随机的贴图变换（官方 _UV2Offset）：z 决定旋转，xy 决定平移。
                // 官方 RandomVector() 返回 Unity Random.value，即 **0..1 全区间**，
                // 而非小幅度抖动，这样相邻砖块采到的贴图区域才会完全不同。
                // The game's per-tile `_UV2Offset`: z rotates, xy translate. `RandomVector()` returns
                // Unity's `Random.value`, i.e. the full 0..1 range rather than a small jitter, which
                // is what makes neighbouring tiles sample far-apart texture regions.
                tile.textureRotation = Hash01(i);
                tile.textureOffsetX = Hash01(i * 3 + 1);
                tile.textureOffsetY = Hash01(i * 3 + 2);

                floors.Add(tile);
                progressCallback?.Invoke(75, $"初始化变速与链表关系({i}/{tileArr.Length - 1})");
            }

            progressCallback?.Invoke(100, "完成");
            return floors;
        }

        /// <summary>
        /// 按官方优先级从事件推断地板图标：Twirl &gt; Hold &gt; Pause &gt; SetSpeed，
        /// 并同时写入图标的翻转与旋转角。
        /// 旋转按本格扫过角分红色（&lt; 半圈）/ 蓝色（≥ 半圈），
        /// 变速按速度变化方向分兔子（加速）/ 蜗牛（减速）。
        /// Mirrors the game's precedence when picking the icon for a tile and fills in the icon's flip
        /// and rotation. A twirl picks the red swirl when the planet sweeps less than half a turn on
        /// this tile and the blue one otherwise; a speed change picks the rabbit when speeding up and
        /// the snail when slowing down.
        /// </summary>
        /// <param name="events">该格的事件 / Events on this tile.</param>
        /// <param name="tile">该格本身，用于读取变速与朝向 / The tile itself, for speed and orientation.</param>
        /// <param name="previous">该格的前一格 / The previous tile.</param>
        private static FloorIcon ResolveFloorIcon(List<BaseEvent> events, Floor tile, Floor? previous)
        {
            FloorIcon result = FloorIcon.None;
            bool twirlSeen = false;

            foreach (BaseEvent ev in events)
            {
                switch (ev)
                {
                    case Twirl when !twirlSeen:
                        twirlSeen = true;
                        return ResolveSwirlIcon(tile, previous);

                    case MultiPlanet when result == FloorIcon.None:
                        // 官方按行星数量变化分三档
                        // The game has three branches based on the planet count change
                        result = ResolveMultiPlanetIcon(ev as MultiPlanet);
                        break;

                    case Hold when result == FloorIcon.None:
                        // 官方按 holdLength 是否为 0 分长短两种箭头
                        // The game picks the long or short arrow from holdLength
                        result = ResolveHoldIcon(ev as Hold);
                        break;

                    case SetSpeed setSpeed when result == FloorIcon.None:
                        // 官方 scnGame：(speed - prevSpeed) / prevSpeed，正为加速、负为减速。
                        // bpm = 基准 bpm × speed，所以相对变化与 speed 完全等价。
                        // The game compares the tile's speed with the previous tile's. Since
                        // bpm = baseBpm * speed, the relative change matches the speed's exactly.
                        double previousBpm = previous?.bpm > 1e-9 ? previous.bpm : tile.bpm;
                        result = ResolveSpeedIcon(tile, setSpeed, previousBpm);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// 官方 <c>ResolveMultiPlanetIcon</c> 分支：两颗 / 三颗以上 / 行星减少。
        /// The game's multi-planet branch: two, more than two, or fewer.
        /// </summary>
        private static FloorIcon ResolveMultiPlanetIcon(MultiPlanet? multiPlanet)
        {
            // 官方 scnGame：本格 numPlanets 为 2 用 Two，比前一块多用 More，否则用 Less。
            // 这里 numPlanets 就是事件的目标行星数，2 之外只可能是 3。
            // The game uses Two when the tile's planet count is 2, More when it grew, Less otherwise.
            // Here the event's target count is the tile's count, so anything above 2 means three.
            return multiPlanet?.Planets == EventEnums.PlanetCount.ThreePlanets
                ? FloorIcon.MultiPlanetThreeMore
                : FloorIcon.MultiPlanetTwo;
        }

        /// <summary>官方按 <c>holdLength</c> 是否为零分长短箭头 / Long or short arrow from holdLength.</summary>
        private static FloorIcon ResolveHoldIcon(Hold? hold)
        {
            // 官方 scnGame：`holdLength = (int)item["duration"]`，随后
            // `floorIcon = (holdLength != 0) ? HoldArrowLong : HoldArrowShort`。
            // 也就是说取整后仍大于 0 才是长箭头，`0.9` 秒这类短按归为短箭头。
            // The game sets `holdLength = (int)duration` and then picks
            // `holdLength != 0 ? HoldArrowLong : HoldArrowShort`, so anything truncating to zero is
            // the short arrow.
            int holdLength = hold is null ? 0 : (int)hold.Duration;

            return holdLength != 0 ? FloorIcon.HoldArrowLong : FloorIcon.HoldArrowShort;
        }

        /// <summary>
        /// 官方 <c>scrFloor.UpdateIconSprite</c> 的旋转图标分支：同一个漩涡图标，
        /// 按本格扫过角分红色（小于半圈）/ 蓝色（半圈及以上）。
        /// The game's swirl branch: one icon whose colour depends on the swept angle — red below half
        /// a turn, blue at or above it.
        /// </summary>
        private static FloorIcon ResolveSwirlIcon(Floor tile, Floor? previous)
        {
            double previousAngle = previous?.angle ?? tile.angle;
            double swept = SweptDegrees(previousAngle, tile.angle, tile.isCW, tile.isMidspin);

            // 官方 SetIconFlipped(isCCW)，isCCW = !isCW / The game flips when turning counter-clockwise
            tile.iconFlipped = !tile.isCW;
            tile.iconAngle = IconAngleRadians(tile, previous, swept);

            // 官方：`num < PI` 用红色漩涡，否则蓝色（num 是本格扫过的弧度）。
            // The game uses the red swirl when the swept angle is under PI radians, blue otherwise.
            double sweptRadians = swept * Math.PI / 180.0;

            tile.swirlRed = sweptRadians < Math.PI;

            return FloorIcon.Swirl;
        }

        /// <summary>
        /// 图标旋转角（弧度），逐字复刻官方 <c>SetIconAngle</c> 的网格分支：
        /// <c>entryangle + 扫过角 / 2 * (isCW ? 1 : -1)</c>。
        /// 其中 <c>entryangle</c> 是官方角度空间的方位角 <c>270° - 前一格角度</c>，
        /// 因为两边的顶点坐标数值一致，直接沿用官方数值即可，不需要换算。
        /// The icon's rotation, replicating the game's mesh branch of <c>SetIconAngle</c>. The game's
        /// <c>entryangle</c> lives in its own angle space (<c>270 - previous tile angle</c>); both
        /// engines produce the same vertex coordinates, so the raw value carries over unchanged.
        /// </summary>
        private static float IconAngleRadians(Floor tile, Floor? previous, double sweptDegrees)
        {
            // 官方 entryangle = exitangle(prev) + 180°，exitangle = 90° - 角度；
            // 第 0 格没有上一格，官方的 entryangle 固定为 270°。
            // The game's entryangle is exitangle(prev) + 180 where exitangle = 90 - tileAngle. Floor 0
            // has no predecessor and the game hard-codes its entry angle to 270 deg.
            double entry = previous is null ? 270.0 : 270.0 - previous.angle;

            double direction = tile.isCW ? 1.0 : -1.0;
            return (float)((entry + sweptDegrees / 2.0 * direction) * Math.PI / 180.0);
        }

        /// <summary>
        /// 按速度相对变化挑官方四档图标，逐字对应 <c>scnGame.CreateFloor</c>：
        /// <c>(speed - prevSpeed) / prevSpeed</c> 为 <c>delta</c>，<c>abs(delta) &lt;= 0.05</c> 时是「速度不变」；
        /// 加速时 <c>abs &lt; 1.05</c> 为兔子、否则双兔子；减速时 <c>1 - abs &gt; 0.45</c> 为蜗牛、否则双蜗牛。
        /// Picks the game's four speed tiers. With `delta = (speed - prevSpeed) / prevSpeed`, an
        /// absolute change under 0.05 is "unchanged"; speeding up gives the rabbit below 1.05 and the
        /// double rabbit above; slowing down gives the snail above 0.45 remaining and the double snail
        /// below.
        /// </summary>
        /// <param name="tile">本格 / This tile.</param>
        /// <param name="setSpeed">触发图标的事件 / The event that triggered the icon.</param>
        /// <param name="previousBpm">前一格的 BPM / The previous tile's BPM.</param>
        private static FloorIcon ResolveSpeedIcon(Floor tile, SetSpeed setSpeed, double previousBpm)
        {
            double reference = previousBpm > 1e-9 ? previousBpm : (tile.bpm > 1e-9 ? tile.bpm : 120.0);
            double delta = (tile.bpm - reference) / reference;

            // 事件自己就说明了变化方向时以它为准，避免 bpm 回填时机带来的歧义
            // Prefer the event's own setting, which states the direction unambiguously
            if (setSpeed.SpeedType == EventEnums.SpeedType.Multiplier)
            {
                delta = setSpeed.BpmMultiplier - 1.0;
            }
            else if (Math.Abs(delta) < 1e-9 && setSpeed.BeatsPerMinute > 1e-9)
            {
                delta = setSpeed.BeatsPerMinute - reference;
            }

            double magnitude = Math.Abs(delta);

            if (magnitude <= 0.05)
            {
                return FloorIcon.SameSpeed;
            }

            if (delta > 0)
            {
                return magnitude < 1.05 ? FloorIcon.Rabbit : FloorIcon.DoubleRabbit;
            }

            return 1.0 - magnitude > 0.45 ? FloorIcon.Snail : FloorIcon.DoubleSnail;
        }

        /// <summary>
        /// 本格行星扫过的角度（度），对应官方 <c>scrMisc.GetAngleMoved</c> 的 mod 语义。
        /// 掉头砖 mod 后为 0，但官方仍按整圈处理；只有中旋格真的不动。
        /// The swept angle in degrees for a tile, mirroring <c>scrMisc.GetAngleMoved</c>'s mod
        /// semantics. A U-turn tile collapses to 0 but still travels a full circle; only midspin tiles
        /// genuinely do not move.
        /// </summary>
        /// <param name="previousAngle">前一格朝向 / Previous tile heading in degrees.</param>
        /// <param name="angle">本格朝向 / This tile's heading in degrees.</param>
        /// <param name="isCW">是否顺时针 / Whether the rotation is clockwise.</param>
        /// <param name="isMidspin">是否中旋格 / Whether this is a midspin tile.</param>
        public static double SweptDegrees(double previousAngle, double angle, bool isCW, bool isMidspin)
        {
            double direction = isCW ? -1.0 : 1.0;
            double swept = Mod360((angle - (previousAngle + 180.0)) * direction);

            if (swept <= 1e-6 || swept >= 360.0 - 1e-6)
            {
                swept = isMidspin ? 0.0 : 360.0;
            }

            return swept;
        }

        private static double Mod360(double degrees) => (degrees % 360.0 + 360.0) % 360.0;

        /// <summary>
        /// 把事件按地板号分组，一次遍历换取后续每次 O(1) 查询。
        /// Groups events by floor index: one pass now, O(1) lookups afterwards.
        /// </summary>
        private static Dictionary<int, List<BaseEvent>> BuildEventsByFloor(List<BaseEvent> events)
        {
            Dictionary<int, List<BaseEvent>> byFloor = new(events.Count);

            foreach (BaseEvent ev in events)
            {
                int floor = ev.Floor;
                if (!byFloor.TryGetValue(floor, out List<BaseEvent>? list))
                {
                    list = [];
                    byFloor[floor] = list;
                }

                list.Add(ev);
            }

            return byFloor;
        }

        /// <summary>
        /// 查询某格是否含有指定类型的事件（等价 <c>level.HasEvents(floor, type)</c>，但不重新扫描）。
        /// Whether the floor's event list contains the given type, without rescanning the whole level.
        /// </summary>
        private static bool HasEventType(List<BaseEvent>? events, EventType type)
        {
            if (events is null)
            {
                return false;
            }

            foreach (BaseEvent ev in events)
            {
                if (ev.EventType == type)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 把地板索引散列成 [0,1) 的确定性随机数，用于砖块贴图的随机朝向。
        /// Hashes a floor index into a deterministic value in [0,1) for the tile's random orientation.
        /// </summary>
        private static float Hash01(int value)
        {
            // 32 位整型的雪崩混合，分布均匀且不依赖运行时的随机源
            // A 32-bit avalanche mix: well distributed and independent of any runtime RNG
            uint x = (uint)value;
            x ^= x >> 16;
            x *= 0x7feb352dU;
            x ^= x >> 15;
            x *= 0x846ca68bU;
            x ^= x >> 16;
            return (x & 0xFFFFFF) / 16777216f;
        }

        /// <summary>
        /// Get the floor index corresponding to a specific note time in milliseconds
        /// 获取与特定音符时间（以毫秒为单位）对应的地板索引
        /// </summary>
        /// <param name="noteTimeSecond">The note time in seconds / 以秒为单位的音符时间</param>
        /// <returns>The floor index / 地板索引</returns>
        public int GetFloorIndexByNoteTime(double noteTimeSecond)
        {
            List<double> noteTimes = level.GetNoteTimes(true).Select(a => a.Item1).ToList();
            double targetTime = noteTimeSecond * 1000;

            int left = 0;
            int right = noteTimes.Count - 1;

            // 如果目标时间大于所有元素，直接返回最后一个索引
            if (targetTime >= noteTimes[right])
            {
                return right;
            }

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (noteTimes[mid] >= targetTime)
                {
                    // 如果mid是第一个满足条件的元素，或者是当前元素满足条件但前一个不满足
                    if (mid == 0 || noteTimes[mid - 1] < targetTime)
                    {
                        return mid;
                    }

                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return noteTimes.Count - 1;
        }
    }
}