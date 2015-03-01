using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System;
using System.Runtime.InteropServices;

[RequireComponent(typeof(UnityEngine.UI.RawImage))]
public class WindowDisplay : MonoBehaviour
{

    [DllImport("UnityNativeBridge")]
    static extern bool SetupCapture(UInt32 pid, int device, int output);

    [DllImport("UnityNativeBridge")]
    static extern void DestroyCaptureDevice();

    UInt32 GetCaptureAdapterPid()
    {
        foreach(Process p in Process.GetProcesses())
        {
            try
            {
                if(p.ProcessName == "CaptureAdapter")
                {
                    return (UInt32)p.Id;
                }
            }
            catch (InvalidOperationException) { continue; }
        }

        return 0;
    }

	// Use this for initialization
	void Start ()
    {
        UInt32 pid = GetCaptureAdapterPid();
        if (pid == 0)
            return;

        SetupCapture(pid, adapter, screen);
	}

    void OnDestroy()
    {
        DestroyCaptureDevice();
    }

    [SerializeField]
    private int adapter;

    [SerializeField]
    private int screen;

    private Texture2D outTex;
}
