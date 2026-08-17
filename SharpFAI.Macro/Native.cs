using System.Runtime.InteropServices;
using SharpHook.Data;

namespace SharpFAI.Macro
{
	public class Native
	{
		private delegate void KeyBoardEvent(int vkCode, int scanCode, int flags, int extraInfo);

		private static KeyBoardEvent KeyBoardEventPtr;

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GetOpenFileName(ref OpenFileName ofn);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr LoadLibrary(string lpszLibName);

		[DllImport("user32.dll")]
		public static extern int VkKeyScanA(char ch);

		static Native()
		{
			KeyBoardEventPtr = Marshal.GetDelegateForFunctionPointer<KeyBoardEvent>(NativeLibrary.GetExport(NativeLibrary.Load("user32.dll"), "keybd_event"));
		}

		public static string Show(string filter, string initialDir = null, string title = null)
		{
			OpenFileName ofn = default(OpenFileName);
			ofn.lStructSize = Marshal.SizeOf(ofn);
			ofn.hwndOwner = (IntPtr)0;
			ofn.lpstrFilter = filter ?? "所有文件(*.*)\0*.*\0\0";
			ofn.lpstrFile = new string(new char[260]);
			ofn.nMaxFile = 260;
			ofn.lpstrInitialDir = initialDir;
			ofn.lpstrTitle = title;
			if (!GetOpenFileName(ref ofn))
			{
				return null;
			}
			return ofn.lpstrFile;
		}

		public static void PressKey(KeyCode keyCode, double timeMs)
		{
			KeyBoardEventPtr((int)keyCode, 0, 0, 0);
			if (timeMs > 1.0)
			{
				double num = (double)DateTime.Now.Ticks / 10000.0;
				while ((double)DateTime.Now.Ticks / 10000.0 - num < timeMs)
				{
				}
			}
			KeyBoardEventPtr((int)keyCode, 0, 2, 0);
		}

		public static void PressKey(int keyCode, double timeMs)
		{
			KeyBoardEventPtr(keyCode, 0, 0, 0);
			if (timeMs > 1.0)
			{
				double num = HighPrecisionTimer.GetTimeInNanoseconds() / 1000000.0;
				while (!(HighPrecisionTimer.GetTimeInNanoseconds() / 1000000.0 - num > timeMs))
				{
				}
			}
			KeyBoardEventPtr(keyCode, 0, 2, 0);
		}
	}
}
