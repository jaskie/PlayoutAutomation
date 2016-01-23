using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace TestPlayer
{
    public class VideoHost : HwndHost
    {
        internal const int
            WsChild = 0x40000000,
            WsVisible = 0x10000000,
            HostId = 0x00000002;

        private IntPtr _hwndHost;
        private int _hostHeight;
        private int _hostWidth;

        public VideoHost(int initialHeight, int initialWidth)
        {
            _hostHeight = initialHeight;
            _hostWidth = initialWidth;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            _hwndHost = IntPtr.Zero;

            _hwndHost = CreateWindowEx(0, "STATIC", "VideoHostWindow",
                WsChild | WsVisible,
                0, 0,
                _hostHeight, _hostWidth,
                hwndParent.Handle,
                (IntPtr)HostId,
                IntPtr.Zero,
                0);
            return new HandleRef(this, _hwndHost);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyWindow(hwnd.Handle);
        }

        public IntPtr Handle { get { return _hwndHost; } }

        //PInvoke declarations
        [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateWindowEx(int dwExStyle,
            string lpszClassName,
            string lpszWindowName,
            int style,
            int x, int y,
            int width, int height,
            IntPtr hwndParent,
            IntPtr hMenu,
            IntPtr hInst,
            [MarshalAs(UnmanagedType.AsAny)] object pvParam);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        internal static extern bool DestroyWindow(IntPtr hwnd);
    }
}
