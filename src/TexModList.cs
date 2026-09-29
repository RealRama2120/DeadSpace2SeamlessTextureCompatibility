using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
namespace DeadSpaceTextureLauncher {
    // Launcher and TexMod are both x86. List-view text pointers must point into
    // the TexMod process, not the launcher's address space.
    internal static class TexModList {
        [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
        [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr VirtualAllocEx(IntPtr p,IntPtr address,uint bytes,uint allocation,uint protection);
        [DllImport("kernel32.dll")]static extern bool VirtualFreeEx(IntPtr p,IntPtr address,uint bytes,uint type);
        [DllImport("kernel32.dll")]static extern bool WriteProcessMemory(IntPtr p,IntPtr address,byte[] data,int size,out IntPtr written);
        [DllImport("kernel32.dll")]static extern bool ReadProcessMemory(IntPtr p,IntPtr address,byte[] data,int size,out IntPtr read);
        [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
        [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SendMessageTimeout(IntPtr w,uint m,IntPtr a,IntPtr b,uint flags,uint timeout,out IntPtr result);
        internal static List<string> Read(int pid,IntPtr list,int count) {
            var rows=new List<string>();IntPtr process=OpenProcess(0x38,false,pid);if(process==IntPtr.Zero)throw new LauncherException("Cannot read back TexMod's package order.");
            IntPtr remote=IntPtr.Zero;
            try {
                remote=VirtualAllocEx(process,IntPtr.Zero,8192,0x3000,4);if(remote==IntPtr.Zero)throw new LauncherException("Cannot allocate TexMod list readback buffer.");
                IntPtr text=new IntPtr(remote.ToInt64()+256);
                for(int i=0;i<count;i++) {
                    byte[] item=new byte[64];Array.Copy(BitConverter.GetBytes(i),0,item,4,4);Array.Copy(BitConverter.GetBytes(text.ToInt32()),0,item,20,4);Array.Copy(BitConverter.GetBytes(2048),0,item,24,4);
                    IntPtr transferred,result;if(!WriteProcessMemory(process,remote,item,item.Length,out transferred))throw new LauncherException("Could not submit list readback request.");
                    if(SendMessageTimeout(list,0x102d,new IntPtr(i),remote,2,3000,out result)==IntPtr.Zero)throw new LauncherException("TexMod package-order readback timed out.");
                    byte[] value=new byte[4096];if(!ReadProcessMemory(process,text,value,value.Length,out transferred))throw new LauncherException("Could not read TexMod list text.");
                    string row=Encoding.Default.GetString(value);int zero=row.IndexOf('\0');if(zero>=0)row=row.Substring(0,zero);rows.Add(row);
                }
                return rows;
            }finally{if(remote!=IntPtr.Zero)VirtualFreeEx(process,remote,0,0x8000);CloseHandle(process);}
        }
    }
}
