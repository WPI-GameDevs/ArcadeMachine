// CaptureAdapter.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

typedef void(*RUN_CAPTURE)(void);

int _tmain(int argc, _TCHAR* argv[])
{
	HMODULE hook = LoadLibraryA("CaptureAdapterHook.dll");
	
	RUN_CAPTURE run_capture = (RUN_CAPTURE)GetProcAddress(hook, "RunCapture");

	run_capture();

	Sleep(5000);

	return 0;
}

