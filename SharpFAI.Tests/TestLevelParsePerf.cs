using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SharpFAI.Serialization;
using SimpleJson = SharpFAI.Serialization.SimpleJSON;

namespace SharpFAI.Tests;

/// <summary>
/// Baseline profiling for a very large level: measures each stage of the load pipeline separately so
/// the hot spots are measured rather than guessed.
/// </summary>
[TestFixture]
public class TestLevelParsePerf
{
    private const string BigLevelPath =
        @"c:\Users\StArray\Downloads\Arche - かめりあ (Camellia)\[𝐃𝐎 𝐍𝐎𝐓 𝐎𝐏𝐄𝐍 𝐓𝐇𝐈𝐒 𝐀𝐓 𝐇𝐎𝐌𝐄] FULL.adofai";

    private static void Step(string name, Stopwatch sw)
    {
        sw.Stop();
        TestContext.Out.WriteLine($"  {name,-42} {sw.Elapsed.TotalMilliseconds,10:N0} ms");
        sw.Restart();
    }

    [Test]
    public void ProfileRawFile()
    {
        if (!File.Exists(BigLevelPath))
        {
            Assert.Ignore($"Level not found: {BigLevelPath}");
            return;
        }

        Stopwatch total = Stopwatch.StartNew();
        Stopwatch sw = Stopwatch.StartNew();

        string text = File.ReadAllText(BigLevelPath);
        Step("A. File.ReadAllText", sw);

        var dict = SimpleJson.Deserialize(text);
        Step("B. SimpleJSON.Parse -> Dictionary", sw);
        TestContext.Out.WriteLine($"     keys = {dict?.Count ?? 0}");

        var root = JsonValueConverter.ToJObject(dict!);
        Step("C. Dictionary -> JObject (direct)", sw);

        JArray actions = (JArray)root["actions"]!;
        JArray angleData = (JArray)root["angleData"]!;
        JArray? decorations = root["decorations"] as JArray;
        Step("D. index arrays", sw);
        TestContext.Out.WriteLine(
            $"     actions={actions.Count:N0}  angles={angleData.Count:N0}  decorations={decorations?.Count ?? 0:N0}");

        var angles = angleData.ToObject<System.Collections.Generic.List<double>>();
        Step("E. angleData.ToObject<List<double>>", sw);

        string actionsText = actions.ToString();
        Step("F. actions.ToString()", sw);

        var events = JsonConvert.DeserializeObject<BaseEvent[]>(
            actionsText, EventJsonConverter.GetJsonSettings());
        Step("G. DeserializeObject<BaseEvent[]> from text", sw);
        TestContext.Out.WriteLine($"     events = {events?.Length ?? 0:N0}");

        // Compare against binding straight from the token, which skips the 34 MB intermediate text.
        var events2 = new System.Collections.Generic.List<BaseEvent>();
        using (var reader = actions.CreateReader())
        {
            var ser = JsonSerializer.Create(EventJsonConverter.GetJsonSettings());
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject
                    && ser.Deserialize<BaseEvent>(reader) is { } item)
                {
                    events2.Add(item);
                }
            }
        }
        Step("H. bind events from JToken (no text)", sw);
        TestContext.Out.WriteLine($"     events = {events2.Count:N0}");

        total.Stop();
        TestContext.Out.WriteLine($"  {"TOTAL",-42} {total.Elapsed.TotalMilliseconds,10:N0} ms");
    }

    [Test]
    public void ProfileLevelConstructor()
    {
        if (!File.Exists(BigLevelPath))
        {
            Assert.Ignore($"Level not found: {BigLevelPath}");
            return;
        }

        // Warm up the serializers so the measurement reflects steady state, not first-call JIT.
        Level warm = new(BigLevelPath);
        _ = warm.actions.Count;

        for (int run = 1; run <= 3; run++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Level level = new(BigLevelPath);
            sw.Stop();

            TestContext.Out.WriteLine(
                $"  run {run}: new Level(path) {sw.Elapsed.TotalMilliseconds,8:N0} ms  " +
                $"(angles={level.angles.Count:N0}, actions={level.actions.Count:N0}, " +
                $"eventsBound={level.EventsDeserialized})");
        }

        // Now include the event binding, which is lazy.
        Stopwatch bind = Stopwatch.StartNew();
        Level full = new(BigLevelPath);
        int eventCount = full.deserializedEvents?.Count ?? 0;
        bind.Stop();
        TestContext.Out.WriteLine(
            $"  with events:              {bind.Elapsed.TotalMilliseconds,8:N0} ms  (events={eventCount:N0})");
    }
}
