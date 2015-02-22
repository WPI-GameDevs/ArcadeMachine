using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System;

public class MainManager : MonoBehaviour
{
    void Awake()
    {
        Process us = Process.GetCurrentProcess();
        IntPtr ourWindow = us.MainWindowHandle;

        Win32.SetWindowPosition(ourWindow, 0, 0);
    }
}
