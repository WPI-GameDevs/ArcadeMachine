#include "stdafx.h"
#include "d3d11_bridge.h"
#include "UnityNativeBridge.h"


ID3D11Device* g_device = NULL;//the game device
ID3D11Texture2D* capture_tex = NULL;//the shared resource texture that we will capture onto
ID3D11ShaderResourceView* capture_shader_resource = NULL;//the resource that unity will use for rendering
IDXGIOutputDuplication* desktop_output;//the output from the desktop

#define SAFE_RELEASE(a) if(a){a->Release(); a = NULL;}

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

extern "C" UNITYNATIVEBRIDGE_API HRESULT SetupCapture(UINT capture_adapter, UINT capture_output)
{
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
	
	//grab the output on the adapter that we want to capture from
	IDXGIOutput* output = NULL;
	if (adapter->EnumOutputs(capture_output, &output) == DXGI_ERROR_NOT_FOUND)
		return -1;

	DXGI_OUTPUT_DESC desc;
	output->GetDesc(&desc);

	LONG width = desc.DesktopCoordinates.right - desc.DesktopCoordinates.left;
	LONG height = desc.DesktopCoordinates.bottom - desc.DesktopCoordinates.top;

	D3D11_TEXTURE2D_DESC tex_desc;
	tex_desc.Width = width;
	tex_desc.Height = height;
	tex_desc.MipLevels = tex_desc.ArraySize = 1;
	tex_desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	tex_desc.SampleDesc.Count = 1;
	tex_desc.SampleDesc.Quality = 0;
	tex_desc.Usage = D3D11_USAGE_DEFAULT;
	tex_desc.BindFlags = D3D11_BIND_SHADER_RESOURCE;
	tex_desc.CPUAccessFlags = 0;
	tex_desc.MiscFlags = 0;//allow us to share the resource

	HRESULT result;
	result = g_device->CreateTexture2D(&tex_desc, NULL, &capture_tex);
	if (FAILED(result))
		return result;

	D3D11_SHADER_RESOURCE_VIEW_DESC rec_desc;
	memset(&rec_desc, 0, sizeof(rec_desc));

	rec_desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
	rec_desc.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	rec_desc.Texture2D.MipLevels = 1;

	ID3D11Resource* tex_resource;
	capture_tex->QueryInterface(__uuidof(ID3D11Resource), (void**)&tex_resource);
	result = g_device->CreateShaderResourceView(tex_resource, &rec_desc, &capture_shader_resource);

	if (FAILED(result))
		return result;

	IDXGIOutput1* output1 = NULL;
	output->QueryInterface(__uuidof(IDXGIOutput1), (void**)&output1);

	result = output1->DuplicateOutput(g_device, &desktop_output);
	if (FAILED(result))
		return result;

	return result;
} 

extern "C" UNITYNATIVEBRIDGE_API void* __stdcall UnityGetCaptureResource()
{
	if (!g_device || !capture_shader_resource)
		return NULL;

	return capture_shader_resource;
}

extern "C" void UNITYNATIVEBRIDGE_API UnityRenderEvent(int eventID)
{
	if (!g_device || !desktop_output)
		return;

	IDXGIResource* src_resource;//the resource to copy from, this will be our screen
	DXGI_OUTDUPL_FRAME_INFO frame_info;

	if (desktop_output->AcquireNextFrame(16, &frame_info, &src_resource) == DXGI_ERROR_WAIT_TIMEOUT)//grab the screen resource
	{
		return;
	}

	ID3D11DeviceContext* context;
	g_device->GetImmediateContext(&context);

	ID3D11Resource* src_resource_d3d;
	src_resource->QueryInterface(__uuidof(ID3D11Resource), (void**)&src_resource_d3d);
	context->CopyResource(src_resource_d3d, capture_tex);

	src_resource->Release();
	desktop_output->ReleaseFrame();
}

extern "C" void UNITYNATIVEBRIDGE_API DestroyCaptureDevice()
{
	SAFE_RELEASE(desktop_output);
	SAFE_RELEASE(capture_shader_resource);
}