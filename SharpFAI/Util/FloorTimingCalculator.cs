using System;
using System.Collections.Generic;
using System.Linq;
using SharpFAI.Events;
using SharpFAI.Serialization;

namespace SharpFAI.Util;

/// <summary>
/// Replicates the game's floor entry time calculation
/// (scrLevelMaker.InstantiateFloatFloors → scnGame.ApplyCoreEventsToFloors → scrLevelMaker.CalculateFloorEntryTimes)
/// 复刻游戏源码的方块 entryTime 计算流程（角度构建 → 核心事件应用 → 进块时间计算）
/// </summary>
public static class FloorTimingCalculator
{
    // 与游戏源码一致的常量（由 float 精度导出 / Constants as derived in game source from float precision）
    private const double Pi = 3.1415927410125732;
    private const double TwoPi = 6.2831854820251465;
    private const double FirstEntryAngle = 4.71238899230957;
    private const int CheckpointSpacing = 128;

    internal struct TimingCheckpoint(double entryAngle, double speed, bool isCCW, int numPlanets)
    {
        internal double EntryAngle = entryAngle;
        internal double Speed = speed;
        internal bool IsCCW = isCCW;
        internal int NumPlanets = numPlanets;
        internal double EntryTime;
    }

    /// <summary>Cached incoming timing state for an existing editor path and its settings.</summary>
    public sealed class TimingCheckpoints
    {
        internal readonly int FloorCount;
        internal readonly TimingCheckpoint[] States;

        internal TimingCheckpoints(int floorCount)
        {
            FloorCount = floorCount;
            States = new TimingCheckpoint[(floorCount - 1) / CheckpointSpacing + 1];
        }
    }

    /// <summary>Recomputed timing from <paramref name="firstFloor"/> onward.</summary>
    public sealed class EditorTiming(int firstFloor, IReadOnlyList<FloorData> floors,
        IReadOnlyDictionary<int, SpeedContext> speedContexts, TimingCheckpoints checkpoints)
    {
        /// <summary>Chart index where the returned timing begins.</summary>
        public int FirstFloor { get; } = firstFloor;
        /// <summary>Timing for the recalculated floors, starting at FirstFloor.</summary>
        public IReadOnlyList<FloorData> Floors { get; } = floors;
        /// <summary>Speed conversion inputs keyed by chart floor in the recalculated range.</summary>
        public IReadOnlyDictionary<int, SpeedContext> SpeedContexts { get; } = speedContexts;
        /// <summary>Incoming timing states for the entire current chart.</summary>
        public TimingCheckpoints Checkpoints { get; } = checkpoints;
    }

    /// <summary>
    /// Per-floor timing state / 单个方块的时间状态
    /// </summary>
    public class FloorData
    {
        /// <summary>进块角度（弧度）/ Entry angle (radians)</summary>
        public double EntryAngle;

        /// <summary>出块角度（弧度）/ Exit angle (radians)</summary>
        public double ExitAngle;

        /// <summary>是否逆时针旋转 / Whether the planet rotates counter-clockwise</summary>
        public bool IsCCW;

        /// <summary>是否中旋方块（999）/ Whether this floor is a midspin (999)</summary>
        public bool MidSpin;

        /// <summary>速度倍率 / Speed multiplier relative to level BPM</summary>
        public double Speed = 1;

        /// <summary>行星数量 / Planet count (2 or 3)</summary>
        public int NumPlanets = 2;

        /// <summary>Hold 时长（拍）/ Hold length in beats (-1 = none)</summary>
        public int HoldLength = -1;

        /// <summary>额外拍数（Pause / FreeRoam）/ Extra beats (Pause / FreeRoam)</summary>
        public double ExtraBeats;

        /// <summary>事件应用前的角度行程（弧度，FreeRoam 使用）/ Pre-event angle length (radians, used by FreeRoam)</summary>
        public double AngleLength;

        /// <summary>进块时间（秒，不含倒计时）/ Entry time in seconds (no countdown)</summary>
        public double EntryTime;
    }

    /// <summary>
    /// Calculates per-floor timing data exactly like the game
    /// 完全按照游戏逻辑计算每个方块的时间数据
    /// </summary>
    /// <param name="level">The level to calculate / 要计算的关卡</param>
    /// <returns>Floor data list, index i = floor i (count = angleData.Count + 1) / 方块数据列表，下标 i 即方块 i（数量 = angleData.Count + 1）</returns>
    public static List<FloorData> Calculate(Level level) => CalculateCore(level, null, null);

