using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// 旋转事件（反转行星旋转方向）
/// Twirl event (reverses the planets' rotation direction)
/// </summary>
public class Twirl : BaseEvent
{
    /// <summary>
    /// 初始化一个旋转事件
    /// Initializes a new Twirl event
    /// </summary>
    public Twirl()
    {
        EventType = EventType.Twirl;
    }
}
