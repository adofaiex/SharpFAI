using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using SharpFAI.Serialization;

namespace SharpFAI.Tests;

/// <summary>
/// Loads the level a user reported as causing runaway memory growth. A small chart that grows past a
/// few hundred megabytes means the reader is looping or duplicating data, not that the file is big.
/// </summary>
[TestFixture]
public class TestIdplmaLevel
{
    private const string LevelPath = @"c:\Users\StArray\Downloads\IDPLMA\level.adofai";

    private static void Report(string stage, long baselineBytes, Stopwatch sw)
    {
        sw.Stop();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long managed = GC.GetTotalMemory(false);
        long working = Process.GetCurrentProcess().WorkingSet64;

        TestContext.Out.WriteLine(
            $"  {stage,-34} {sw.Elapsed.TotalMilliseconds,9:N0} ms   " +
            $"managed={(managed - baselineBytes) / 1048576.0,8:N1} MB   " +
            $"working={working / 1048576.0,8:N1} MB");

        sw.Restart();
    }

    [Test]
    public void LoadsWithoutRunawayMemory()
    {
        if (!File.Exists(LevelPath))
        {
            Assert.Ignore($"Level not found: {LevelPath}");
            return;
        }

        long fileSize = new FileInfo(LevelPath).Length;
        TestContext.Out.WriteLine($"  file: {fileSize / 1024.0:N1} KB");

        GC.Collect();
        long baseline = GC.GetTotalMemory(true);
        Stopwatch sw = Stopwatch.StartNew();

        Level level = new(LevelPath);
        Report("new Level(path)", baseline, sw);

        int angles = level.angles.Count;
        int actions = level.actions.Count;
        int decorations = level.decorations?.Count ?? 0;
        Report("read counts", baseline, sw);

        TestContext.Out.WriteLine(
            $"  angles={angles:N0}  actions={actions:N0}  decorations={decorations:N0}");

        int bound = level.deserializedEvents?.Count ?? 0;
        Report("deserializedEvents", baseline, sw);

        TestContext.Out.WriteLine($"  bound events={bound:N0}");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long managed = GC.GetTotalMemory(true);
        long working = Process.GetCurrentProcess().WorkingSet64;

        TestContext.Out.WriteLine(
            $"  FINAL managed={managed / 1048576.0:N1} MB  working={working / 1048576.0:N1} MB");

        Assert.That(angles, Is.GreaterThan(0));
        Assert.That(managed, Is.LessThan(512L * 1024 * 1024), "managed heap exceeded 512 MB");
    }
}
