using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace SharpFAI.Serialization;
#pragma warning disable CS1591
public sealed class LevelSetting : JObject
{
    public LevelSetting(string item1, object item2) 
    {
        Add(item1, FromObject(item2));
    }

    /// <summary>
    /// 该预设项的名称（唯一一个属性名）。
    /// The preset's key, i.e. its only property name.
    /// </summary>
    public string Key => ((JProperty)First!).Name;
}

public static class PresetSettings
{
    // 基础信息设置
    public static LevelSetting Version => new LevelSetting("version", 15);
    public static LevelSetting Artist => new LevelSetting("artist", "");
    public static LevelSetting SpecialArtistType => new LevelSetting("specialArtistType", "None");
    public static LevelSetting ArtistPermission => new LevelSetting("artistPermission", "");
    public static LevelSetting Song => new LevelSetting("song", "");
    public static LevelSetting Author => new LevelSetting("author", "Created Level by SharpFAI");
    public static LevelSetting SeparateCountdownTime => new LevelSetting("separateCountdownTime", true);
    public static LevelSetting PreviewImage => new LevelSetting("previewImage", "");
    public static LevelSetting PreviewIcon => new LevelSetting("previewIcon", "");
    public static LevelSetting PreviewIconColor => new LevelSetting("previewIconColor", "003f52");
    public static LevelSetting PreviewSongStart => new LevelSetting("previewSongStart", 0);
    public static LevelSetting PreviewSongDuration => new LevelSetting("previewSongDuration", 10);
    public static LevelSetting SeizureWarning => new LevelSetting("seizureWarning", false);
    public static LevelSetting LevelDesc => new LevelSetting("levelDesc", "");
    public static LevelSetting LevelTags => new LevelSetting("levelTags", "");
    public static LevelSetting ArtistLinks => new LevelSetting("artistLinks", "");
    public static LevelSetting SpeedTrialAim => new LevelSetting("speedTrialAim", 0);
    public static LevelSetting Difficulty => new LevelSetting("difficulty", 1);
    public static LevelSetting RequiredMods => new LevelSetting("requiredMods", new object[0]);
    
    // 音频设置
    public static LevelSetting SongFilename => new LevelSetting("songFilename", "");
    public static LevelSetting Bpm => new LevelSetting("bpm", 100);
    public static LevelSetting Volume => new LevelSetting("volume", 100);
    public static LevelSetting Offset => new LevelSetting("offset", 0);
    public static LevelSetting Pitch => new LevelSetting("pitch", 100);
    public static LevelSetting Hitsound => new LevelSetting("hitsound", "Kick");
    public static LevelSetting HitsoundVolume => new LevelSetting("hitsoundVolume", 100);
    public static LevelSetting CountdownTicks => new LevelSetting("countdownTicks", 4);
    public static LevelSetting SongURL => new LevelSetting("songURL", "");
    
    // 轨道设置
    public static LevelSetting TileShape => new LevelSetting("tileShape", "Long");
    public static LevelSetting TrackColorType => new LevelSetting("trackColorType", "Single");
    public static LevelSetting TrackColor => new LevelSetting("trackColor", "debb7b");
    public static LevelSetting SecondaryTrackColor => new LevelSetting("secondaryTrackColor", "ffffff");
    public static LevelSetting TrackColorAnimDuration => new LevelSetting("trackColorAnimDuration", 2);
    public static LevelSetting TrackColorPulse => new LevelSetting("trackColorPulse", "None");
    public static LevelSetting TrackPulseLength => new LevelSetting("trackPulseLength", 10);
    public static LevelSetting TrackStyle => new LevelSetting("trackStyle", "Standard");
    public static LevelSetting TrackTexture => new LevelSetting("trackTexture", "");
    public static LevelSetting TrackTextureScale => new LevelSetting("trackTextureScale", 1);
    public static LevelSetting TrackGlowIntensity => new LevelSetting("trackGlowIntensity", 100);
    public static LevelSetting TrackAnimation => new LevelSetting("trackAnimation", "None");
    public static LevelSetting BeatsAhead => new LevelSetting("beatsAhead", 3);
    public static LevelSetting TrackDisappearAnimation => new LevelSetting("trackDisappearAnimation", "None");
    public static LevelSetting BeatsBehind => new LevelSetting("beatsBehind", 4);
    
    // 背景设置
    public static LevelSetting BackgroundColor => new LevelSetting("backgroundColor", "000000");
    public static LevelSetting ShowDefaultBGIfNoImage => new LevelSetting("showDefaultBGIfNoImage", true);
    public static LevelSetting ShowDefaultBGTile => new LevelSetting("showDefaultBGTile", true);
    public static LevelSetting DefaultBGTileColor => new LevelSetting("defaultBGTileColor", "101121");
    public static LevelSetting DefaultBGShapeType => new LevelSetting("defaultBGShapeType", "Default");
    public static LevelSetting DefaultBGShapeColor => new LevelSetting("defaultBGShapeColor", "ffffff");
    public static LevelSetting BgImage => new LevelSetting("bgImage", "");
    public static LevelSetting BgImageColor => new LevelSetting("bgImageColor", "ffffff");
    public static LevelSetting Parallax => new LevelSetting("parallax", new int[] { 100, 100 });
    public static LevelSetting BgDisplayMode => new LevelSetting("bgDisplayMode", "FitToScreen");
    public static LevelSetting ImageSmoothing => new LevelSetting("imageSmoothing", true);
    public static LevelSetting LockRot => new LevelSetting("lockRot", false);
    public static LevelSetting LoopBG => new LevelSetting("loopBG", false);
    public static LevelSetting ScalingRatio => new LevelSetting("scalingRatio", 100);
    
