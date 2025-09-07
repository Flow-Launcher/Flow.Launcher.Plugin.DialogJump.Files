using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace Flow.Launcher.Plugin.DialogJump.Files;

public static class Win32Helper
{
    internal static unsafe string GetProcessNameFromHwnd(HWND hWnd)
    {
        return Path.GetFileName(GetProcessPathFromHwnd(hWnd));
    }

    internal static unsafe string GetProcessPathFromHwnd(HWND hWnd)
    {
        uint pid;
        var threadId = PInvoke.GetWindowThreadProcessId(hWnd, &pid);
        if (threadId == 0) return string.Empty;

        var process = PInvoke.OpenProcess(PROCESS_ACCESS_RIGHTS.PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
        if (process != HWND.Null)
        {
            using var safeHandle = new SafeProcessHandle((nint)process.Value, true);
            uint capacity = 2000;
            Span<char> buffer = new char[capacity];
            if (!PInvoke.QueryFullProcessImageName(safeHandle, PROCESS_NAME_FORMAT.PROCESS_NAME_WIN32, buffer, ref capacity))
            {
                return string.Empty;
            }

            return buffer[..(int)capacity].ToString();
        }

        return string.Empty;
    }
}
