using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// 书签事件
/// Bookmark event
/// </summary>
public class Bookmark : BaseEvent
{
    /// <summary>
    /// 初始化一个书签事件
    /// Initializes a new Bookmark event
    /// </summary>
    public Bookmark()
    {
        EventType = EventType.Bookmark;
    }
}
