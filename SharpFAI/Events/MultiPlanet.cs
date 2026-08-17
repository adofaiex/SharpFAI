using Newtonsoft.Json;
using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// 多行星事件（改变行星数量）
/// MultiPlanet event (changes the planet count)
/// </summary>
public class MultiPlanet : BaseEvent
{
    /// <summary>
    /// 行星数量
    /// Number of planets
    /// </summary>
    [JsonProperty("planets")]
    public EventEnums.PlanetCount Planets;
    
    /// <summary>
    /// 初始化一个多行星事件
    /// Initializes a new MultiPlanet event
    /// </summary>
    /// <param name="planets">行星数量 / Planet count</param>
    public MultiPlanet(EventEnums.PlanetCount planets = EventEnums.PlanetCount.TwoPlanets)
    {
        EventType = EventType.MultiPlanet;
        Planets = planets;
    }
}