using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;
using SharpFAI.Serialization;

namespace SharpFAI.Util;

/// <summary>Core timing events prepared for playback. Twirls only need presence and parity per floor.</summary>
public sealed class FloorCoreEvents
{
    internal BitArray HasTwirl { get; }
    internal BitArray OddTwirls { get; }
    internal List<BaseEvent> Others { get; } = [];

    private FloorCoreEvents(int count)
    {
        HasTwirl = new BitArray(count);
        OddTwirls = new BitArray(count);
    }

    /// <summary>Read the core events in chart order, excluding disabled hold, pause and planet-count events.</summary>
    public static FloorCoreEvents Read(Level level, CancellationToken token = default) =>
        Read(level.actions, level.angles.Count + 1, token);

    /// <summary>Prepare timing events for the given number of floors, including floor zero.</summary>
    public static FloorCoreEvents Read(JArray actions, int floorCount, CancellationToken token = default) =>
        Read(actions, floorCount, token, null);

    /// <summary>Read the committed actions followed by pending appends without copying the chart.</summary>
    public static FloorCoreEvents Read(JArray actions, int floorCount, CancellationToken token,
        IReadOnlyList<JObject>? appendedActions)
    {
        var result = new FloorCoreEvents(floorCount);
        int committedCount = actions.Count;
        int count = committedCount + (appendedActions?.Count ?? 0);
        for (int i = 0; i < count; i++)
        {
            if ((i & 4095) == 0) token.ThrowIfCancellationRequested();
            JToken item = i < committedCount ? actions[i] : appendedActions![i - committedCount];
            if (TwirlEventToken.TryGetFloor(item, out int packedFloor))
            {
                result.AddTwirl(packedFloor);
                continue;
            }
            if (item is not JObject action) continue;
            switch ((string?)action["eventType"])
            {
                case "Twirl":
                    result.AddTwirl((int?)action["floor"] ?? 0);
                    break;
                case "Hold" or "Pause" or "MultiPlanet":
                    string? active = (string?)action["active"];
                    if (string.Equals(active, "false", System.StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(active, "disabled", System.StringComparison.OrdinalIgnoreCase))
                        break;
                    goto case "SetSpeed";
                case "SetSpeed":
                case "FreeRoam":
                    result.Others.Add(EventJsonConverter.ConvertEvent(action));
                    break;
            }
        }
        return result;
    }

    private void AddTwirl(int floor)
    {
        if ((uint)floor >= (uint)HasTwirl.Length) return;
        HasTwirl[floor] = true;
        OddTwirls[floor] = !OddTwirls[floor];
    }
}
