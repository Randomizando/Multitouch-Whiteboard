using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Interop;
using System.Windows.Media;

namespace AnnotationApp
{
    public static class DesktopMode
    {
        private static InkCanvas inkCanvas;
        private static Window overlayWindow;

        public static void Start()
        {
            DisplayOverlay();
        }

        private static void DisplayOverlay()
        {
            var screenBounds = System.Windows.Forms.Screen.PrimaryScreen.Bounds;

            overlayWindow = new Window
            {
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                Topmost = true,
                Left = 0,
                Top = 0,
                Width = screenBounds.Width,
                Height = screenBounds.Height,
                ShowInTaskbar = false
            };

            inkCanvas = new InkCanvas
            {
                Background = Brushes.Transparent,
                EditingMode = InkCanvasEditingMode.Ink,
                Width = screenBounds.Width,
                Height = screenBounds.Height
            };

            overlayWindow.Content = inkCanvas;
            overlayWindow.Show();

            MakeWindowTransparent(new WindowInteropHelper(overlayWindow).Handle);
        }

        private static void MakeWindowTransparent(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);

            // Ensure the window is transparent
            SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA);
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const uint LWA_ALPHA = 0x2;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    }
}
