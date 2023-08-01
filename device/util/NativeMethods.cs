using System;
using System.Runtime.InteropServices;

namespace GodOfBeer.util
{
    internal class NativeMethods
    {
        public const int handleBroadcast = 0xFFFF;
        public static readonly int messageShowMe;

        static NativeMethods()
        {
            NativeMethods.messageShowMe = NativeMethods.RegisterWindowMessage("com.medipiaenc.vitalsign.CMS.showMe");
        }

        [DllImport("user32")]
        public static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32")]
        public static extern int RegisterWindowMessage(string message);
    }
}
