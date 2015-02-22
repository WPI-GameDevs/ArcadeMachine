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
    static extern UInt32 SetupCapture(uint adapter, uint output);

    [DllImport("UnityNativeBridge", CallingConvention = CallingConvention.StdCall)]
    static extern IntPtr UnityGetCaptureResource();

    [DllImport("UnityNativeBridge", CallingConvention = CallingConvention.StdCall)]
    static extern void DestroyCaptureDevice();

	// Use this for initialization
	void Start ()
    {
        long result = SetupCapture((uint)adapter, (uint)screen);
        UnityEngine.Debug.Log(result);
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
