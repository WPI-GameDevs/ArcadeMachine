// CaptureAdapterHook.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "CaptureAdapterHook.h"

#define MAIN_PROCESS_STILL_LAUNCHING 1
#define DX_SETUP_FAILED 2

struct SharedBlock
{
	UINT errorFlag;
	bool runningFlag;
	HANDLE sharedTex;

	int device;
	int output;
};


bool createdLock = false;
CONDITION_VARIABLE runLock;
CRITICAL_SECTION criticalSection;
HANDLE accessLock;

SharedBlock* sharedMem = NULL;

ID3D11Device* device = NULL;
IDXGIFactory1* factory = NULL;
IDXGIOutput* output = NULL;
IDXGIOutputDuplication* duplication = NULL;
ID3D11Texture2D* sharedTexture = NULL;

IDXGIKeyedMutex* sharedTextureLock = NULL;

bool CreateDXFactory()
{
	HRESULT result = CreateDXGIFactory1(__uuidof(IDXGIFactory1), (void**)&factory);
	return !FAILED(result);
}

bool CreateDXDevice()
{
	HRESULT result;

	IDXGIAdapter* adapter;
	result = factory->EnumAdapters(sharedMem->device, &adapter);
	if (FAILED(result))
		return false;
	
	result = adapter->EnumOutputs(sharedMem->output, &output);
	if (FAILED(result))
		return false;

	IDXGIOutput1* out1;
	result = output->QueryInterface<IDXGIOutput1>(&out1);
	if (FAILED(result))
		return false;

	D3D_FEATURE_LEVEL feature_levels[] = {
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
		D3D_FEATURE_LEVEL_9_3,
		D3D_FEATURE_LEVEL_9_2,
		D3D_FEATURE_LEVEL_9_1,
	};

	D3D_FEATURE_LEVEL featureLevel;
	result = D3D11CreateDevice(adapter, D3D_DRIVER_TYPE_UNKNOWN, NULL, 0, feature_levels, ARRAYSIZE(feature_levels), D3D11_SDK_VERSION, &device, &featureLevel, NULL);
	if (FAILED(result))
		return false;

	result = out1->DuplicateOutput(device, &duplication);
	if (FAILED(result))
		return false;

	return true;
}

bool CreateTexture()
{
	DXGI_OUTPUT_DESC out_desc;
	output->GetDesc(&out_desc);

	UINT width = out_desc.DesktopCoordinates.right - out_desc.DesktopCoordinates.left;
	UINT height = out_desc.DesktopCoordinates.bottom - out_desc.DesktopCoordinates.top;

	D3D11_TEXTURE2D_DESC desc;
	memset(&desc, 0, sizeof(D3D11_TEXTURE2D_DESC));

	desc.ArraySize = 1;
	desc.MipLevels = 1;
	desc.Width = width;
	desc.Height = height;
	desc.SampleDesc.Count = 1;
	desc.SampleDesc.Quality = 0;
	desc.CPUAccessFlags = 0;
	desc.Usage = D3D11_USAGE_DEFAULT;
	desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	desc.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;

	HRESULT result = device->CreateTexture2D(&desc, NULL, &sharedTexture);
	if (FAILED(result))
		return false;

	IDXGIResource* sharedResource;
	sharedTexture->QueryInterface<IDXGIResource>(&sharedResource);

	result = sharedResource->GetSharedHandle(&sharedMem->sharedTex);
	if (FAILED(result))
		return false;

	result = sharedTexture->QueryInterface<IDXGIKeyedMutex>(&sharedTextureLock);
	if (FAILED(result))
		return false;

	return true;
}

extern "C" CAPTUREADAPTERHOOK_API void SetupCapture(SharedBlock* sharedBlock)
{
	WaitForSingleObject(accessLock, INFINITE);

	std::cout << "Setting up capture device\n";

	if (!createdLock)
	{
		sharedBlock->errorFlag = MAIN_PROCESS_STILL_LAUNCHING;
		ReleaseMutex(accessLock);
		return;
	}

	if (sharedMem)
	{
		sharedBlock->sharedTex = sharedMem->sharedTex;
		sharedMem = sharedBlock;
	}
	else
	{
		sharedMem = sharedBlock;
		if (!CreateDXFactory() || !CreateDXDevice() || !CreateTexture())
		{
			sharedBlock->errorFlag = DX_SETUP_FAILED;
			ReleaseMutex(&accessLock);
			return;
		}
	}

	ReleaseMutex(&accessLock);

	WakeAllConditionVariable(&runLock);
}

// This is an example of an exported function.
extern "C" CAPTUREADAPTERHOOK_API void RunCapture(void)
{
	InitializeCriticalSection(&criticalSection);
	InitializeConditionVariable(&runLock);

	accessLock = CreateMutex(NULL, FALSE, NULL);

	createdLock = true;

	SleepConditionVariableCS(&runLock, &criticalSection, INFINITE);//wait till we are signaled that we have been setup to begin the capture

	while (sharedMem->runningFlag)
	{
		Sleep(8);//run at twice unitys frame rate to give us some buffer time

		WaitForSingleObject(accessLock, INFINITE);

		HRESULT result;

		IDXGIResource* screenRes = NULL;

		DXGI_OUTDUPL_FRAME_INFO frameInfo;
		result = duplication->AcquireNextFrame(8, &frameInfo, &screenRes);

		if (result == DXGI_ERROR_WAIT_TIMEOUT)
		{
			ReleaseMutex(accessLock);
			continue;
		}
		else if (FAILED(result))
		{
			std::cout << "Error while duplicating, killing.\n";
			return;
		}

		sharedTextureLock->AcquireSync(0, INFINITE);//acquire ownership over texture, or wait till we can acquire owner ownership

		ID3D11Resource* d3dScreenRes;
		screenRes->QueryInterface<ID3D11Resource>(&d3dScreenRes);

		ID3D11Resource* sharedRes;
		sharedTexture->QueryInterface<ID3D11Resource>(&sharedRes);

		ID3D11DeviceContext* context;
		device->GetImmediateContext(&context);

		context->CopyResource(sharedRes, d3dScreenRes);

		sharedTextureLock->ReleaseSync(0);

		screenRes->Release();
		duplication->ReleaseFrame();

#ifdef _DEBUG
		std::cout << "Frame pushed.\n";
#endif

		ReleaseMutex(accessLock);
	}

	sharedMem = NULL;
}