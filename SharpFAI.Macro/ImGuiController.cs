using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace SharpFAI.Macro
{
	public class ImGuiController : IDisposable
	{
		private bool _disposed;

		private int _vertexArray;

		private int _vertexBuffer;

		private int _vertexBufferSize;

		private int _indexBuffer;

		private int _indexBufferSize;

		private int _fontTexture;

		private int _shader;

		private int _shaderFontTextureLocation;

		private int _shaderProjectionMatrixLocation;

		private int _windowWidth;

		private int _windowHeight;

		private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

		private ImGuiIOPtr io;

		private float _time;

		private bool _useGradientShader;

		private int _gradientMode = 3;

		public ImGuiController(int width, int height)
		{
			_windowWidth = width;
			_windowHeight = height;
			ImGui.SetCurrentContext(ImGui.CreateContext());
			io = ImGui.GetIO();
			io.Fonts.AddFontFromFileTTF("C:\\Windows\\Fonts\\msyh.ttc", 18f, null, io.Fonts.GetGlyphRangesChineseFull());
			io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
			io.DisplaySize = new System.Numerics.Vector2((float)_windowWidth / _scaleFactor.X, (float)_windowHeight / _scaleFactor.Y);
			CreateDeviceResources();
		}

		public void WindowResized(int width, int height)
		{
			_windowWidth = width;
			_windowHeight = height;
			io.DisplaySize = new System.Numerics.Vector2((float)_windowWidth / _scaleFactor.X, (float)_windowHeight / _scaleFactor.Y);
		}

		public void CreateDeviceResources()
		{
			_vertexArray = GL.GenVertexArray();
			GL.BindVertexArray(_vertexArray);
			_vertexBufferSize = 10000;
			_indexBufferSize = 2000;
			_vertexBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
			GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
			_indexBuffer = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
			GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
			RecreateFontDeviceTexture();
			string vertexSource = "#version 330 core\r\nuniform mat4 projection_matrix;\r\nlayout(location = 0) in vec2 in_position;\r\nlayout(location = 1) in vec2 in_texCoord;\r\nlayout(location = 2) in vec4 in_color;\r\nout vec4 color;\r\nout vec2 texCoord;\r\nvoid main()\r\n{\r\n    gl_Position = projection_matrix * vec4(in_position, 0, 1);\r\n    color = in_color;\r\n    texCoord = in_texCoord;\r\n}";
			string fragmentSource = "#version 330 core\r\nuniform sampler2D in_fontTexture;\r\nin vec4 color;\r\nin vec2 texCoord;\r\nout vec4 outputColor;\r\nvoid main()\r\n{\r\n    outputColor = color * texture(in_fontTexture, texCoord);\r\n}";
			_shader = CreateProgram("ImGui", vertexSource, fragmentSource);
			_shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
			_shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");
			_useGradientShader = false;
			GL.BindVertexArray(_vertexArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
			int stride = Unsafe.SizeOf<ImDrawVert>();
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, normalized: false, stride, 0);
			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, normalized: false, stride, 8);
			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, normalized: true, stride, 16);
			GL.BindVertexArray(0);
		}

		private void RecreateFontDeviceTexture()
		{
			ImGuiIOPtr iO = ImGui.GetIO();
			iO.Fonts.GetTexDataAsRGBA32(out IntPtr out_pixels, out int out_width, out int out_height, out int _);
			_fontTexture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, out_width, out_height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, out_pixels);
			iO.Fonts.SetTexID((IntPtr)_fontTexture);
			iO.Fonts.ClearTexData();
		}

		private int CreateProgram(string name, string vertexSource, string fragmentSource)
		{
			int num = GL.CreateProgram();
			int shader = CompileShader(name, ShaderType.VertexShader, vertexSource);
			int shader2 = CompileShader(name, ShaderType.FragmentShader, fragmentSource);
			GL.AttachShader(num, shader);
			GL.AttachShader(num, shader2);
			GL.LinkProgram(num);
			GL.GetProgram(num, GetProgramParameterName.LinkStatus, out var @params);
			if (@params == 0)
			{
				string programInfoLog = GL.GetProgramInfoLog(num);
				Console.WriteLine("GL.LinkProgram had errors for " + name + ":\n" + programInfoLog);
			}
			GL.DetachShader(num, shader);
			GL.DetachShader(num, shader2);
			GL.DeleteShader(shader);
			GL.DeleteShader(shader2);
			return num;
		}

		private int CompileShader(string name, ShaderType type, string source)
		{
			int num = GL.CreateShader(type);
			GL.ShaderSource(num, source);
			GL.CompileShader(num);
			GL.GetShader(num, ShaderParameter.CompileStatus, out var @params);
			if (@params == 0)
			{
				string shaderInfoLog = GL.GetShaderInfoLog(num);
				Console.WriteLine($"GL.CompileShader for {name} [{type}] had errors:\n{shaderInfoLog}");
			}
			return num;
		}

		public void Update(GameWindow wnd, float deltaSeconds)
		{
			_time += deltaSeconds;
			io.DeltaTime = deltaSeconds;
			UpdateImGuiInput(wnd);
			try
			{
				ImGui.NewFrame();
			}
			catch (Exception ex)
			{
				Console.WriteLine("ImGui.NewFrame exception: " + ex.Message);
			}
		}

		private void UpdateImGuiInput(GameWindow wnd)
		{
			ImGuiIOPtr iO = ImGui.GetIO();
			MouseState mouseState = wnd.MouseState;
			KeyboardState keyboardState = wnd.KeyboardState;
			iO.MouseDown[0] = mouseState[MouseButton.Button1];
			iO.MouseDown[1] = mouseState[MouseButton.Button2];
			iO.MouseDown[2] = mouseState[MouseButton.Button3];
			Vector2i vector2i = new Vector2i((int)mouseState.X, (int)mouseState.Y);
			iO.MousePos = new System.Numerics.Vector2(vector2i.X, vector2i.Y);
			foreach (Keys value in Enum.GetValues(typeof(Keys)))
			{
				if (value != Keys.Unknown)
				{
					ImGuiKey imGuiKey = ConvertKey(value);
					if (imGuiKey != ImGuiKey.None)
					{
						bool down = keyboardState.IsKeyDown(value);
						iO.AddKeyEvent(imGuiKey, down);
					}
				}
			}
			iO.KeyCtrl = keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
			iO.KeyAlt = keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt);
			iO.KeyShift = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
			iO.KeySuper = keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper);
		}

		private ImGuiKey ConvertKey(Keys key)
		{
			return key switch
			{
				Keys.Tab => ImGuiKey.NamedKey_BEGIN, 
				Keys.Left => ImGuiKey.LeftArrow, 
				Keys.Right => ImGuiKey.RightArrow, 
				Keys.Up => ImGuiKey.UpArrow, 
				Keys.Down => ImGuiKey.DownArrow, 
				Keys.PageUp => ImGuiKey.PageUp, 
				Keys.PageDown => ImGuiKey.PageDown, 
				Keys.Home => ImGuiKey.Home, 
				Keys.End => ImGuiKey.End, 
				Keys.Insert => ImGuiKey.Insert, 
				Keys.Delete => ImGuiKey.Delete, 
				Keys.Backspace => ImGuiKey.Backspace, 
				Keys.Space => ImGuiKey.Space, 
				Keys.Enter => ImGuiKey.Enter, 
				Keys.Escape => ImGuiKey.Escape, 
				Keys.A => ImGuiKey.A, 
				Keys.C => ImGuiKey.C, 
				Keys.V => ImGuiKey.V, 
				Keys.X => ImGuiKey.X, 
				Keys.Y => ImGuiKey.Y, 
				Keys.Z => ImGuiKey.Z, 
				_ => ImGuiKey.None, 
			};
		}

		public void Render()
		{
			try
			{
				ImGui.Render();
				RenderImDrawData(ImGui.GetDrawData());
			}
			catch (Exception ex)
			{
				Console.WriteLine("ImGui.Render exception: " + ex.Message);
			}
		}

		private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
		{
			if (drawData.CmdListsCount == 0)
			{
				return;
			}
			int integer = GL.GetInteger(GetPName.VertexArrayBinding);
			int integer2 = GL.GetInteger(GetPName.ArrayBufferBinding);
			int integer3 = GL.GetInteger(GetPName.CurrentProgram);
			bool boolean = GL.GetBoolean(GetPName.Blend);
			bool boolean2 = GL.GetBoolean(GetPName.ScissorTest);
			int integer4 = GL.GetInteger(GetPName.BlendEquation);
			int integer5 = GL.GetInteger(GetPName.BlendEquationAlpha);
			int integer6 = GL.GetInteger(GetPName.BlendSrcRgb);
			int integer7 = GL.GetInteger(GetPName.BlendSrcAlpha);
			int integer8 = GL.GetInteger(GetPName.BlendDstRgb);
			int integer9 = GL.GetInteger(GetPName.BlendDstAlpha);
			bool boolean3 = GL.GetBoolean(GetPName.CullFace);
			bool boolean4 = GL.GetBoolean(GetPName.DepthTest);
			int integer10 = GL.GetInteger(GetPName.ActiveTexture);
			GL.ActiveTexture(TextureUnit.Texture0);
			int integer11 = GL.GetInteger(GetPName.TextureBinding2D);
			Span<int> span = stackalloc int[4];
			fixed (int* data = &span[0])
			{
				GL.GetInteger(GetPName.ScissorBox, data);
			}
			GL.BindVertexArray(_vertexArray);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
			for (int i = 0; i < drawData.CmdListsCount; i++)
			{
				ImDrawListPtr imDrawListPtr = drawData.CmdLists[i];
				int num = imDrawListPtr.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
				if (num > _vertexBufferSize)
				{
					int num2 = (int)Math.Max((float)_vertexBufferSize * 1.5f, num);
					GL.BufferData(BufferTarget.ArrayBuffer, num2, IntPtr.Zero, BufferUsageHint.DynamicDraw);
					_vertexBufferSize = num2;
				}
				int num3 = imDrawListPtr.IdxBuffer.Size * 2;
				if (num3 > _indexBufferSize)
				{
					int num4 = (int)Math.Max((float)_indexBufferSize * 1.5f, num3);
					GL.BufferData(BufferTarget.ElementArrayBuffer, num4, IntPtr.Zero, BufferUsageHint.DynamicDraw);
					_indexBufferSize = num4;
				}
			}
			ImGuiIOPtr iO = ImGui.GetIO();
			Matrix4 matrix = Matrix4.CreateOrthographicOffCenter(0f, iO.DisplaySize.X, iO.DisplaySize.Y, 0f, -1f, 1f);
			GL.UseProgram(_shader);
			GL.UniformMatrix4(_shaderProjectionMatrixLocation, transpose: false, ref matrix);
			GL.Uniform1(_shaderFontTextureLocation, 0);
			GL.BindVertexArray(_vertexArray);
			drawData.ScaleClipRects(iO.DisplayFramebufferScale);
			GL.Enable(EnableCap.Blend);
			GL.Enable(EnableCap.ScissorTest);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.DepthTest);
			for (int j = 0; j < drawData.CmdListsCount; j++)
			{
				ImDrawListPtr imDrawListPtr2 = drawData.CmdLists[j];
				GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
				GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, imDrawListPtr2.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), imDrawListPtr2.VtxBuffer.Data);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
				GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, imDrawListPtr2.IdxBuffer.Size * 2, imDrawListPtr2.IdxBuffer.Data);
				for (int k = 0; k < imDrawListPtr2.CmdBuffer.Size; k++)
				{
					ImDrawCmdPtr imDrawCmdPtr = imDrawListPtr2.CmdBuffer[k];
					GL.ActiveTexture(TextureUnit.Texture0);
					GL.BindTexture(TextureTarget.Texture2D, (int)(nint)imDrawCmdPtr.TextureId);
					System.Numerics.Vector4 clipRect = imDrawCmdPtr.ClipRect;
					GL.Scissor((int)clipRect.X, _windowHeight - (int)clipRect.W, (int)(clipRect.Z - clipRect.X), (int)(clipRect.W - clipRect.Y));
					if ((iO.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != ImGuiBackendFlags.None)
					{
						GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)imDrawCmdPtr.ElemCount, DrawElementsType.UnsignedShort, (nint)(imDrawCmdPtr.IdxOffset * 2), (int)imDrawCmdPtr.VtxOffset);
					}
					else
					{
						GL.DrawElements(BeginMode.Triangles, (int)imDrawCmdPtr.ElemCount, DrawElementsType.UnsignedShort, (int)(imDrawCmdPtr.IdxOffset * 2));
					}
				}
			}
			GL.BindVertexArray(integer);
			GL.BindBuffer(BufferTarget.ArrayBuffer, integer2);
			GL.UseProgram(integer3);
			GL.BlendEquationSeparate((BlendEquationMode)integer4, (BlendEquationMode)integer5);
			GL.BlendFuncSeparate((BlendingFactorSrc)integer6, (BlendingFactorDest)integer8, (BlendingFactorSrc)integer7, (BlendingFactorDest)integer9);
			if (boolean)
			{
				GL.Enable(EnableCap.Blend);
			}
			else
			{
				GL.Disable(EnableCap.Blend);
			}
			if (boolean4)
			{
				GL.Enable(EnableCap.DepthTest);
			}
			else
			{
				GL.Disable(EnableCap.DepthTest);
			}
			if (boolean3)
			{
				GL.Enable(EnableCap.CullFace);
			}
			else
			{
				GL.Disable(EnableCap.CullFace);
			}
			if (boolean2)
			{
				GL.Enable(EnableCap.ScissorTest);
			}
			else
			{
				GL.Disable(EnableCap.ScissorTest);
			}
			GL.ActiveTexture((TextureUnit)integer10);
			GL.BindTexture(TextureTarget.Texture2D, integer11);
			GL.Scissor(span[0], span[1], span[2], span[3]);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				GL.DeleteVertexArray(_vertexArray);
				GL.DeleteBuffer(_vertexBuffer);
				GL.DeleteBuffer(_indexBuffer);
				GL.DeleteTexture(_fontTexture);
				GL.DeleteProgram(_shader);
				_disposed = true;
			}
		}
	}
}
