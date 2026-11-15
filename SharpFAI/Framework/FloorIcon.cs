namespace SharpFAI.Framework;

/// <summary>
/// 地板图标类型，对应官方 <c>scrFloor.FloorIcon</c>。
/// Floor icon types, mirroring the game's <c>scrFloor.FloorIcon</c>.
/// </summary>
/// <remarks>
/// 官方图标由 <c>scnGame.CreateFloor</c> 按事件优先级选出。这里保留官方命名，方便逐项对照。
/// The game picks these in <c>scnGame.CreateFloor</c> by event priority. The official names are kept
/// so each one can be checked off against the source.
/// </remarks>
public enum FloorIcon
{
    /// <summary>无图标 / No icon.</summary>
    None = 0,

    /// <summary>旋转（漩涡）/ Twirl (swirl).</summary>
    Swirl,

    /// <summary>加速一倍（兔子）/ Speed up by 2x (rabbit).</summary>
    Rabbit,

    /// <summary>加速更多（双兔子）/ Speed up by 4x (double rabbit).</summary>
    DoubleRabbit,

    /// <summary>减速一半（蜗牛）/ Slow down by half (snail).</summary>
    Snail,

    /// <summary>减速更多（双蜗牛）/ Slow down by 4x (double snail).</summary>
    DoubleSnail,

    /// <summary>速度不变 / Speed unchanged.</summary>
    SameSpeed,

    /// <summary>长按，短 / Hold, short.</summary>
    HoldArrowShort,

    /// <summary>长按，长 / Hold, long.</summary>
    HoldArrowLong,

    /// <summary>检查点 / Checkpoint.</summary>
    Checkpoint,

    /// <summary>两颗行星 / Two planets.</summary>
    MultiPlanetTwo,

    /// <summary>三颗以上行星 / More than two planets.</summary>
    MultiPlanetThreeMore,

    /// <summary>行星减少 / Fewer planets.</summary>
    MultiPlanetThreeLess,

    /// <summary>仅事件标记 / Event marker only.</summary>
    Vfx,

    /// <summary>终点传送门 / End portal.</summary>
    Portal,
}
