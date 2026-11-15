namespace SharpFAI.Framework;

/// <summary>
/// 地板图标类型（对应官方 scrFloor.FloorIcon 的常用子集）
/// Floor icon types, a subset of the game's scrFloor.FloorIcon covering the ones the editor draws.
/// </summary>
public enum FloorIcon
{
    /// <summary>无图标 / No icon.</summary>
    None = 0,

    /// <summary>旋转，本格扫过不足半圈（红色漩涡）/ Twirl with less than half a turn (red swirl).</summary>
    TwirlShort,

    /// <summary>旋转，本格扫过半圈及以上（蓝色漩涡）/ Twirl with half a turn or more (blue swirl).</summary>
    TwirlLong,

    /// <summary>长按 / Hold (hold note).</summary>
    Hold,

    /// <summary>暂停 / Pause.</summary>
    Pause,

    /// <summary>加速（兔子）/ Speed up (rabbit).</summary>
    SpeedUp,

    /// <summary>减速（蜗牛）/ Speed down (snail).</summary>
    SpeedDown,

    /// <summary>检查点 / Checkpoint.</summary>
    Checkpoint,
}
