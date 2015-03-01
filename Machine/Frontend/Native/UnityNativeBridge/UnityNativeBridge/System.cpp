
#include "stdafx.h"

#include "System.h"

LPVOID GetModuleBaseAddress(DWORD iProcId, const LPWSTR DLLName)
{
	HANDLE hSnap; // Process snapshot handle.
	MODULEENTRY32 xModule; // Module information structure.

	hSnap = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, iProcId); // Creates a module
	// snapshot of the
	// game process.
	xModule.dwSize = sizeof(MODULEENTRY32); // Needed for Module32First/Next to work.
	if (Module32First(hSnap, &xModule)) // Gets the first module.
	{
		do {
			if (wcscmp(xModule.szModule, DLLName) == 0) // If this is the module we want...
			{
				CloseHandle(hSnap); // Free the handle.
				return (LPVOID)xModule.modBaseAddr; // return the base address.
			}
		} while (Module32Next(hSnap, &xModule)); // Loops through the rest of the modules.
	}
	CloseHandle(hSnap); // Free the handle.
	return 0; // If the result of the function is 0, it didn't find the base address.
	// i.e.. the dll isn't loaded.
}


//Sense modules can be loaded in different areas of memory on different applications
//we need some way to get the address of a function in a remote module. Sense function
//is always at the same offset within a module we can do some math to solve the remote
//address. We take the base address of the module in our application, and get the offset
//to the function. We can then add that to the base address in our target application
//to get the remote address.
LPVOID GetModuleProcAddress(DWORD procID, const LPWSTR dll, LPCSTR entryPoint)
{
	LPVOID ourBaseAddress = (LPVOID)GetModuleBaseAddress(GetCurrentProcessId(), dll);
	if (ourBaseAddress == 0)
		return NULL;



	LPVOID ourEntryPoint = GetProcAddress(GetModuleHandle(dll), entryPoint);

	uintptr_t offset = reinterpret_cast<uintptr_t>(ourEntryPoint) - reinterpret_cast<uintptr_t>(ourBaseAddress);

	LPVOID otherBaseAddress = (LPVOID)GetModuleBaseAddress(procID, dll);

	uintptr_t otherEntryPoint = reinterpret_cast<uintptr_t>(otherBaseAddress) + offset;

	return reinterpret_cast<LPVOID>(otherEntryPoint);
}