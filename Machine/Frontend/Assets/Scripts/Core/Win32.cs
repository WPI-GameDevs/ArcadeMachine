using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


public static class Win32
{
    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    public static void SetWindowPosition(IntPtr hwnd, int x, int y, int resX = 0, int resY = 0)
    {
        SetWindowPos(hwnd, 0, x, y, resX, resY, resX * resY == 0 ? 1 : 0);
    }
}
