#include <windows.h>
#include <cstdio>
int main(){DWORD ignored=0;wchar_t path[MAX_PATH];GetSystemDirectoryW(path,MAX_PATH);wcscat_s(path,L"\\kernel32.dll");DWORD size=GetFileVersionInfoSizeW(path,&ignored);FILE* file=nullptr;fopen_s(&file,"original-ran.txt","w");if(file){fprintf(file,"forwarded_version_size=%lu\n",size);fclose(file);}return size?0:9;}