    // 摄像机设置
    public static LevelSetting RelativeTo => new LevelSetting("relativeTo", "Player");
    public static LevelSetting Position => new LevelSetting("position", new int[] { 0, 0 });
    public static LevelSetting Rotation => new LevelSetting("rotation", 0);
    public static LevelSetting Zoom => new LevelSetting("zoom", 100);
    public static LevelSetting PulseOnFloor => new LevelSetting("pulseOnFloor", true);
    
    // 视频设置
    public static LevelSetting BgVideo => new LevelSetting("bgVideo", "");
    public static LevelSetting LoopVideo => new LevelSetting("loopVideo", false);
    public static LevelSetting VidOffset => new LevelSetting("vidOffset", 0);
    
    // 游戏性设置
    public static LevelSetting FloorIconOutlines => new LevelSetting("floorIconOutlines", false);
    public static LevelSetting StickToFloors => new LevelSetting("stickToFloors", true);
    public static LevelSetting PlanetEase => new LevelSetting("planetEase", "Linear");
    public static LevelSetting PlanetEaseParts => new LevelSetting("planetEaseParts", 1);
    public static LevelSetting PlanetEasePartBehavior => new LevelSetting("planetEasePartBehavior", "Mirror");
    
    // 文本设置
    public static LevelSetting DefaultTextColor => new LevelSetting("defaultTextColor", "ffffff");
    public static LevelSetting DefaultTextShadowColor => new LevelSetting("defaultTextShadowColor", "00000050");
    public static LevelSetting CongratsText => new LevelSetting("congratsText", "");
    public static LevelSetting PerfectText => new LevelSetting("perfectText", "");
    
    // 兼容性设置
    public static LevelSetting LegacyFlash => new LevelSetting("legacyFlash", false);
    public static LevelSetting LegacyCamRelativeTo => new LevelSetting("legacyCamRelativeTo", false);
    public static LevelSetting LegacySpriteTiles => new LevelSetting("legacySpriteTiles", false);
    public static LevelSetting LegacyTween => new LevelSetting("legacyTween", false);
    public static LevelSetting DisableV15Features => new LevelSetting("disableV15Features", true);
    
    // 预设组合
    public static List<LevelSetting> DefaultSettings =>
    [
        Version, Artist, SpecialArtistType, ArtistPermission, Song, Author,
        SeparateCountdownTime, PreviewImage, PreviewIcon, PreviewIconColor,
        PreviewSongStart, PreviewSongDuration, SeizureWarning, LevelDesc,
        LevelTags, ArtistLinks, SpeedTrialAim, Difficulty, RequiredMods,
        SongFilename, Bpm, Volume, Offset, Pitch, Hitsound, HitsoundVolume,
        CountdownTicks, SongURL, TileShape, TrackColorType, TrackColor,
        SecondaryTrackColor, TrackColorAnimDuration, TrackColorPulse,
        TrackPulseLength, TrackStyle, TrackTexture, TrackTextureScale,
        TrackGlowIntensity, TrackAnimation, BeatsAhead, TrackDisappearAnimation,
        BeatsBehind, BackgroundColor, ShowDefaultBGIfNoImage, ShowDefaultBGTile,
        DefaultBGTileColor, DefaultBGShapeType, DefaultBGShapeColor, BgImage,
        BgImageColor, Parallax, BgDisplayMode, ImageSmoothing, LockRot,
        LoopBG, ScalingRatio, RelativeTo, Position, Rotation, Zoom, PulseOnFloor,
        BgVideo, LoopVideo, VidOffset, FloorIconOutlines, StickToFloors,
        PlanetEase, PlanetEaseParts, PlanetEasePartBehavior, DefaultTextColor,
        DefaultTextShadowColor, CongratsText, PerfectText, LegacyFlash,
        LegacyCamRelativeTo, LegacySpriteTiles, LegacyTween, DisableV15Features
    ];

    /// <summary>
    /// 把一组预设写入目标 <see cref="JObject"/>；已存在的键会被覆盖。
    /// Copy a set of presets into a target settings object, overwriting keys that already exist.
    /// </summary>
    /// <param name="target">接收设置的对象 / Settings object to write into.</param>
    /// <param name="presets">要写入的预设 / Presets to apply.</param>
    /// <returns><paramref name="target"/>，便于链式调用 / The same instance, for chaining.</returns>
    public static JObject Apply(JObject target, params LevelSetting[] presets)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (presets is null)
        {
            throw new ArgumentNullException(nameof(presets));
        }

        foreach (LevelSetting preset in presets)
        {
            foreach (JProperty property in preset.Properties())
            {
                target[property.Name] = property.Value;
            }
        }

        return target;
    }

    /// <summary>
    /// 把全部默认预设写入目标 <see cref="JObject"/>。
    /// Copy every default preset into a target settings object.
    /// </summary>
    /// <param name="target">接收设置的对象 / Settings object to write into.</param>
    /// <returns><paramref name="target"/>，便于链式调用 / The same instance, for chaining.</returns>
    public static JObject ApplyDefaults(JObject target) => Apply(target, [.. DefaultSettings]);
}