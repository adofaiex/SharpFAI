using Newtonsoft.Json;
using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// 变速事件
/// SetSpeed event
/// </summary>
public class SetSpeed : BaseEvent
{
    /// <summary>
    /// 速度类型
    /// Speed type
    /// </summary>
    [JsonProperty("speedType")]
    public EventEnums.SpeedType SpeedType;
    
    /// <summary>
    /// 每分钟节拍数
    /// Beats per minute
    /// </summary>
    [JsonProperty("beatsPerMinute")]
    public double BeatsPerMinute;
    
    /// <summary>
    /// BPM 倍率
    /// BPM multiplier
    /// </summary>
    [JsonProperty("bpmMultiplier")]
    public double BpmMultiplier;
    
    /// <summary>
    /// 角度偏移
    /// Angle offset
    /// </summary>
    [JsonProperty("angleOffset")]
    public double AngleOffset;
    
    /// <summary>
    /// 初始化一个变速事件
    /// Initializes a new SetSpeed event
    /// </summary>
    /// <param name="speedType">速度类型 / Speed type</param>
    /// <param name="beatsPerMinute">每分钟节拍数 / Beats per minute</param>
    /// <param name="bpmMultiplier">BPM 倍率 / BPM multiplier</param>
    /// <param name="angleOffset">角度偏移 / Angle offset</param>
    public SetSpeed(EventEnums.SpeedType speedType = EventEnums.SpeedType.Bpm,
        double beatsPerMinute = 100,
        double bpmMultiplier = 1,
        double angleOffset = 0)
    {
        EventType = EventType.SetSpeed;
        SpeedType = speedType;
        BeatsPerMinute = beatsPerMinute;
        BpmMultiplier = bpmMultiplier;
        AngleOffset = angleOffset;
    }
}