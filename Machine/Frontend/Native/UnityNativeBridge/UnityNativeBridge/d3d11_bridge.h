#pragma once

#include "stdafx.h"
#include "UnityNativeBridge.h"

// Graphics device identifiers in Unity
enum GfxDeviceRenderer
{
	kGfxRendererOpenGL = 0,			// OpenGL
	kGfxRendererD3D9,				// Direct3D 9
	kGfxRendererD3D11,				// Direct3D 11
	kGfxRendererGCM,				// Sony PlayStation 3 GCM
	kGfxRendererNull,				// "null" device (used in batch mode)
	kGfxRendererHollywood,			// Nintendo Wii
	kGfxRendererXenon,				// Xbox 360
	kGfxRendererOpenGLES_Obsolete,
	kGfxRendererOpenGLES20Mobile,	// OpenGL ES 2.0
	kGfxRendererMolehill_Obsolete,
	kGfxRendererOpenGLES20Desktop_Obsolete,
	kGfxRendererOpenGLES30,			// OpenGL ES 3.0
	kGfxRendererCount
};


// Event types for UnitySetGraphicsDevice
enum GfxDeviceEventType {
	kGfxDeviceEventInitialize = 0,
	kGfxDeviceEventShutdown,
	kGfxDeviceEventBeforeReset,
	kGfxDeviceEventAfterReset,
};