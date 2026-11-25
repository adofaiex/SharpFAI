using NUnit.Framework;
using Newtonsoft.Json.Linq;
using SharpFAI.Events;
using SharpFAI.Serialization;
using SimpleJson = SharpFAI.Serialization.SimpleJSON;

namespace SharpFAI.Tests;

/// <summary>
/// Round-trip and robustness checks for the JSON reader. The reader is hand-written, so a defect in
/// string or structure handling shows up as a hang or an out-of-memory instead of an exception, which
/// is exactly the failure a user reported on another machine.
/// </summary>
[TestFixture]
public class TestJsonReaderRobustness
{
    [Test]
    public void EscapedStringsParseCorrectly()
    {
        const string json = """
            {"a":"quote\"inside","b":"back\\slash","c":"tab\there","d":"newline\nhere","e":"unicode\u0041BC","f":"slash\/ok"}
            """;

        JObject obj = SimpleJson.DeserializeToken(json);

        Assert.That(obj["a"]!.Value<string>(), Is.EqualTo("quote\"inside"));
        Assert.That(obj["b"]!.Value<string>(), Is.EqualTo("back\\slash"));
        Assert.That(obj["c"]!.Value<string>(), Is.EqualTo("tab\there"));
        Assert.That(obj["d"]!.Value<string>(), Is.EqualTo("newline\nhere"));
        Assert.That(obj["e"]!.Value<string>(), Is.EqualTo("unicodeABC"));
        Assert.That(obj["f"]!.Value<string>(), Is.EqualTo("slash/ok"));
    }

    [Test]
    public void TrailingBackslashDoesNotHang()
    {
        // A backslash immediately before the closing quote is malformed but must terminate.
        const string json = """{"a":"ends with backslash\\"}""";

        JObject obj = SimpleJson.DeserializeToken(json);
        TestContext.Out.WriteLine($"parsed: {obj}");
    }

    [Test]
    public void MissingCommaBetweenMembersStillParses()
    {
        const string json = """{"a":[1,2]"b":3}""";
        JObject obj = SimpleJson.DeserializeToken(json);

        TestContext.Out.WriteLine($"parsed: {obj}");
        Assert.That(obj["b"]?.Value<int>(), Is.EqualTo(3).Or.Null);
    }

    /// <summary>
    /// A literal (null/true/false) inside an array used to be read twice: once to classify the token
    /// and again inside ParseJValue. The parser then lost sync, hit the following ':' and appended
    /// null forever. A 1 MB chart grew to more than 8 GB on a machine that contained
    /// "position": [null, null], so this shape has to stay cheap and finite.
    /// </summary>
    [Test]
    public void LiteralArrayElementsAreNotReRead()
    {
        const string json = """
            {"a":[null,null],"b":[true,false],"c":[null,1],"d":[[null,null],[1,2]],"e":[[null,null]]}
            """;

        JObject root = SimpleJson.DeserializeToken(json);

        Assert.That(((JArray)root["a"]!).Count, Is.EqualTo(2));
        Assert.That(((JArray)root["b"]!).Count, Is.EqualTo(2));
        Assert.That(((JArray)root["c"]!).Count, Is.EqualTo(2));
        Assert.That(((JArray)root["d"]!).Count, Is.EqualTo(2));
        Assert.That(((JArray)root["e"]!)[0], Is.InstanceOf<JArray>());
        Assert.That(((JArray)((JArray)root["e"]!)[0]!).Count, Is.EqualTo(2));
    }

    [Test]
    public void NullPositionIsToleratedByMoveCamera()
    {
        const string json = """
            {"floor":29344,"eventType":"MoveCamera","duration":0,"relativeTo":"Player","position":[null,null],"rotation":0,"zoom":450,"angleOffset":0,"ease":"Linear","eventTag":""}
            """;

        BaseEvent bound = EventJsonConverter.Deserialize<BaseEvent>(json)!;

        Assert.That(bound, Is.InstanceOf<MoveCamera>());
        Assert.That(((MoveCamera)bound).Position, Has.Length.EqualTo(2));
    }

}
