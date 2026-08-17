using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SharpFAI.Serialization;
using SharpFAI.Util;
using SharpHook;
using SharpHook.Data;

namespace SharpFAI.Macro
{
	public class MainWindow : GameWindow
	{
		private ImGuiController _imguiController;

		private MacroThread macroThread;

		private string keyList = "ASDFGHJKL";

		private string state = "未加载关卡";

		private string _lastOpenedFile = "未打开任何文件";

		private int startFloor;

		private bool useNewTiming = true;

		private float currentProgress;

		private TaskPoolGlobalHook globalHook;

		private AppData appData = AppData.instance;

		public Level? currentLevel;

		public static MainWindow Instance;

		public MainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
			: base(gameWindowSettings, nativeWindowSettings)
		{
			Instance = this;
			globalHook = new TaskPoolGlobalHook();
			globalHook.KeyPressed += delegate(object? o, KeyboardHookEventArgs e)
			{
				if (macroThread != null)
				{
					switch (e.Data.KeyCode)
					{
					case KeyCode.VcW:
						macroThread.Offset = 0.0;
						macroThread.Start();
						state = "运行中";
						break;
					case KeyCode.VcLeft:
						macroThread.Offset -= 2.0;
						break;
					case KeyCode.VcRight:
						macroThread.Offset += 2.0;
						break;
					case KeyCode.VcEscape:
						macroThread.Stop();
						state = "已停止，正在等待按键";
						break;
					}
				}
			};
		}

		protected override void OnTextInput(TextInputEventArgs e)
		{
			base.OnTextInput(e);
			if (_imguiController != null)
			{
				ImGui.GetIO().AddInputCharacter((uint)e.Unicode);
			}
		}

		protected override void OnLoad()
		{
			base.OnLoad();
			_imguiController = new ImGuiController(base.ClientSize.X, base.ClientSize.Y);
		}

		protected override void OnUpdateFrame(FrameEventArgs args)
		{
			base.OnUpdateFrame(args);
			_imguiController.Update(this, (float)args.Time);
		}

		private void LoadLevel(string path)
		{
			macroThread?.Stop();
			macroThread = null;
			startFloor = 0;
			currentLevel = new Level(path);
			appData.Write("lastOpen", path);
			state = "已加载关卡，等待开始";
		}

		protected override void OnRenderFrame(FrameEventArgs args)
		{
			base.OnRenderFrame(args);
			GL.ClearColor(0f, 0f, 0f, 1f);
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
			ImGui.SetNextWindowSize(new Vector2(base.ClientSize.X, base.ClientSize.Y));
			ImGui.SetNextWindowPos(new Vector2(0f, 0f));
			ImGui.Begin(" ", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.MenuBar);
			if (ImGui.BeginMenuBar())
			{
				if (ImGui.BeginMenu("文件"))
				{
				if (ImGui.MenuItem("打开", "Ctrl+O"))
				{
					_lastOpenedFile = Native.Show("ADOFAI Level(*.adofai)\0*.adofai\0");
					if (!string.IsNullOrEmpty(_lastOpenedFile))
					{
						LoadLevel(_lastOpenedFile);
					}
				}
				if (appData.Read("lastOpen", "") != "" && ImGui.MenuItem("打开上次", "Ctrl+L"))
				{
					_lastOpenedFile = appData.Read("lastOpen", "");
					if (!string.IsNullOrEmpty(_lastOpenedFile))
					{
						LoadLevel(_lastOpenedFile);
					}
					keyList = appData.Read("keyList", keyList);
				}
					ImGui.EndMenu();
				}
				ImGui.EndMenuBar();
			}
			ImGui.SetNextItemWidth(200f);
			ImGui.InputText("键位", ref keyList, 48u, ImGuiInputTextFlags.CharsNoBlank);
			if (startFloor >= currentLevel?.angles.Count)
			{
				Level? level = currentLevel;
				startFloor = (((object)level != null) ? (level.angles.Count - 1) : 0);
			}
			if (startFloor < 0)
			{
				startFloor = 0;
			}
			ImGui.InputInt("开始砖块", ref startFloor, 1);
			ImGui.Checkbox("New时间算法", ref useNewTiming);
			if (ImGui.Button("开始"))
			{
				macroThread = new MacroThread(keyList.ToCharArray().Select(Native.VkKeyScanA).ToList(), useNewTiming ? currentLevel.GetNoteTimesNew() : currentLevel.GetNoteTimes(), cmd: false, startFloor, useNewTiming);
				if (!globalHook.IsRunning)
				{
					globalHook.RunAsync();
				}
				appData.Write("keyList", keyList);
				state = "已开始，正在等待按键";
			}
			ImGui.Separator();
			ImGui.Text("当前打开文件：" + _lastOpenedFile);
			ImGui.Text("状态:" + state);
			ImGui.End();
			_imguiController.Render();
			SwapBuffers();
		}

		protected override void OnResize(ResizeEventArgs e)
		{
			base.OnResize(e);
			_imguiController.WindowResized(base.ClientSize.X, base.ClientSize.Y);
			GL.Viewport(0, 0, base.ClientSize.X, base.ClientSize.Y);
		}

		protected override void OnUnload()
		{
			_imguiController?.Dispose();
			appData.Write("keyList", keyList);
			appData.Write("lastOpen", _lastOpenedFile);
			appData.Save();
			globalHook.Dispose();
			base.OnUnload();
		}
	}
}
