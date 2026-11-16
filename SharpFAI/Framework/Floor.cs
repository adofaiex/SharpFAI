using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using SharpFAI.Serialization;
using SharpFAI.Util;

namespace SharpFAI.Framework;

/// <summary>
/// One floor tile: heading, position and the per-tile values a renderer needs.
/// The mesh itself is built by the consumer; this type carries data only.
/// </summary>
public class Floor
{
    /// <summary>
    /// Default length of the floor tile / 地板瓦片的默认长度
    /// </summary>
    public static float length = 75;
    
    /// <summary>
    /// Default width of the floor tile / 地板瓦片的默认宽度
    /// 官方 <c>scrFloor.LongDimensions</c> = (0.75, 0.4125)，Standard 样式在
    /// <c>SetTrackStyle</c> 的 default 分支里会把它重置回这个值。
    /// The game's `scrFloor.LongDimensions` is (0.75, 0.4125) and the Standard branch of
    /// `SetTrackStyle` resets the mesh back to exactly these dimensions.
    /// </summary>
    public static float width = 41.25f;
    
    /// <summary>
    /// Outline thickness for the floor border / 地板边框的轮廓厚度
    /// </summary>
    public const float outline = 2f;
    
    /// <summary>
    /// Entry angle of the floor in degrees / 地板的入口角度（度）
    /// </summary>
    public readonly double entryAngle;
    
    /// <summary>
    /// Exit angle of the floor in degrees / 地板的出口角度（度）
    /// </summary>
    public readonly double exitAngle;
    
    /// <summary>
    /// Current angle of the floor / 地板的当前角度
    /// </summary>
    public float angle = 0;
    
    /// <summary>
    /// Whether this is a midspin floor / 是否为中旋地板
    /// </summary>
    public bool isMidspin;
    
    /// <summary>
    /// Whether the rotation is clockwise / 旋转是否为顺时针
    /// </summary>
    public bool isCW = false;

    /// <summary>
    /// Reference to the previous floor / 对前一个地板的引用
    /// </summary>
    public Floor lastFloor;
    
    /// <summary>
    /// Reference to the next floor / 对下一个地板的引用
    /// </summary>
    public Floor nextFloor;

    /// <summary>
    /// BPM (beats per minute) at this floor / 此地板处的BPM（每分钟节拍数）
    /// </summary>
    public double bpm = 0;
    
    /// <summary>
    /// Entry time of this floor in milliseconds / 此地板的入口时间（毫秒）
    /// </summary>
    public double entryTime = 0;

    /// <summary>
    /// World position of the floor / 地板的世界坐标位置
    /// </summary>
    public Vector2 position;

    /// <summary>
    /// List of events on this floor / 此地板上的事件列表
    /// </summary>
    public List<BaseEvent> events = [];

    /// <summary>
    /// Icon shown on this floor / 此地板显示的图标
    /// </summary>
    public FloorIcon icon = FloorIcon.None;

    /// <summary>
    /// Whether the icon is mirrored horizontally (the game's <c>_IconFlipped</c>).
    /// 图标是否水平翻转（对应官方的 <c>_IconFlipped</c>），旋转图标按顺时针/逆时针区分。
    /// </summary>
    public bool iconFlipped;

    /// <summary>
    /// Extra rotation applied to the icon, in radians (the game's <c>_IconAngle</c>).
    /// 图标额外旋转角（弧度），对应官方的 <c>_IconAngle</c>，旋转图标据此贴合砖块朝向。
    /// </summary>
    public float iconAngle;

    /// <summary>
    /// 该砖的漩涡图标是否用红色版本。官方按本格扫过角判定：不足半圈用红色，否则蓝色。
    /// Whether the swirl icon on this tile uses the red variant. The game picks red when the swept
    /// angle is under half a turn and blue otherwise.
    /// </summary>
    public bool swirlRed;

    /// <summary>
    /// 砖块贴图的随机旋转量，取值 [0,1)，对应官方的 <c>_UV2Offset.z</c>。
    /// 着色器把它乘 π 作为贴图 UV 的旋转角，让相邻砖块的纹理朝向各不相同。
    /// The tile texture's random rotation in [0,1), matching the game's <c>_UV2Offset.z</c>. The
    /// shader multiplies it by pi to rotate the texture UVs so neighbouring tiles differ.
    /// </summary>
    public float textureRotation;

    /// <summary>
    /// 砖块贴图的随机平移量（UV 单位），对应官方 <c>_UV2Offset.xy</c>。
    /// 与 <see cref="textureRotation"/> 一起构成完整的 <c>_UV2Offset</c>，让相邻砖块的纹理真正错开。
    /// The tile texture's random UV translation, matching the game's <c>_UV2Offset.xy</c>. Together
    /// with <see cref="textureRotation"/> it forms the full <c>_UV2Offset</c>, so neighbouring tiles
    /// genuinely shift apart.
    /// </summary>
    public float textureOffsetX;

    /// <summary>砖块贴图随机平移的 Y 分量 / The Y component of the random UV translation.</summary>
    public float textureOffsetY;

    /// <summary>
    /// Index of this floor in the level / 此地板在关卡中的索引
    /// </summary>
    public int index = 0;

    /// <summary>
    /// Rendering order / 渲染顺序
    /// </summary>
    public int renderOrder = 0;
    
    /// <summary>
    /// Creates a new floor tile with specified angles and position
    /// 创建具有指定角度和位置的新地板瓦片
    /// </summary>
    /// <param name="entryAngle">Entry angle in degrees / 入口角度（度）</param>
    /// <param name="exitAngle">Exit angle in degrees / 出口角度（度）</param>
    /// <param name="position">World position of the floor / 地板的世界坐标位置</param>
    public Floor(double entryAngle, double exitAngle,Vector2 position)
    {
        this.entryAngle = entryAngle;
        this.exitAngle = exitAngle;
        this.position = position;
        isMidspin = false;
    }



    /// <summary>
    /// Returns a string representation of the floor
    /// 返回地板的字符串表示形式
    /// </summary>
    public override string ToString()
    {
        return this.AsString();
    }
}