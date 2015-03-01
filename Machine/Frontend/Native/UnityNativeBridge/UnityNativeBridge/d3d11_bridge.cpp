#include "stdafx.h"
#include "System.h"
#include "d3d11_bridge.h"
#include "UnityNativeBridge.h"


ID3D11Device* g_device = NULL;//the game device
ID3D11Texture2D* sharedTexture;
ID3D11ShaderResourceView* unityTexture;

#define SAFE_RELEASE(a) if(a){a->Release(); a = NULL;}

struct SharedBlock
{
	UINT errorFlag;
	bool runningFlag;
	HANDLE sharedTex;

	int device;
	int output;
};

LPVOID remoteSharedBlock;
SharedBlock sharedBlock;

HMODULE hookModule = NULL;
HANDLE captureProgram = NULL;

static void SetGraphicsDevice(ID3D11Device* device, int eventType)
{
	if (eventType == GfxDeviceEventType::kGfxDeviceEventInitialize)
	{
		g_device = device;
	}
	else if (eventType == GfxDeviceEventType::kGfxDeviceEventShutdown)
	{
		SAFE_RELEASE(g_device);
	}
}

extern "C" void UNITYNATIVEBRIDGE_API UnitySetGraphicsDevice(void* device, int deviceType, int eventType)
{
	if (deviceType != GfxDeviceRenderer::kGfxRendererD3D11)//we do not support any device that is not d3d11
		return;

	SetGraphicsDevice((ID3D11Device*)device, eventType);
}

HRESULT TestSetupCapture(UINT capture_adapter, UINT capture_output);

extern "C" UNITYNATIVEBRIDGE_API bool SetupCapture(DWORD pid, int device, int output)
{
	hookModule = LoadLibraryA("CaptureAdapterHook.dll");
	if (hookModule == NULL)
		return false;

	//get a handle to the capture process
	captureProgram = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_DUP_HANDLE | PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, FALSE, pid);
	if (captureProgram == NULL)
	{
		FreeLibrary(hookModule);
		hookModule = NULL;
		return false;
	}

	LPVOID runCaptureAddress = GetModuleProcAddress(pid, L"CaptureAdapterHook.dll", "SetupCapture");//get the remote address of setup capture

	remoteSharedBlock = VirtualAllocEx(captureProgram, NULL, sizeof(SharedBlock), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	if (remoteSharedBlock == NULL)
	{
		FreeLibrary(hookModule);
		hookModule = NULL;
		return false;
	}
	
	sharedBlock.device = device;
	sharedBlock.output = output;
	sharedBlock.runningFlag = true;
	
	SIZE_T n = 0;
	BOOL result = WriteProcessMemory(captureProgram, remoteSharedBlock, &sharedBlock, sizeof(SharedBlock), &n);
	if (!result)
	{
		HRESULT winResult = HRESULT_FROM_WIN32(GetLastError());
		FreeLibrary(hookModule);
		hookModule = NULL;
		return false;
	}

	HANDLE threadID = CreateRemoteThread(captureProgram, NULL, 0, (LPTHREAD_START_ROUTINE)runCaptureAddress, remoteSharedBlock, NULL, NULL);
	if (threadID == NULL)
	{
		FreeLibrary(hookModule);
		hookModule = NULL;
		return false;
	}

	return true;
}

