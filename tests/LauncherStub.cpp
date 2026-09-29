#include <windows.h>
#include <cstdlib>
int WINAPI WinMain(HINSTANCE,HINSTANCE,LPSTR,int){char code[32]={};GetEnvironmentVariableA("DS2STC_TEST_EXIT",code,32);return atoi(code);}
