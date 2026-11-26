using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SharpFAI.Serialization;

/// <summary>A compact token for a Twirl that contains only its event type and floor.</summary>
public static class TwirlEventToken
{
    private readonly struct Anchor
    {
        public int Floor { get; }

        public Anchor(int floor) => Floor = floor;

        public override string ToString() =>
            "{\"floor\":" + Floor.ToString(CultureInfo.InvariantCulture) + ",\"eventType\":\"Twirl\"}";
    }

    /// <summary>Create a compact token that still writes as a regular Twirl action.</summary>
    public static JRaw Create(int floor) => new(new Anchor(floor));

    /// <summary>Pack a Twirl only when the action has no other authored fields.</summary>
    public static JRaw? Pack(JToken token) =>
        token is JObject { Count: 2 } action &&
        action["eventType"] is JValue { Value: "Twirl" } &&
        action["floor"] is JValue { Type: JTokenType.Integer, Value: int floor }
        ? Create(floor) : null;

    /// <summary>Update an existing compact floor anchor while retaining its place in the action list.</summary>
    public static void Move(JRaw token, int floor) => token.Value = new Anchor(floor);

    /// <summary>Materialize a regular editable Twirl action.</summary>
    public static JObject Materialize(int floor) => new() { ["floor"] = floor, ["eventType"] = "Twirl" };

    /// <summary>Read the floor from a compact token.</summary>
    public static bool TryGetFloor(JToken? token, out int floor)
    {
        if (token is JRaw { Value: Anchor anchor })
        {
            floor = anchor.Floor;
            return true;
        }
        floor = 0;
        return false;
    }
}
