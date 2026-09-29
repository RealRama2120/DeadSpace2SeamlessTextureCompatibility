#pragma once
// Optional diagnostic observer. It never changes texture data, API arguments,
// graphics settings, or the D3D object returned to the game.
#include <d3d9.h>
#include <map>
#include <set>
#include <mutex>
#include <cstdarg>
namespace TextureTrace {
static std::recursive_mutex guard;
static std::map<void**,void*> originals;
struct Lock { D3DLOCKED_RECT rect; D3DSURFACE_DESC desc; };
static std::map<IDirect3DTexture9*,Lock> locks;
static std::map<IDirect3DTexture9*,DWORD> hashes;
static std::set<DWORD> bound;
struct Expected {UINT width,height;DWORD format,crc;bool supported;};
static std::map<DWORD,Expected> expected;
static std::map<DWORD,int> attempts;
static bool verbose=false;
static IDirect3D9* (WINAPI *create9)(UINT)=nullptr;
static void Write(const char* format,...) {
    if(!verbose&&strncmp(format,"TRACE_",6)==0)return;
    char line[1600];va_list args;va_start(args,format);vsnprintf_s(line,sizeof(line),_TRUNCATE,format,args);va_end(args);
    LogEvent(line);
}
static void Owner(void* address,const char* label) {
    HMODULE module=nullptr;char path[MAX_PATH]={};
    if(GetModuleHandleExA(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT,reinterpret_cast<LPCSTR>(address),&module))GetModuleFileNameA(module,path,MAX_PATH);
    Write("TRACE_OWNER %s address=%p module=%s",label,address,path);
}
static void** Slot(void* object,size_t index){return *reinterpret_cast<void***>(object)+index;}
template<class T> static T Original(void* object,size_t index){std::lock_guard<std::recursive_mutex> lock(guard);return reinterpret_cast<T>(originals.at(Slot(object,index)));}
static void Patch(void* object,size_t index,void* replacement,const char* label) {
    std::lock_guard<std::recursive_mutex> lock(guard);void** slot=Slot(object,index);
    if(originals.count(slot))return;
    DWORD old;if(!VirtualProtect(slot,sizeof(void*),PAGE_EXECUTE_READWRITE,&old)){Write("TRACE_PATCH_FAILED %s",label);return;}
    originals[slot]=*slot;Owner(*slot,label);InterlockedExchangePointer(slot,replacement);DWORD ignored;VirtualProtect(slot,sizeof(void*),old,&ignored);
}
static DWORD Crc(const BYTE* data,size_t bytes,DWORD crc) {
    static DWORD table[256]={};static bool ready=false;
    if(!ready){for(DWORD i=0;i<256;i++){DWORD c=i;for(int n=0;n<8;n++)c=(c>>1)^((c&1)?0xedb88320:0);table[i]=c;}ready=true;}
    for(size_t i=0;i<bytes;i++)crc=(crc>>8)^table[(crc^data[i])&255];return crc;
}
#include "DdsPixels.h"
static HRESULT WINAPI LockTexture(IDirect3DTexture9* self,UINT level,D3DLOCKED_RECT* rect,const RECT* area,DWORD flags) {
    auto original=Original<HRESULT(WINAPI*)(IDirect3DTexture9*,UINT,D3DLOCKED_RECT*,const RECT*,DWORD)>(self,19);
    HRESULT hr=original(self,level,rect,area,flags);
    if(SUCCEEDED(hr)&&level==0&&rect&&rect->pBits&&!area){Lock info={};info.rect=*rect;if(SUCCEEDED(self->GetLevelDesc(0,&info.desc))){std::lock_guard<std::recursive_mutex> lock(guard);locks[self]=info;}}
    return hr;
}
static HRESULT WINAPI UnlockTexture(IDirect3DTexture9* self,UINT level) {
    auto original=Original<HRESULT(WINAPI*)(IDirect3DTexture9*,UINT)>(self,20);
    if(level==0){std::lock_guard<std::recursive_mutex> lock(guard);auto it=locks.find(self);if(it!=locks.end()){
        const Lock info=it->second;locks.erase(it);size_t row=0,rows=info.desc.Height;
        switch(info.desc.Format){case D3DFMT_DXT1:row=((info.desc.Width+3)/4)*8;rows=(rows+3)/4;break;
        case D3DFMT_DXT2:case D3DFMT_DXT3:case D3DFMT_DXT4:case D3DFMT_DXT5:row=((info.desc.Width+3)/4)*16;rows=(rows+3)/4;break;
        case D3DFMT_A8R8G8B8:case D3DFMT_X8R8G8B8:case D3DFMT_A8B8G8R8:case D3DFMT_X8B8G8R8:row=info.desc.Width*4;break;
        case D3DFMT_A8:case D3DFMT_L8:row=info.desc.Width;break;default:break;}
        if(row&&info.rect.Pitch>0&&row<=static_cast<size_t>(info.rect.Pitch)&&row*rows<=67108864){DWORD crc=0xffffffff;for(size_t y=0;y<rows;y++)crc=Crc(static_cast<const BYTE*>(info.rect.pBits)+y*info.rect.Pitch,row,crc);hashes[self]=crc;
            Write("TRACE_UPLOAD texture=%p width=%u height=%u format=%08x pitch=%d hash=%08x",self,info.desc.Width,info.desc.Height,info.desc.Format,info.rect.Pitch,crc);}
    }}return original(self,level);
}
static HRESULT WINAPI CreateTexture(IDirect3DDevice9* self,UINT w,UINT h,UINT levels,DWORD usage,D3DFORMAT format,D3DPOOL pool,IDirect3DTexture9** texture,HANDLE* shared) {
    auto original=Original<HRESULT(WINAPI*)(IDirect3DDevice9*,UINT,UINT,UINT,DWORD,D3DFORMAT,D3DPOOL,IDirect3DTexture9**,HANDLE*)>(self,23);
    HRESULT hr=original(self,w,h,levels,usage,format,pool,texture,shared);
    if(SUCCEEDED(hr)&&texture&&*texture){std::lock_guard<std::recursive_mutex> lock(guard);hashes.erase(*texture);locks.erase(*texture);
        Patch(*texture,19,reinterpret_cast<void*>(&LockTexture),"Texture.LockRect");Patch(*texture,20,reinterpret_cast<void*>(&UnlockTexture),"Texture.UnlockRect");
        Write("TRACE_CREATE texture=%p width=%u height=%u format=%08x pool=%u",*texture,w,h,format,pool);}
    return hr;
}
static HRESULT WINAPI SetTexture(IDirect3DDevice9* self,DWORD stage,IDirect3DBaseTexture9* texture) {
    auto original=Original<HRESULT(WINAPI*)(IDirect3DDevice9*,DWORD,IDirect3DBaseTexture9*)>(self,65);
    HRESULT hr=original(self,stage,texture);
    if(SUCCEEDED(hr)&&texture){std::lock_guard<std::recursive_mutex> lock(guard);auto it=hashes.find(reinterpret_cast<IDirect3DTexture9*>(texture));
        if(it!=hashes.end()&&!bound.count(it->second)){
            DWORD sourceHash=it->second;auto target=expected.find(sourceHash);
            if(target==expected.end()){bound.insert(sourceHash);Write("TRACE_BOUND stage=%u texture=%p hash=%08x",stage,texture,sourceHash);return hr;}
            Expected want=target->second;
            if(!want.supported){bound.insert(sourceHash);Write("TARGET_UNVERIFIABLE hash=%08x reason=unsupported_or_ambiguous_package_image",sourceHash);return hr;}
            IDirect3DBaseTexture9* actual=nullptr;HRESULT queried=self->GetTexture(stage,&actual);bool matched=false,readable=false;DWORD crc=0;D3DSURFACE_DESC desc={};
            if(SUCCEEDED(queried)&&actual&&actual!=texture&&actual->GetType()==D3DRTYPE_TEXTURE){auto replacement=static_cast<IDirect3DTexture9*>(actual);
                if(SUCCEEDED(replacement->GetLevelDesc(0,&desc))&&desc.Width==want.width&&desc.Height==want.height&&static_cast<DWORD>(desc.Format)==want.format){D3DLOCKED_RECT data={};
                    if(SUCCEEDED(replacement->LockRect(0,&data,nullptr,D3DLOCK_READONLY))){size_t row=0,rows=desc.Height;
                        if(desc.Format>=D3DFMT_DXT1&&desc.Format<=D3DFMT_DXT5){row=((desc.Width+3)/4)*(desc.Format==D3DFMT_DXT1?8:16);rows=(desc.Height+3)/4;}
                        else if(desc.Format==D3DFMT_A8R8G8B8||desc.Format==D3DFMT_X8R8G8B8)row=desc.Width*4;
                        if(row&&data.Pitch>0&&row<=static_cast<size_t>(data.Pitch)&&data.pBits){crc=0xffffffff;for(size_t y=0;y<rows;y++){const BYTE* bytes=static_cast<const BYTE*>(data.pBits)+y*data.Pitch;
                            if(desc.Format==D3DFMT_DXT1||desc.Format==D3DFMT_DXT3||desc.Format==D3DFMT_DXT5){size_t blockSize=desc.Format==D3DFMT_DXT1?8:16;for(size_t x=0;x<row;x+=blockSize)crc=PixelBlockCrc(bytes+x,desc.Format,crc);}else crc=Crc(bytes,row,crc);
                        }readable=true;matched=crc==want.crc;}
                        replacement->UnlockRect(0);
                    }
                }
            }
            bool same=actual==texture;if(actual)actual->Release();
            if(matched){bound.insert(sourceHash);Write("INJECTION_VERIFIED hash=%08x replacement_crc=%08x width=%u height=%u",sourceHash,crc,want.width,want.height);}
            else if(++attempts[sourceHash]>=3){bound.insert(sourceHash);const char* outcome=same?"INJECTION_UNCHANGED_OBJECT":(readable?"INJECTION_PIXEL_MISMATCH":"INJECTION_UNVERIFIABLE");Write("%s hash=%08x same_object=%u readable=%u actual_crc=%08x expected_crc=%08x actual_size=%ux%u format=%08x",outcome,sourceHash,same?1:0,readable?1:0,crc,want.crc,desc.Width,desc.Height,desc.Format);}
        }
    }return hr;
}
static HRESULT WINAPI CreateDevice(IDirect3D9* self,UINT adapter,D3DDEVTYPE type,HWND window,DWORD behavior,D3DPRESENT_PARAMETERS* parameters,IDirect3DDevice9** device) {
    auto original=Original<HRESULT(WINAPI*)(IDirect3D9*,UINT,D3DDEVTYPE,HWND,DWORD,D3DPRESENT_PARAMETERS*,IDirect3DDevice9**)>(self,16);
    HRESULT hr=original(self,adapter,type,window,behavior,parameters,device);
    Write("TRACE_DEVICE hr=%08x",hr);if(SUCCEEDED(hr)&&device&&*device){Patch(*device,23,reinterpret_cast<void*>(&CreateTexture),"Device.CreateTexture");Patch(*device,65,reinterpret_cast<void*>(&SetTexture),"Device.SetTexture");}return hr;
}
static IDirect3D9* WINAPI Create9(UINT sdk){IDirect3D9* result=create9(sdk);Write("TRACE_CREATE9 result=%p",result);if(result)Patch(result,16,reinterpret_cast<void*>(&CreateDevice),"D3D9.CreateDevice");return result;}
static void Start() {
    wchar_t flag[8]={};verbose=GetEnvironmentVariableW(L"DS2STC_TRACE",flag,8)&&wcscmp(flag,L"1")==0;
    wchar_t path[32768];DWORD size=GetEnvironmentVariableW(L"DS2STC_SESSION",path,32768);
    if(size&&size<32740){wcscat_s(path,L"\\expected.txt");FILE* file=nullptr;if(_wfopen_s(&file,path,L"r")==0&&file){char line[256];while(fgets(line,sizeof(line),file)){unsigned hash,width,height,format,crc,supported;if(sscanf_s(line,"%x %u %u %x %x %u",&hash,&width,&height,&format,&crc,&supported)==6&&width<=16384&&height<=16384)expected[hash]={width,height,format,crc,supported!=0};}fclose(file);}}
    Write("VERIFIER_STARTED expected_hashes=%u",static_cast<unsigned>(expected.size()));
    HMODULE d3d=GetModuleHandleW(L"d3d9.dll");if(!d3d){Write("VERIFIER_UNAVAILABLE reason=no_d3d9_module_at_entry");return;}
    auto target=GetProcAddress(d3d,"Direct3DCreate9");Owner(reinterpret_cast<void*>(target),"Direct3DCreate9");
    MH_STATUS status=MH_CreateHook(reinterpret_cast<void*>(target),reinterpret_cast<void*>(&Create9),reinterpret_cast<void**>(&create9));
    if(status==MH_OK)status=MH_EnableHook(reinterpret_cast<void*>(target));Write("VERIFIER_HOOK_STATUS status=%d",status);
}
}
