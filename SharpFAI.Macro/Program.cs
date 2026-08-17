using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace SharpFAI.Macro
{
    public static class Program
    {
        private static void Main()
        {
            GameWindowSettings gameWindowSettings = GameWindowSettings.Default;
            NativeWindowSettings nativeWindowSettings = new NativeWindowSettings
            {
                ClientSize = new Vector2i(500, 300),
                Title = "OpenTK",
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            };
            using MainWindow mainWindow = new MainWindow(gameWindowSettings, nativeWindowSettings);
            mainWindow.Run();
        }
    }
}