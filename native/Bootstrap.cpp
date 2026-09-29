#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <cstdio>

// A diagnostic build first establishes whether MarkerPatch loads us before the
// game's first D3D creation. No code is patched on disk and no proxy is replaced.
static std::wstring root;
static decltype(&RegCloseKey) realClose;
typedef void* (WINAPI *Create9)(UINT);
static Create9 realCreate9;
static void Record(const char* event) {
    std::wstring path=root+L"\\DS2STC-probe.log";
    HANDLE f=CreateFileW(path.c_str(),FILE_APPEND_DATA,FILE_SHARE_READ|FILE_SHARE_WRITE,nullptr,OPEN_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(f==INVALID_HANDLE_VALUE)return;
    char line[512];int n=sprintf_s(line,"%llu pid=%lu tid=%lu %s\r\n",GetTickCount64(),GetCurrentProcessId(),GetCurrentThreadId(),event);
    DWORD written;WriteFile(f,line,static_cast<DWORD>(n),&written,nullptr);CloseHandle(f);
}
static LSTATUS WINAPI CloseHook(HKEY key) { Record("GAME_REGCLOSEKEY");return realClose(key); }
static void* WINAPI CreateHook(UINT sdk) {Record("GAME_DIRECT3DCREATE9");return realCreate9(sdk);}
static bool PatchImport(const char* name,void* replacement,void** original) {
    auto base=reinterpret_cast<BYTE*>(GetModuleHandleW(nullptr));
    auto dos=reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    if(dos->e_magic!=IMAGE_DOS_SIGNATURE)return false;
    auto nt=reinterpret_cast<IMAGE_NT_HEADERS*>(base+dos->e_lfanew);
    auto dir=nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if(!dir.VirtualAddress)return false;
    auto desc=reinterpret_cast<IMAGE_IMPORT_DESCRIPTOR*>(base+dir.VirtualAddress);
    for(;desc->Name;++desc) {
        auto module=GetModuleHandleA(reinterpret_cast<char*>(base+desc->Name));
        auto wanted=module?GetProcAddress(module,name):nullptr;
        if(!wanted)continue;
        auto names=desc->OriginalFirstThunk?reinterpret_cast<IMAGE_THUNK_DATA*>(base+desc->OriginalFirstThunk):nullptr;
        auto slots=reinterpret_cast<IMAGE_THUNK_DATA*>(base+desc->FirstThunk);
        for(;slots->u1.Function;++slots) {
            bool match=reinterpret_cast<FARPROC>(slots->u1.Function)==wanted;
            if(names) {
                if(!IMAGE_SNAP_BY_ORDINAL(names->u1.Ordinal) && names->u1.AddressOfData<nt->OptionalHeader.SizeOfImage) {
                    auto import=reinterpret_cast<IMAGE_IMPORT_BY_NAME*>(base+names->u1.AddressOfData);
                    match=match||!strcmp(reinterpret_cast<char*>(import->Name),name);
                }
                ++names;
            }
            if(!match)continue;
            DWORD old;if(!VirtualProtect(&slots->u1.Function,sizeof(void*),PAGE_READWRITE,&old))return false;
            *original=reinterpret_cast<void*>(slots->u1.Function);
            InterlockedExchangePointer(reinterpret_cast<void* volatile*>(&slots->u1.Function),replacement);
            DWORD ignored;VirtualProtect(&slots->u1.Function,sizeof(void*),old,&ignored);return true;
        }
    }
    return false;
}
BOOL WINAPI DllMain(HINSTANCE instance,DWORD reason,LPVOID) {
    if(reason!=DLL_PROCESS_ATTACH)return TRUE;
    DisableThreadLibraryCalls(instance);
    wchar_t path[32768];DWORD n=GetModuleFileNameW(nullptr,path,32768);
    if(!n||n>=32768)return FALSE;
    std::wstring game(path,n);auto slash=game.find_last_of(L"\\/");
    if(slash==std::wstring::npos||_wcsicmp(game.substr(slash+1).c_str(),L"deadspace2.exe"))return TRUE;
    root=game.substr(0,slash);
    Record("ASI_PROCESS_ATTACH");
    Record(PatchImport("RegCloseKey",reinterpret_cast<void*>(&CloseHook),reinterpret_cast<void**>(&realClose))?"REGCLOSE_GATE_INSTALLED":"REGCLOSE_GATE_UNAVAILABLE");
    Record(PatchImport("Direct3DCreate9",reinterpret_cast<void*>(&CreateHook),reinterpret_cast<void**>(&realCreate9))?"D3D9_OBSERVER_INSTALLED":"D3D9_OBSERVER_UNAVAILABLE");
    return TRUE;
}