    /// <summary>Calculate timing with a previously prepared set of core events.</summary>
    public static List<FloorData> Calculate(Level level, FloorCoreEvents coreEvents) => CalculateCore(level, coreEvents, null);

    /// <summary>Inputs required to evaluate speed changes on a tile independently of the rest of the chart.</summary>
    public readonly struct SpeedContext(double bpm, double incomingMultiplier, double angleDegrees)
    {
        /// <summary>The level's base BPM.</summary>
        public double Bpm { get; } = bpm;
        /// <summary>The speed multiplier entering the tile.</summary>
        public double IncomingMultiplier { get; } = incomingMultiplier;
        /// <summary>The angle span used to interpolate speed changes inside the tile.</summary>
        public double AngleDegrees { get; } = angleDegrees;
    }

    /// <summary>Calculate timing and collect local conversion inputs for floors with speed changes.</summary>
    public static List<FloorData> Calculate(Level level, FloorCoreEvents? coreEvents, IDictionary<int, SpeedContext> speedContexts) =>
        CalculateCore(level, coreEvents, speedContexts);

    /// <summary>Reuse the previous timing prefix when only events at or after the supplied floor have changed.</summary>
    /// <remarks>Callers must supply previous checkpoints only when the angle data and timing settings are unchanged.</remarks>
    public static EditorTiming CalculateForEditor(Level level, FloorCoreEvents? coreEvents,
        TimingCheckpoints? previous = null, int firstChangedFloor = 0)
    {
        int count = level.angles.Count + 1;
        var checkpoints = new TimingCheckpoints(count);
        // A planet-count event can also change the midspin directly before it.
        int checkpointIndex = previous?.FloorCount == count && firstChangedFloor > 0 && firstChangedFloor < count
            ? (firstChangedFloor - 1) / CheckpointSpacing : 0;
        int firstFloor = checkpointIndex * CheckpointSpacing;
        var contexts = new Dictionary<int, SpeedContext>();
        TimingCheckpoint incoming = new(FirstEntryAngle, 1.0, false, 2);
        if (checkpointIndex > 0)
        {
            Array.Copy(previous!.States, checkpoints.States, checkpointIndex);
            incoming = previous.States[checkpointIndex];
        }
        var floors = CalculateSegment(level, coreEvents, contexts, firstFloor, incoming, checkpoints.States);
        for (int i = checkpointIndex; i < checkpoints.States.Length; i++)
            checkpoints.States[i].EntryTime = floors[i * CheckpointSpacing - firstFloor].EntryTime;

        return new EditorTiming(firstFloor, floors, contexts, checkpoints);
    }

    private static List<FloorData> CalculateCore(Level level, FloorCoreEvents? coreEvents, IDictionary<int, SpeedContext>? speedContexts)
        => CalculateSegment(level, coreEvents, speedContexts, 0, new TimingCheckpoint(FirstEntryAngle, 1.0, false, 2), null)
            .ToList();

