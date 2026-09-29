#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <d3d9.h>
#include <cstdio>
static LRESULT CALLBACK WndProc(HWND h,UINT m,WPARAM w,LPARAM l){return DefWindowProcW(h,m,w,l);}
int WINAPI WinMain(HINSTANCE instance,HINSTANCE,LPSTR,int){
    FILE* log=nullptr;fopen_s(&log,"graphics-result.txt","w");if(!log)return 10;
    WNDCLASSW wc={};wc.lpfnWndProc=WndProc;wc.hInstance=instance;wc.lpszClassName=L"DS2STCGraphicsTest";RegisterClassW(&wc);
    HWND window=CreateWindowW(wc.lpszClassName,L"DS2STC controlled graphics test",WS_OVERLAPPEDWINDOW,100,100,320,320,nullptr,nullptr,instance,nullptr);ShowWindow(window,SW_SHOW);
    DWORD unused;GetFileVersionInfoSizeW(L"deadspace2.exe",&unused);
    IDirect3D9* d3d=Direct3DCreate9(D3D_SDK_VERSION);IDirect3DDevice9* device=nullptr;
    D3DPRESENT_PARAMETERS p={};p.Windowed=TRUE;p.SwapEffect=D3DSWAPEFFECT_DISCARD;p.BackBufferFormat=D3DFMT_X8R8G8B8;p.BackBufferWidth=256;p.BackBufferHeight=256;p.PresentationInterval=D3DPRESENT_INTERVAL_IMMEDIATE;
    HRESULT hr=d3d->CreateDevice(0,D3DDEVTYPE_HAL,window,D3DCREATE_HARDWARE_VERTEXPROCESSING,&p,&device);fprintf(log,"CreateDevice=%08lx\n",hr);fflush(log);if(FAILED(hr)){fclose(log);return 11;}
    IDirect3DTexture9* texture=nullptr;hr=device->CreateTexture(64,64,1,0,D3DFMT_A8R8G8B8,D3DPOOL_MANAGED,&texture,nullptr);fprintf(log,"CreateTexture=%08lx\n",hr);if(FAILED(hr)){fclose(log);return 12;}
    D3DLOCKED_RECT rect={};hr=texture->LockRect(0,&rect,nullptr,0);if(FAILED(hr)){fclose(log);return 13;}
    for(UINT y=0;y<64;y++)for(UINT x=0;x<64;x++)reinterpret_cast<DWORD*>(static_cast<BYTE*>(rect.pBits)+y*rect.Pitch)[x]=0xffff0000;
    texture->UnlockRect(0);
    struct Vertex{float x,y,z,rhw,u,v;};Vertex vertices[]={{-.5f,-.5f,0,1,0,0},{255.5f,-.5f,0,1,1,0},{-.5f,255.5f,0,1,0,1},{255.5f,255.5f,0,1,1,1}};
    device->SetFVF(D3DFVF_XYZRHW|D3DFVF_TEX1);device->SetRenderState(D3DRS_LIGHTING,FALSE);device->SetRenderState(D3DRS_CULLMODE,D3DCULL_NONE);device->SetRenderState(D3DRS_ZENABLE,FALSE);
    device->SetTextureStageState(0,D3DTSS_COLOROP,D3DTOP_SELECTARG1);device->SetTextureStageState(0,D3DTSS_COLORARG1,D3DTA_TEXTURE);
    DWORD began=GetTickCount();bool captured=false;
    while(GetTickCount()-began<15000){MSG msg;while(PeekMessageW(&msg,nullptr,0,0,PM_REMOVE)){TranslateMessage(&msg);DispatchMessageW(&msg);}
        device->Clear(0,nullptr,D3DCLEAR_TARGET,0xff000000,1,0);device->BeginScene();device->SetTexture(0,texture);device->DrawPrimitiveUP(D3DPT_TRIANGLESTRIP,2,vertices,sizeof(Vertex));device->EndScene();
        if(!captured&&GetTickCount()-began>5000){
            IDirect3DBaseTexture9* actual=nullptr;hr=device->GetTexture(0,&actual);fprintf(log,"BoundTexture=%p original=%p hr=%08lx\n",actual,texture,hr);
            if(actual&&actual->GetType()==D3DRTYPE_TEXTURE){auto boundTexture=static_cast<IDirect3DTexture9*>(actual);D3DSURFACE_DESC desc={};boundTexture->GetLevelDesc(0,&desc);fprintf(log,"BoundSize=%ux%u format=%08x\n",desc.Width,desc.Height,desc.Format);D3DLOCKED_RECT texels={};hr=boundTexture->LockRect(0,&texels,nullptr,D3DLOCK_READONLY);fprintf(log,"BoundLock=%08lx\n",hr);if(SUCCEEDED(hr)){fprintf(log,"BOUND_PIXEL=%08lx\n",*static_cast<DWORD*>(texels.pBits));boundTexture->UnlockRect(0);}}if(actual)actual->Release();
            IDirect3DSurface9* target=nullptr;IDirect3DSurface9* copy=nullptr;device->GetRenderTarget(0,&target);device->CreateOffscreenPlainSurface(256,256,D3DFMT_X8R8G8B8,D3DPOOL_SYSTEMMEM,&copy,nullptr);
            hr=device->GetRenderTargetData(target,copy);fprintf(log,"Readback=%08lx\n",hr);if(SUCCEEDED(hr)){D3DLOCKED_RECT pixels={};if(SUCCEEDED(copy->LockRect(&pixels,nullptr,D3DLOCK_READONLY))){DWORD color=reinterpret_cast<DWORD*>(static_cast<BYTE*>(pixels.pBits)+128*pixels.Pitch)[128];fprintf(log,"CENTER_PIXEL=%08lx\n",color);copy->UnlockRect();}}fflush(log);if(copy)copy->Release();if(target)target->Release();captured=true;}
        device->Present(nullptr,nullptr,nullptr,nullptr);Sleep(16);
    }
    texture->Release();device->Release();d3d->Release();DestroyWindow(window);fclose(log);return 0;
}
