using Newtonsoft.Json;
using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// Hold（长按）事件
/// Hold event
/// </summary>
public class Hold : BaseEvent
{
    /// <summary>
    /// 持续时间
    /// Duration in seconds
    /// </summary>
    [JsonProperty("duration")] 
    public double Duration;

    /// <summary>
    /// 距离
    /// Distance multiplier (percentage)
    /// </summary>
    [JsonProperty("distanceMultiplier")] 
    public int DistanceMultiplier;
    
    /// <summary>
    /// 是否播放落地动画
    /// Whether to play landing animation
    /// </summary>
    [JsonProperty("landingAnimation")]
    public bool LandingAnimation;
    
    /// <summary>
    /// 初始化一个 Hold 事件
    /// Initializes a new Hold event
    /// </summary>
    /// <param name="duration">持续时间 / Duration</param>
    /// <param name="distanceMultiplier">距离倍率（百分比）/ Distance multiplier (percentage)</param>
    /// <param name="landingAnimation">是否播放落地动画 / Whether to play landing animation</param>
    public Hold(double duration = 0,
        int distanceMultiplier = 100,
        bool landingAnimation = false)
    {
        EventType = EventType.Hold;
        Duration = duration;
        DistanceMultiplier = distanceMultiplier;
        LandingAnimation = landingAnimation;
    }
}