/*HRESULT TestSetupCapture(UINT capture_adapter, UINT capture_output)
{
	HRESULT result;

	UINT flags = g_device->GetCreationFlags();

	UINT supported;
	g_device->CheckFormatSupport(DXGI_FORMAT_R8G8B8A8_UNORM, &supported);

	//when there is no device return NULL to unity
	if (!g_device)
		return -1;

	//create the factory that we will use to get adapter information
	IDXGIFactory1* factory = NULL;
	CreateDXGIFactory1(__uuidof(IDXGIFactory1), (void**)&factory);
	if (!factory)
		return -1;

	//grab the capture adapter, if we do not have that adapter then fail
	IDXGIAdapter1* adapter = NULL;
	if(factory->EnumAdapters1(capture_adapter, &adapter) == DXGI_ERROR_NOT_FOUND)
		return -1;

	IDXGIAdapter* device_adapter = NULL;
	if (factory->EnumAdapters(capture_adapter, &device_adapter) == DXGI_ERROR_NOT_FOUND)
		return -1;
	
	//grab the output on the adapter that we want to capture from
	IDXGIOutput* output = NULL;
	if (adapter->EnumOutputs(capture_output, &output) == DXGI_ERROR_NOT_FOUND)
		return -1;

	//create a new device that can do the capture
	D3D_FEATURE_LEVEL feature_levels[] = {
		D3D_FEATURE_LEVEL_11_1,
		D3D_FEATURE_LEVEL_11_0,
		D3D_FEATURE_LEVEL_10_1,
		D3D_FEATURE_LEVEL_10_0,
		D3D_FEATURE_LEVEL_9_3,
		D3D_FEATURE_LEVEL_9_2,
		D3D_FEATURE_LEVEL_9_1,
	};

	D3D_FEATURE_LEVEL feature_level;
	ID3D11DeviceContext* context;
	result = D3D11CreateDevice(device_adapter,
		D3D_DRIVER_TYPE_UNKNOWN,
		NULL,
		g_device->GetCreationFlags(),
		feature_levels,
		ARRAYSIZE(feature_levels),
		D3D11_SDK_VERSION,
		&capture_device,
		&feature_level,
		&context);
	if (FAILED(result))
		return result;

	DXGI_OUTPUT_DESC desc;
	output->GetDesc(&desc);

	//look at the target adapters width and height for content creation
	LONG width = desc.DesktopCoordinates.right - desc.DesktopCoordinates.left;
	LONG height = desc.DesktopCoordinates.bottom - desc.DesktopCoordinates.top;

	//create the shared texture that we will capture too
	D3D11_TEXTURE2D_DESC tex_desc;
	tex_desc.Width = width;
	tex_desc.Height = height;
	tex_desc.MipLevels = tex_desc.ArraySize = 1;
	tex_desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	tex_desc.SampleDesc.Count = 1;
	tex_desc.SampleDesc.Quality = 0;
	tex_desc.Usage = D3D11_USAGE_DEFAULT;
	tex_desc.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
	tex_desc.CPUAccessFlags = 0;
	tex_desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;//allow us to share the resource

	result = capture_device->CreateTexture2D(&tex_desc, NULL, &capture_tex);
	if (FAILED(result))
		return result;

	//create the unity resource that we will use around the texture
	D3D11_SHADER_RESOURCE_VIEW_DESC resource_description;
	memset(&resource_description, 0, sizeof(resource_description));

	resource_description.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	resource_description.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	resource_description.Texture2D.MipLevels = 1;

	ID3D11Resource* tex_resource;
	capture_tex->QueryInterface(__uuidof(ID3D11Resource), (void**)&tex_resource);
	result = capture_device->CreateShaderResourceView(tex_resource, &resource_description, &capture_shader_resource);

	if (FAILED(result))
		return result;

	//grab the shared resource information
	IDXGIResource* shareable_resource;
	result = capture_tex->QueryInterface(__uuidof(IDXGIResource), (void**)&shareable_resource);
	if (FAILED(result))
		return result;

	HANDLE shared_handle;
	result = shareable_resource->GetSharedHandle(&shared_handle);
	if (FAILED(result))
		return result;

	ID3D11Resource* temp;
	result = g_device->OpenSharedResource(shared_handle, __uuidof(ID3D11Resource), (void**)&temp);
	if (result != S_OK)
		return result;

	//create the capture duplicator
	IDXGIOutput1* output1 = NULL;
	output->QueryInterface(__uuidof(IDXGIOutput1), (void**)&output1);

	result = output1->DuplicateOutput(capture_device, &desktop_output);
	if (FAILED(result))
		return result;

	return result;
} */

extern "C" UNITYNATIVEBRIDGE_API void* __stdcall UnityGetCaptureResource()
{
	if (!remoteSharedBlock)
		return NULL;

	HRESULT result;

	do
	{
		result = ReadProcessMemory(captureProgram, remoteSharedBlock, (void*)&sharedBlock, sizeof(SharedBlock), NULL);
		if (FAILED(result))
			return NULL;
	} while (!sharedBlock.sharedTex && !sharedBlock.errorFlag);

	if (sharedBlock.errorFlag)
		return NULL;

	HANDLE sharedHandle = sharedBlock.sharedTex;

	result = g_device->OpenSharedResource(sharedHandle, __uuidof(ID3D11Texture2D), (void**)&sharedTexture);

	D3D11_SHADER_RESOURCE_VIEW_DESC resource_description;
	memset(&resource_description, 0, sizeof(resource_description));

	resource_description.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	resource_description.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	resource_description.Texture2D.MipLevels = 1;

	ID3D11Resource* texResource;
	result = sharedTexture->QueryInterface<ID3D11Resource>(&texResource);
	if (FAILED(result))
		return NULL;

	result = g_device->CreateShaderResourceView(texResource, &resource_description, &unityTexture);
	if (FAILED(result))
		return NULL;

	return (void**)unityTexture;
}

extern "C" void UNITYNATIVEBRIDGE_API UnityRenderEvent(int eventID)
{

}

extern "C" void UNITYNATIVEBRIDGE_API DestroyCaptureDevice()
{
	if (hookModule)
		FreeLibrary(hookModule);
}