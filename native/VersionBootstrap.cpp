#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <string>
#include <vector>
#include <cwchar>
#include <cstdio>
#include "../vendor/minhook/include/MinHook.h"

extern "C" FARPROC versionExports[17]={};
static const char* names[]={"GetFileVersionInfoA","GetFileVersionInfoByHandle","GetFileVersionInfoExA","GetFileVersionInfoExW","GetFileVersionInfoSizeA","GetFileVersionInfoSizeExA","GetFileVersionInfoSizeExW","GetFileVersionInfoSizeW","GetFileVersionInfoW","VerFindFileA","VerFindFileW","VerInstallFileA","VerInstallFileW","VerLanguageNameA","VerLanguageNameW","VerQueryValueA","VerQueryValueW"};
static wchar_t gamePath[32768]={};
static void* entryAddress=nullptr;
static void (__cdecl* originalEntry)()=nullptr;
static bool hookInstalled=false;
static bool traceChild=false;
static wchar_t runtimePath[32768]={};
static void LogEvent(const char* event) {
    wchar_t path[32768];DWORD n=GetEnvironmentVariableW(L"LOCALAPPDATA",path,32768);
    if(!n||n>32000)return;
    wcscat_s(path,L"\\Rama2120");CreateDirectoryW(path,nullptr);
    wcscat_s(path,L"\\DS2SeamlessTextures");CreateDirectoryW(path,nullptr);
    wcscat_s(path,L"\\bootstrap.log");
    HANDLE f=CreateFileW(path,FILE_APPEND_DATA,FILE_SHARE_READ|FILE_SHARE_WRITE,nullptr,OPEN_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);
    if(f==INVALID_HANDLE_VALUE)return;
    char line[2048];int len=_snprintf_s(line,sizeof(line),_TRUNCATE,"ticks=%llu pid=%lu %s\r\n",GetTickCount64(),GetCurrentProcessId(),event);if(len<0)len=static_cast<int>(strlen(line));DWORD wrote;WriteFile(f,line,static_cast<DWORD>(len),&wrote,nullptr);CloseHandle(f);
    if(runtimePath[0]){f=CreateFileW(runtimePath,FILE_APPEND_DATA,FILE_SHARE_READ|FILE_SHARE_WRITE,nullptr,OPEN_ALWAYS,FILE_ATTRIBUTE_NORMAL,nullptr);if(f!=INVALID_HANDLE_VALUE){WriteFile(f,line,static_cast<DWORD>(len),&wrote,nullptr);CloseHandle(f);}}
}
#include "TextureTrace.h"
static std::wstring Quote(const std::wstring& value) {
    std::wstring out=L"\"";size_t slashes=0;
    for(wchar_t ch:value){if(ch==L'\\'){++slashes;continue;}out.append(ch==L'"'?slashes*2+1:slashes,L'\\');slashes=0;out+=ch;}
    out.append(slashes*2,L'\\');out+=L'"';return out;
}
static void __cdecl EntryHook() {
    if(traceChild){DWORD size=GetEnvironmentVariableW(L"DS2STC_SESSION",runtimePath,32768);if(size&&size<32740)wcscat_s(runtimePath,L"\\runtime.log");else runtimePath[0]=0;MH_DisableHook(entryAddress);TextureTrace::Start();originalEntry();return;}
    // Called at the EXE entry point, after loader initialization has finished.
    // Keep this original Steam-tracked process alive until the owned launcher ends.
    LogEvent("ENTRY_GATE_REACHED");
    std::wstring root(gamePath);root.resize(root.find_last_of(L"\\/"));
    std::wstring launcher=root+L"\\DS2TextureLauncher.exe";
    if(GetFileAttributesW(launcher.c_str())==INVALID_FILE_ATTRIBUTES) {
        LogEvent("LAUNCHER_MISSING_OR_DISABLED: original entry continues");
        MH_DisableHook(entryAddress);originalEntry();return;
    }
    wchar_t pid[32];_ultow_s(GetCurrentProcessId(),pid,10);
    std::wstring command=Quote(launcher)+L" --bootstrap --game-pid "+pid+L" --game-root "+Quote(root);
    std::vector<wchar_t> buffer(command.begin(),command.end());buffer.push_back(0);
    STARTUPINFOW si={};si.cb=sizeof(si);PROCESS_INFORMATION pi={};
    if(!CreateProcessW(launcher.c_str(),buffer.data(),nullptr,nullptr,FALSE,0,nullptr,root.c_str(),&si,&pi)) {
        LogEvent("LAUNCHER_START_FAILED");MessageBoxW(nullptr,L"The texture launcher could not start. Textures were not enabled.",L"Dead Space 2 Seamless Textures",MB_OK|MB_ICONERROR);ExitProcess(1);
    }
    CloseHandle(pi.hThread);WaitForSingleObject(pi.hProcess,INFINITE);DWORD code=1;GetExitCodeProcess(pi.hProcess,&code);CloseHandle(pi.hProcess);
    if(code==10){LogEvent("BYPASS_REQUESTED: original entry continues");MH_DisableHook(entryAddress);originalEntry();return;}
    LogEvent(code==0?"OWNED_SESSION_ENDED":"OWNED_SESSION_FAILED");ExitProcess(code);
}
#define THUNK(name,offset) extern "C" __declspec(naked) void Forward_##name(){__asm{jmp dword ptr [versionExports+offset]}}
THUNK(GetFileVersionInfoA,0) THUNK(GetFileVersionInfoByHandle,4) THUNK(GetFileVersionInfoExA,8) THUNK(GetFileVersionInfoExW,12)
THUNK(GetFileVersionInfoSizeA,16) THUNK(GetFileVersionInfoSizeExA,20) THUNK(GetFileVersionInfoSizeExW,24) THUNK(GetFileVersionInfoSizeW,28)
THUNK(GetFileVersionInfoW,32) THUNK(VerFindFileA,36) THUNK(VerFindFileW,40) THUNK(VerInstallFileA,44) THUNK(VerInstallFileW,48)
THUNK(VerLanguageNameA,52) THUNK(VerLanguageNameW,56) THUNK(VerQueryValueA,60) THUNK(VerQueryValueW,64)
BOOL WINAPI DllMain(HINSTANCE self,DWORD reason,LPVOID) {
    if(reason!=DLL_PROCESS_ATTACH)return TRUE;
    DisableThreadLibraryCalls(self);
    wchar_t system[MAX_PATH];UINT len=GetSystemDirectoryW(system,MAX_PATH);if(!len||len>=MAX_PATH-13)return FALSE;
    wcscat_s(system,L"\\version.dll");HMODULE real=LoadLibraryW(system);if(!real||real==self)return FALSE;
    for(size_t i=0;i<17;i++){versionExports[i]=GetProcAddress(real,names[i]);if(!versionExports[i])return FALSE;}
    DWORD n=GetModuleFileNameW(nullptr,gamePath,32768);if(!n||n>=32768)return TRUE;
    const wchar_t* leaf=wcsrchr(gamePath,L'\\');if(!leaf||_wcsicmp(leaf+1,L"deadspace2.exe"))return TRUE;
    wchar_t flag[8]={};if(GetEnvironmentVariableW(L"DS2STC_TEXMOD_CHILD",flag,8)&&wcscmp(flag,L"1")==0){
        traceChild=GetEnvironmentVariableW(L"DS2STC_TRACE",flag,8)&&wcscmp(flag,L"1")==0;
        if(GetEnvironmentVariableW(L"DS2STC_SESSION",nullptr,0)>0)traceChild=true;
        if(!traceChild)return TRUE;
    }
    auto base=reinterpret_cast<BYTE*>(GetModuleHandleW(nullptr));auto dos=reinterpret_cast<IMAGE_DOS_HEADER*>(base);
    auto nt=reinterpret_cast<IMAGE_NT_HEADERS*>(base+dos->e_lfanew);
    entryAddress=base+nt->OptionalHeader.AddressOfEntryPoint;
    if(MH_Initialize()!=MH_OK)return FALSE;
    if(MH_CreateHook(entryAddress,reinterpret_cast<void*>(&EntryHook),reinterpret_cast<void**>(&originalEntry))!=MH_OK)return FALSE;
    hookInstalled=MH_EnableHook(entryAddress)==MH_OK;
    return hookInstalled?TRUE:FALSE;
}
