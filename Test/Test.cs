using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using SharpFAI.Events;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Test;
#pragma warning disable CS1591
#if DEBUG
public static class Test
{
    public static void Main2()
    {
        Level level2 = new Level(pathToLevel: @"D:\ADOFAILevels\THE STAR SYSTEM'S CALLING\lead.adofai");
        Console.WriteLine(level2.GetNoteTimes().Count);
         Level level = Level.CreateNewLevel();
         Console.WriteLine(level.GetNoteTimes().Count);
        level2.AddSegmentsAsFloor(1.6666666666666666,712);
        level2.Save();
    }

    extension(Level level)
    {
        public void SmoothScale(int start, int end, double startScale = 100d, double endScale = 200d)
        {
            // 参数验证
            if (level == null) throw new ArgumentNullException(nameof(level));
            if (start >= end) throw new ArgumentException("起始楼层必须小于结束楼层");
    
            int count = end - start + 1; // 包含起始和结束楼层
    
            // 如果只有一层，直接使用起始缩放值
            if (count == 1)
            {
                level.AddEvent(start, EventType.ScaleRadius, new JObject()
                {
                    ["scale"] = startScale
                });
                return;
            }
    
            // 计算每层之间的缩放增量
            double scaleRange = endScale - startScale;
            double scalePerFloor = scaleRange / (count - 1);
    
            for (int i = 0; i < count; i++)
            {
                int currentFloor = start + i;
                double currentScale = startScale + (i * scalePerFloor);
        
                // 根据您的需求选择其中一种方式，或者都保留
        
                // 方式1：使用 PositionTrack（如果这是您自定义的类型的
                level.AddEvent(new PositionTrack()
                {
                    Scale = currentScale,
                    Floor = currentFloor
                });
        
                // 方式2：添加缩放半径事件
                level.AddEvent(currentFloor, EventType.ScaleRadius, new JObject()
                {
                    ["scale"] = currentScale
                });
            }
            // 方式1：使用 PositionTrack（如果这是您自定义的类型）
            level.AddEvent(new PositionTrack()
            {
                Scale = 100,
                Floor = end + 1
            });
        
            // 方式2：添加缩放半径事件
            level.AddEvent(end + 1, EventType.ScaleRadius, new JObject()
            {
                ["scale"] = 100
            });
        }

        public void AddSegmentsAsFloor(double multiplier = 2, int count = 32)
        {
            double angle = 180 - 180 / multiplier;
            for (int i = 0; i < count; i++)
            {
                level.angleData.Add(angle * i) ;
            }
        }

        public void ToLinerFloors()
        {
            var bpmList = new List<double>();
            for (int i = 0; i < level.GetNoteTimes().Count -1; i++)
            {
                if (level.GetNoteTimes()[i + 1] - level.GetNoteTimes()[i] == 0) continue;
                bpmList.Add(60000 / (level.GetNoteTimes()[i + 1] - level.GetNoteTimes()[i]));
            }

            level.actions.Clear();
            level.angleData.Clear();
            level.angleData.Add(0);
            for (int i = 0; i < bpmList.Count - 1; i++)
            {
                level.angleData.Add(0);
                level.AddEvent(new SetSpeed()
                {
                    Floor = i + 1,
                    BeatsPerMinute = bpmList[i],
                    SpeedType = EventEnums.SpeedType.Bpm
                });
            }
            level.Save();
        }

        public void GenerateGlide(int startFloor, double baseBPM, double startFrequency, double endFrequency, double duration)
        {
            Console.WriteLine($"GenerateGlide: 开始执行 - startFloor={startFloor}, baseBPM={baseBPM}, startFreq={startFrequency}, endFreq={endFrequency}, duration={duration}");
        
            double current = 0;
            List<double> noteTimes = [];
        
            // 记录每次循环的详细信息
            int iteration = 0;
            while (current < duration - 2 / endFrequency)
            {
                double progress = current / duration;  // 从0到接近1
                double currentCents = GetGlidePitch(startFrequency, endFrequency, progress);
                current += 1 / currentCents;
                noteTimes.Add(currentCents * 60);
                Console.WriteLine($"迭代 {iteration}: progress={progress:F3}, currentCents={currentCents:F3}, current累计={current:F3}");
                iteration++;
            
                // 防止无限循环
                if (iteration > 1000) {
                    Console.WriteLine("警告：循环迭代次数过多，强制退出");
                    break;
                }
            }

            // 计算剩余时间对应的BPM
            double remainingTime = duration - current;
            if (remainingTime > 0) {
                // 使用结束频率来计算剩余时间的BPM
                double finalBPM = endFrequency * 60;  // 将频率转换为BPM
                noteTimes.Add(finalBPM);
                Console.WriteLine($"剩余时间 {remainingTime:F3}, 使用结束频率计算BPM: {finalBPM:F3}");
            }
        
            Console.WriteLine($"总共生成 {noteTimes.Count} 个BPM值");
        
            // 添加角度数据
            for (int i = 0; i < noteTimes.Count; i++)
            {
                double bpmRatio = noteTimes[i] / baseBPM;
                Console.WriteLine($"{i}: BPM={noteTimes[i]:F3}, baseBPM={baseBPM}, 比率={bpmRatio:F3}");
            
                // 使用AddSegmentsAsFloor的逻辑计算multiplier
                double multiplier = 1.0 / bpmRatio;  // 这里是关键：BPM比率的倒数作为multiplier
                double angle = 180 - 180 * multiplier;  // 等价于 180 - 180 / (1/bpmRatio)
            
                // 确保 angleData 有足够的元素
                if (level.angleData.Count <= startFloor + i)
                {
                    // 扩展 angleData 数组
                    while (level.angleData.Count <= startFloor + i)
                    {
                        level.angleData.Add(0);
                    }
                }
            
                // 设置当前地板的角度，使用累积方式
                if (i == 0)
                {
                    level.angleData[startFloor + i] = angle;
                }
                else
                {
                    level.angleData[startFloor + i] = level.angleData[startFloor + i - 1].ToObject<double>() + angle;
                }
            }
        
            // 只在起始方块添加一个SetSpeed事件
            level.AddEvent(new SetSpeed()
            {
                Floor = startFloor,
                BeatsPerMinute = baseBPM,
                SpeedType = EventEnums.SpeedType.Bpm
            });
        
            Console.WriteLine($"GenerateGlide: 完成执行 - 在Floor {startFloor}添加了SetSpeed事件，BPM={baseBPM}");
        }
    }

    internal static double GetGlidePitch(double startFreq, double endFreq, double percent)
    {
        double deltaCents = 1200 * Math.Log(endFreq / startFreq, 2);
        double currentCents = deltaCents * percent;
        return startFreq * Math.Pow(2, currentCents / 1200);
    }
}
#endif