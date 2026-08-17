using System;
using Newtonsoft.Json;
using SharpFAI.Serialization;

namespace SharpFAI.Events;

/// <summary>
/// 轨道位置事件（移动/旋转/缩放轨道）
/// PositionTrack event (moves/rotates/scales the track)
/// </summary>
public class PositionTrack : BaseEvent
{
    /// <summary>
    /// 位置偏移 [x, y]
    /// Position offset [x, y]
    /// </summary>
    [JsonProperty("positionOffset")]
    public double[] PositionOffset;
    
    /// <summary>
    /// 相对于 [轨道索引, 瓦片参考类型]
    /// Relative to [track index, tile reference type]
    /// </summary>
    [JsonProperty("relativeTo")]
    public object[] RelativeTo;
    
    /// <summary>
    /// 旋转角度
    /// Rotation angle
    /// </summary>
    [JsonProperty("rotation")]
    public double Rotation;
    
    /// <summary>
    /// 缩放百分比
    /// Scale percentage
    /// </summary>
    [JsonProperty("scale")]
    public double Scale;
    
    /// <summary>
    /// 不透明度百分比
    /// Opacity percentage
    /// </summary>
    [JsonProperty("opacity")]
    public double Opacity;
    
    /// <summary>
    /// 仅此瓦片
    /// Just this tile
    /// </summary>
    [JsonProperty("justThisTile")]
    public bool JustThisTile;
    
    /// <summary>
    /// 仅编辑器可见
    /// Editor only
    /// </summary>
    [JsonProperty("editorOnly")]
    public bool EditorOnly;
    
    /// <summary>
    /// 粘附到地板
    /// Stick to floors
    /// </summary>
    [JsonProperty("stickToFloors")]
    public bool StickToFloors;
    
    /// <summary>
    /// 初始化一个轨道位置事件
    /// Initializes a new PositionTrack event
    /// </summary>
    /// <param name="positionOffset">位置偏移 [x, y] / Position offset [x, y]</param>
    /// <param name="relativeTo">相对于 [轨道索引, 瓦片参考类型] / Relative to [track index, tile reference type]</param>
    /// <param name="rotation">旋转角度 / Rotation angle</param>
    /// <param name="scale">缩放百分比 / Scale percentage</param>
    /// <param name="opacity">不透明度百分比 / Opacity percentage</param>
    /// <param name="justThisTile">仅此瓦片 / Just this tile</param>
    /// <param name="editorOnly">仅编辑器可见 / Editor only</param>
    /// <param name="stickToFloors">粘附到地板 / Stick to floors</param>
    public PositionTrack(double[] positionOffset = null,
        object[] relativeTo = null,
        double rotation = 0,
        double scale = 100,
        double opacity = 100,
        bool justThisTile = false,
        bool editorOnly = false,
        bool stickToFloors = true)
    {
        EventType = EventType.PositionTrack;
        PositionOffset = positionOffset ?? [0, 0];
        RelativeTo = relativeTo ?? [0, "ThisTile"];
        Rotation = rotation;
        Scale = scale;
        Opacity = opacity;
        JustThisTile = justThisTile;
        EditorOnly = editorOnly;
        StickToFloors = stickToFloors;
    }
    
    /// <summary>
    /// 设置 relativeTo 的值 / Sets the relativeTo value
    /// </summary>
    /// <param name="track">轨道索引 / Track index</param>
    /// <param name="type">瓦片参考类型 / Tile reference type</param>
    public void SetRelativeTo(int track, EventEnums.TileRelativeTo type)
    {
        RelativeTo = [track, type.ToString()];
    }
    
    /// <summary>
    /// 获取轨道索引 / Gets the track index
    /// </summary>
    /// <returns>轨道索引，无效时返回 0 / Track index, or 0 if invalid</returns>
    public int GetTrack()
    {
        if (RelativeTo != null && RelativeTo.Length >= 1 && RelativeTo[0] is int track)
            return track;
        return 0;
    }
    
    /// <summary>
    /// 获取瓦片参考类型 / Gets the tile reference type
    /// </summary>
    /// <returns>瓦片参考类型，无效时返回 ThisTile / Tile reference type, or ThisTile if invalid</returns>
    public EventEnums.TileRelativeTo GetTileRelativeTo()
    {
        if (RelativeTo != null && RelativeTo.Length >= 2 && RelativeTo[1] is string typeStr)
        {
            if (Enum.TryParse<EventEnums.TileRelativeTo>(typeStr, out var result))
                return result;
        }
        return EventEnums.TileRelativeTo.ThisTile;
    }
}