    private static FloorData[] CalculateSegment(Level level, FloorCoreEvents? coreEvents, IDictionary<int, SpeedContext>? speedContexts,
        int firstFloor, TimingCheckpoint incoming, TimingCheckpoint[]? checkpoints)
    {
        var angles = level.angles;
        var floors = new FloorData[angles.Count + 1 - firstFloor];
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i] = new FloorData();
        }

        BuildFloors(angles, floors, firstFloor, incoming.EntryAngle);

        // 游戏在应用事件前先用默认状态计算角度行程（供 FreeRoam 使用）
        // The game computes angle lengths with default state before applying events (used by FreeRoam)
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i].AngleLength = GetAngleLength(floors[i], isCCW: false, numPlanets: 2,
                holdLength: -1, previousMidSpin: i > 0 && floors[i - 1].MidSpin);
        }

        ApplyCoreEvents(level, floors, coreEvents, speedContexts, firstFloor, incoming, checkpoints);
        CalculateEntryTimes(level, floors, firstFloor, incoming.EntryTime);
        return floors;
    }

    /// <summary>
    /// Builds entry/exit angles from angle data (scrLevelMaker.InstantiateFloatFloors)
    /// 根据角度数据构建进块/出块角度
    /// </summary>
    private static void BuildFloors(IList<double> angles, FloorData[] floors, int firstFloor, double entryAngle)
    {
        floors[0].EntryAngle = entryAngle;
        for (int j = 0; j < floors.Length - 1; j++)
        {
            var f = floors[j];
            double angle = angles[firstFloor + j];
            if (angle == 999)
            {
                f.ExitAngle = f.EntryAngle;
                f.MidSpin = true;
            }
            else
            {
                f.ExitAngle = (90 - angle) * Pi / 180.0;
            }

            floors[j + 1].EntryAngle = Mod(f.ExitAngle + Pi, TwoPi);
        }

        floors[floors.Length - 1].ExitAngle = floors[floors.Length - 1].EntryAngle + Pi;
    }

    /// <summary>
    /// Applies core events to floors (scnGame.ApplyCoreEventsToFloors)
    /// 将核心事件（Twirl/Pause/FreeRoam/Hold/MultiPlanet/SetSpeed）应用到方块上
    /// </summary>
    private static void ApplyCoreEvents(Level level, FloorData[] floors, FloorCoreEvents? coreEvents,
        IDictionary<int, SpeedContext>? speedContexts, int firstFloor, TimingCheckpoint incoming, TimingCheckpoint[]? checkpoints)
    {
        double bpm = level.GetSetting<double>("bpm");
        double speedMult = incoming.Speed;
        bool isCCW = incoming.IsCCW;
        int numPlanets = incoming.NumPlanets;

        var eventsByFloor = new Dictionary<int, List<BaseEvent>>();
        int floorCount = firstFloor + floors.Length;
        var twirls = coreEvents?.OddTwirls ?? new System.Collections.BitArray(floorCount);
        IReadOnlyList<BaseEvent> source = coreEvents is null ? level.DeserializeEvents() : coreEvents.Others;
        foreach (var e in source)
        {
            if (e.Floor < firstFloor || e.Floor >= floorCount) continue;
            if (e.EventType == EventType.Twirl)
            {
                twirls[e.Floor] = !twirls[e.Floor];
                continue;
            }
            if (e.EventType is not (EventType.SetSpeed or EventType.Pause or EventType.FreeRoam
                or EventType.Hold or EventType.MultiPlanet))
                continue;

            if (!eventsByFloor.TryGetValue(e.Floor, out var list))
            {
                list = [];
                eventsByFloor[e.Floor] = list;
            }

            list.Add(e);
        }

        for (int i = 0; i < floors.Length; i++)
        {
            var f = floors[i];
            int floor = firstFloor + i;
            if (checkpoints is not null && floor % CheckpointSpacing == 0)
                checkpoints[floor / CheckpointSpacing] = new TimingCheckpoint(f.EntryAngle, speedMult, isCCW, numPlanets);
            if (twirls[floor])
                isCCW = !isCCW;
            f.ExtraBeats = 0.0;
            bool hasNext = i < floors.Length - 1;
            double floorSpeed = speedMult;

            if (eventsByFloor.TryGetValue(floor, out var events))
            {
                var setSpeeds = events.Where(e => e.EventType == EventType.SetSpeed)
                    .Select(e => e.ToEvent<SetSpeed>()).ToList();

                foreach (var e in events)
                {
                    switch (e.EventType)
                    {
                        case EventType.Pause:
                            if (hasNext && !f.MidSpin)
                            {
                                f.ExtraBeats += e.ToEvent<Pause>().Duration;
                            }

                            break;

                        case EventType.FreeRoam:
                        {
                            var fr = e.ToEvent<FreeRoam>();
                            if (hasNext && (int)fr.Duration >= 2)
                            {
                                double beats = f.AngleLength / Pi;
                                f.ExtraBeats += Math.Max(beats, (int)fr.Duration - beats);
                            }

                            break;
                        }

                        case EventType.Hold:
                        {
                            var h = e.ToEvent<Hold>();
                            f.HoldLength = (hasNext && h.Duration >= 0) ? (int)h.Duration : -1;
                            break;
                        }

                        case EventType.MultiPlanet:
                        {
                            var mp = e.ToEvent<MultiPlanet>();
                            numPlanets = Math.Max((int)mp.Planets, 2);
                            numPlanets = Math.Min(numPlanets, 3);
                            if (i > 0 && floors[i - 1].MidSpin)
                            {
                                floors[i - 1].NumPlanets = numPlanets;
                            }

                            break;
                        }
                    }
                }

                if (setSpeeds.Count > 0)
                {
                    double floorAngleDeg = ApproximatelyFloor(f.EntryAngle, f.ExitAngle)
                        ? 360.0
                        : GetAngleMoved(f.EntryAngle, f.ExitAngle, !isCCW) * 57.29578;
                    var context = new SpeedContext(bpm, speedMult, floorAngleDeg);
                    if (speedContexts is not null)
                        speedContexts[floor] = context;
                    floorSpeed = ResolveSpeed(context, setSpeeds, out speedMult);
                }
            }

            f.Speed = floorSpeed;
            f.IsCCW = isCCW;
            f.NumPlanets = numPlanets;
        }
    }

    /// <summary>Resolve the speed for this tile and the multiplier inherited by the next tile.</summary>
    public static double ResolveSpeed(SpeedContext context, IReadOnlyList<SetSpeed> changes, out double nextMultiplier)
    {
        double bpm = context.Bpm;
        nextMultiplier = context.IncomingMultiplier;
        if (changes.Count == 0) return nextMultiplier;

        var offsets = changes.GroupBy(change => change.AngleOffset).OrderBy(group => group.Key).ToArray();
        double totalTime = offsets[0].Key / 180.0 * (60.0 / (bpm * nextMultiplier));
        bool hasMidFloorOffset = false;
        for (int index = 0; index < offsets.Length; index++)
        {
            var group = offsets[index];
            double offset = group.Key;
            var last = group.Last();
            double nextOffset = index + 1 < offsets.Length ? offsets[index + 1].Key : context.AngleDegrees;
            if (nextOffset == 0.0) nextOffset = context.AngleDegrees;

            double newBpm = last.BeatsPerMinute;
            if (last.SpeedType != EventEnums.SpeedType.Bpm)
            {
                newBpm = bpm * nextMultiplier;
                foreach (var change in group)
                    newBpm = change.SpeedType == EventEnums.SpeedType.Bpm ? change.BeatsPerMinute : newBpm * change.BpmMultiplier;
            }

            totalTime += (nextOffset - offset) / 180.0 * (60.0 / newBpm);
            hasMidFloorOffset |= offset > 0 && offset <= context.AngleDegrees;
            nextMultiplier = newBpm / bpm;
        }

        return hasMidFloorOffset ? 60.0 / totalTime * (context.AngleDegrees / 180.0) / bpm : nextMultiplier;
    }

    /// <summary>
    /// Calculates entry times (scrLevelMaker.CalculateFloorEntryTimes)
    /// 计算每个方块的进块时间
    /// </summary>
    private static void CalculateEntryTimes(Level level, FloorData[] floors, int firstFloor, double entryTime)
    {
        double bpm = level.GetSetting<double>("bpm");
        if (bpm <= 1e-6)
        {
            bpm = 120.0;
        }

        floors[0].EntryTime = entryTime;
        if (floors.Length == 1) return;

        // 官方 scrLevelMaker.CalculateFloorEntryTimes：
        //   num = crotchetAtStart * (adjustedCountdownTicks - 1) + GetTimeBetweenAngles(floor0)
        //   listFloors[0].entryTime = 0; listFloors[1].entryTime = num;
        // 第 1 格的 entryTime 必须包含倒计时拍数，否则整条时间轴会提前 (countdownTicks - 1) 拍。
        // Official: floor 1's entryTime includes (adjustedCountdownTicks - 1) beats of countdown;
        // omitting it shifts the whole timeline earlier by that many beats.
        int first = 0;
        double time = entryTime;
        if (firstFloor == 0)
        {
            int countdownTicks = 4;
            if (level.HasSetting("countdownTicks"))
            {
                try
                {
                    countdownTicks = level.GetSetting<int>("countdownTicks");
                }
                catch (Exception)
                {
                    countdownTicks = 4;
                }
            }
            var opening = floors[0];
            time = (60.0 / bpm) * (Math.Max(1, countdownTicks) - 1)
                   + GetTimeBetweenAngles(opening.EntryAngle, opening.ExitAngle, opening.Speed, bpm, !opening.IsCCW);
            floors[1].EntryTime = time;
            first = 1;
        }

        for (int i = first; i < floors.Length - 1; i++)
        {
            var f = floors[i];
            double num3 = GetInverseAnglePerBeatMultiplanet(f.NumPlanets) * (f.IsCCW ? -1 : 1);
            if (f.MidSpin)
            {
                num3 = 0.0;
            }

            if (f.NumPlanets > 2 && (i > 0 ? floors[i - 1].MidSpin : level.angles[firstFloor - 1] == 999))
            {
                num3 -= (TwoPi + GetInverseAnglePerBeatMultiplanet(f.NumPlanets)) * (f.IsCCW ? -1 : 1);
            }

            double num4 = GetTimeBetweenAngles(f.EntryAngle + num3, f.ExitAngle + (f.MidSpin ? num3 : 0.0), f.Speed,
                bpm, !f.IsCCW);
            double timeBetweenAngles = GetTimeBetweenAngles(0.0, Pi, f.Speed, bpm, isCW: false);
            double angleMoved = GetAngleMoved(f.EntryAngle + num3, f.ExitAngle + (f.MidSpin ? num3 : 0.0), !f.IsCCW);
            if (angleMoved <= 1E-06 || angleMoved >= 6.283184482025146)
            {
                num4 = f.MidSpin ? 0.0 : 2.0 * timeBetweenAngles;
            }

            time += num4;
            if (f.HoldLength > 0)
            {
                time += f.HoldLength * 2 * timeBetweenAngles;
            }

            time += f.ExtraBeats * timeBetweenAngles;
            floors[i + 1].EntryTime = time;
        }
    }

    /// <summary>
    /// Calculates a floor's angle length (scrLevelMaker.CalculateSingleFloorAngleLength)
    /// 计算方块的角度行程
    /// </summary>
    private static double GetAngleLength(FloorData cf, bool isCCW, int numPlanets, int holdLength, bool previousMidSpin)
    {
        double num = GetInverseAnglePerBeatMultiplanet(numPlanets) * ((!isCCW) ? 1 : -1);
        if (cf.MidSpin)
        {
            num = 0.0;
        }

        if (previousMidSpin && numPlanets > 2)
        {
            num -= (TwoPi + GetInverseAnglePerBeatMultiplanet(numPlanets)) * ((!isCCW) ? 1 : -1);
        }

        double num2 = GetAngleMoved(cf.EntryAngle + num, cf.ExitAngle + (cf.MidSpin ? num : 0.0), !isCCW);
        double num3 = Math.Abs(num2);
        if (num3 <= 1E-06 || num3 >= 6.283184307179586)
        {
            num2 = cf.MidSpin ? 0.0 : TwoPi;
        }

        if (holdLength > 0)
        {
            num2 += holdLength * 2 * Pi;
        }

        return num2;
    }

    /// <summary>
    /// Angle moved between entry and exit (scrMisc.GetAngleMoved)
    /// 进块到出块经过的角度
    /// </summary>
    private static double GetAngleMoved(double entryAngle, double exitAngle, bool isCW)
        => Mod((exitAngle - entryAngle) * (isCW ? 1.0 : -1.0), TwoPi);

    /// <summary>
    /// Travel time in seconds between two angles (scrMisc.GetTimeBetweenAngles)
    /// 两个角度之间的行程时间（秒）
    /// </summary>
    private static double GetTimeBetweenAngles(double entryAngle, double exitAngle, double speed, double bpm, bool isCW)
        => GetAngleMoved(entryAngle, exitAngle, isCW) / Pi * (60.0 / bpm / speed);

    /// <summary>
    /// Inverse angle per beat for multiplanet (scrMisc.GetInverseAnglePerBeatMultiplanet)
    /// 多星球每拍反向角度
    /// </summary>
    private static double GetInverseAnglePerBeatMultiplanet(double planets)
        => 3.1415926 * (planets - 2.0) / planets;

    /// <summary>
    /// Positive modulo (scrMisc.mod) / 正取模
    /// </summary>
    private static double Mod(double x, double m) => (x % m + m) % m;

    /// <summary>
    /// Angle equality check (scrMisc.ApproximatelyFloor) / 角度近似相等判断
    /// </summary>
    private static bool ApproximatelyFloor(double a, double b) => Math.Abs(a - b) < 0.001;
}
