using System.Numerics;

namespace SharpFAI.Framework;

/// <summary>
/// Planet rendering interface / 行星渲染接口
/// </summary>
public interface IPlanets
{
    /// <summary>
    /// Renders the planets / 渲染行星
    /// </summary>
    /// <param name="shader">Shader to use / 使用的着色器</param>
    /// <param name="camera">Camera to render with / 用于渲染的相机</param>
    void Render(IShader shader,ICamera camera);

    /// <summary>
    /// Moves to the target position / 移动到目标位置
    /// </summary>
    /// <param name="target">Target position / 目标位置</param>
    void MoveTo(Vector2 target);

    /// <summary>
    /// Planet tail interface / 行星拖尾接口
    /// </summary>
    public interface ITail
    {
        /// <summary>
        /// Renders the tail / 渲染拖尾
        /// </summary>
        void Render();
    }
}
