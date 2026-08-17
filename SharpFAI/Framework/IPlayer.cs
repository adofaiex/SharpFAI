namespace SharpFAI.Framework;

/// <summary>
/// Player interface / 玩家接口
/// </summary>
public interface IPlayer
{
    /// <summary>
    /// Creates the player / 创建玩家
    /// </summary>
    void CreatePlayer();

    /// <summary>
    /// Updates the player / 更新玩家
    /// </summary>
    /// <param name="delta">Time delta in seconds / 时间增量（秒）</param>
    void UpdatePlayer(double delta);

    /// <summary>
    /// Renders the player / 渲染玩家
    /// </summary>
    /// <param name="delta">Time delta in seconds / 时间增量（秒）</param>
    void RenderPlayer(double delta);

    /// <summary>
    /// Moves to the next floor / 移动到下一块地板
    /// </summary>
    /// <param name="next">The next floor / 下一块地板</param>
    void MoveToNextFloor(Floor next);

    /// <summary>
    /// Starts playing / 开始游玩
    /// </summary>
    void StartPlay();

    /// <summary>
    /// Stops playing / 停止游玩
    /// </summary>
    void StopPlay();

    /// <summary>
    /// Pauses playing / 暂停游玩
    /// </summary>
    void PausePlay();

    /// <summary>
    /// Resumes playing / 恢复游玩
    /// </summary>
    void ResumePlay();

    /// <summary>
    /// Resets the player / 重置玩家
    /// </summary>
    void ResetPlayer();

    /// <summary>
    /// Destroys the player / 销毁玩家
    /// </summary>
    void DestroyPlayer();
}
