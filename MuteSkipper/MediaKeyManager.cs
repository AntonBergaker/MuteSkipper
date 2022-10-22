using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MuteSkipper;
public static class MediaKeyManager {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);
    public const int VK_MEDIA_NEXT_TRACK = 0xB0;
    public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    public const int VK_MEDIA_PREV_TRACK = 0xB1;
    public const int KEYEVENTF_EXTENDEDKEY = 0x0001; //Key down flag
    public const int KEYEVENTF_KEYUP = 0x0002; //Key up flag

    public static void SendSkipCommand() {
        keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_EXTENDEDKEY, IntPtr.Zero);
        keybd_event(VK_MEDIA_NEXT_TRACK, 0, KEYEVENTF_KEYUP, IntPtr.Zero);
    }
}
