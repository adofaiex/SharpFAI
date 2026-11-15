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
    public static List<FloorData> Calculate(Level level)
    {
        var angles = level.angles;
        var floors = new FloorData[angles.Count + 1];
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i] = new FloorData();
        }

        BuildFloors(angles, floors);

        // 游戏在应用事件前先用默认状态计算角度行程（供 FreeRoam 使用）
        // The game computes angle lengths with default state before applying events (used by FreeRoam)
        for (int i = 0; i < floors.Length; i++)
        {
            floors[i].AngleLength = GetAngleLength(floors, i, isCCW: false, numPlanets: 2, holdLength: -1);
        }

        ApplyCoreEvents(level, floors);
        CalculateEntryTimes(level, floors);
        return floors.ToList();
    }

    /// <summary>
    /// Builds entry/exit angles from angle data (scrLevelMaker.InstantiateFloatFloors)
    /// 根据角度数据构建进块/出块角度
    /// </summary>
    private static void BuildFloors(IList<double> angles, FloorData[] floors)
    {
        floors[0].EntryAngle = FirstEntryAngle;
        for (int j = 0; j < angles.Count; j++)
        {
            var f = floors[j];
            double angle = angles[j];
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
    private static void ApplyCoreEvents(Level level, FloorData[] floors)
    {
        double bpm = level.GetSetting<double>("bpm");
        double speedMult = 1.0;
        bool isCCW = false;
        int numPlanets = 2;

        var eventsByFloor = new Dictionary<int, List<BaseEvent>>();
        foreach (var e in level.DeserializeEvents())
        {
            if (e.Floor < 0 || e.Floor >= floors.Length) continue;
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
            f.ExtraBeats = 0.0;
            bool hasNext = i < floors.Length - 1;
            bool hasMidFloorOffset = false;
            double speedAverage = 1.0;

            if (eventsByFloor.TryGetValue(i, out var events))
            {
                var setSpeeds = events.Where(e => e.EventType == EventType.SetSpeed)
                    .Select(e => e.ToEvent<SetSpeed>()).ToList();
                var offsets = setSpeeds.Select(e => e.AngleOffset).Distinct().OrderBy(o => o).ToList();

                foreach (var e in events)
                {
                    switch (e.EventType)
                    {
                        case EventType.Twirl:
                            isCCW = !isCCW;
                            break;

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

                if (offsets.Count > 0)
                {
                    double floorAngleDeg = ApproximatelyFloor(f.EntryAngle, f.ExitAngle)
                        ? 360.0
                        : GetAngleMoved(f.EntryAngle, f.ExitAngle, !isCCW) * 57.29578;
                    double crotchet = 60.0 / (bpm * speedMult);
                    double totalTime = offsets[0] / 180.0 * crotchet;
                    foreach (double angleOffset in offsets)
                    {
                        var lastSpeed = setSpeeds.Last(e => e.AngleOffset == angleOffset);
                        double nextOffset = offsets.FirstOrDefault(o => o > angleOffset);
                        if (nextOffset == 0.0)
                        {
                            nextOffset = floorAngleDeg;
                        }

                        double newBpm = 0.0;
                        if (lastSpeed.SpeedType == EventEnums.SpeedType.Bpm)
                        {
                            newBpm = lastSpeed.BeatsPerMinute;
                        }
                        else
                        {
                            var sameOffset = setSpeeds.Where(e => e.AngleOffset == angleOffset).ToList();
                            for (int k = 0; k < sameOffset.Count; k++)
                            {
                                var s = sameOffset[k];
                                if (s.SpeedType == EventEnums.SpeedType.Bpm)
                                {
                                    newBpm = s.BeatsPerMinute;
                                }
                                else
                                {
                                    newBpm = (k <= 0) ? bpm * speedMult * s.BpmMultiplier : newBpm * s.BpmMultiplier;
                                }
                            }
                        }

                        double segCrotchet = 60.0 / newBpm;
                        bool inFloor = angleOffset > 0.0 && angleOffset <= floorAngleDeg;
                        totalTime += (nextOffset - angleOffset) / 180.0 * segCrotchet;
                        if (inFloor)
                        {
                            hasMidFloorOffset = true;
                        }

                        speedMult = newBpm / bpm;
                    }

                    if (hasMidFloorOffset)
                    {
                        speedAverage = 60.0 / totalTime * (floorAngleDeg / 180.0) / bpm;
                    }
                }
            }

            f.Speed = hasMidFloorOffset ? speedAverage : speedMult;
            f.IsCCW = isCCW;
            f.NumPlanets = numPlanets;
        }
    }

    /// <summary>
    /// Calculates entry times (scrLevelMaker.CalculateFloorEntryTimes)
    /// 计算每个方块的进块时间
    /// </summary>
    private static void CalculateEntryTimes(Level level, FloorData[] floors)
    {
        double bpm = level.GetSetting<double>("bpm");
        if (bpm <= 1e-6)
        {
            bpm = 120.0;
        }

        if (floors.Length == 1)
        {
            return;
        }

        // 官方 scrLevelMaker.CalculateFloorEntryTimes：
        //   num = crotchetAtStart * (adjustedCountdownTicks - 1) + GetTimeBetweenAngles(floor0)
        //   listFloors[0].entryTime = 0; listFloors[1].entryTime = num;
        // 第 1 格的 entryTime 必须包含倒计时拍数，否则整条时间轴会提前 (countdownTicks - 1) 拍。
        // Official: floor 1's entryTime includes (adjustedCountdownTicks - 1) beats of countdown;
        // omitting it shifts the whole timeline earlier by that many beats.
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

        if (countdownTicks < 1)
        {
            countdownTicks = 1;
        }

        double crotchet = 60.0 / bpm;

        var first = floors[0];
        double time = crotchet * (countdownTicks - 1)
                      + GetTimeBetweenAngles(first.EntryAngle, first.ExitAngle, first.Speed, bpm, !first.IsCCW);
        floors[0].EntryTime = 0.0;
        floors[1].EntryTime = time;

        for (int i = 1; i < floors.Length - 1; i++)
        {
            var f = floors[i];
            double num3 = GetInverseAnglePerBeatMultiplanet(f.NumPlanets) * (f.IsCCW ? -1 : 1);
            if (f.MidSpin)
            {
                num3 = 0.0;
            }

            if (floors[i - 1].MidSpin && f.NumPlanets > 2)
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
    private static double GetAngleLength(FloorData[] floors, int i, bool isCCW, int numPlanets, int holdLength)
    {
        var cf = floors[i];
        double num = GetInverseAnglePerBeatMultiplanet(numPlanets) * ((!isCCW) ? 1 : -1);
        if (cf.MidSpin)
        {
            num = 0.0;
        }

        if (i > 0 && floors[i - 1].MidSpin && numPlanets > 2)
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
