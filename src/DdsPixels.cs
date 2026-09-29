using System;
namespace DeadSpaceTextureLauncher {
    // Canonical BGRA pixels in block order. This ignores equivalent differences
    // in compressed endpoints/selectors introduced by TexMod's DDS loader.
    internal sealed class DdsPixels {
        readonly uint format;readonly byte[] block=new byte[16];int used;
        readonly byte[,] colors=new byte[4,4];readonly byte[] alpha=new byte[8];
        internal uint Crc=0xffffffff;
        internal DdsPixels(uint f){format=f;}
        internal void Add(byte value){
            if(format!=0x31545844&&format!=0x33545844&&format!=0x35545844){Crc=TpfValidator.Update(Crc,value);return;}
            block[used++]=value;int size=format==0x31545844?8:16;if(used!=size)return;used=0;
            int offset=size-8;uint a=BitConverter.ToUInt16(block,offset),b=BitConverter.ToUInt16(block,offset+2),selectors=BitConverter.ToUInt32(block,offset+4);
            Expand(a,colors,0);Expand(b,colors,1);
            bool four=a>b||size==16;
            for(int c=0;c<3;c++){colors[2,c]=(byte)(four?(2*colors[0,c]+colors[1,c])/3:(colors[0,c]+colors[1,c])/2);colors[3,c]=(byte)(four?(colors[0,c]+2*colors[1,c])/3:0);}
            colors[2,3]=255;colors[3,3]=(byte)(four?255:0);
            alpha[6]=0;ulong alphaBits=0;
            if(format==0x35545844){alpha[0]=block[0];alpha[1]=block[1];if(alpha[0]>alpha[1])for(int i=2;i<8;i++)alpha[i]=(byte)(((8-i)*alpha[0]+(i-1)*alpha[1])/7);else{for(int i=2;i<6;i++)alpha[i]=(byte)(((6-i)*alpha[0]+(i-1)*alpha[1])/5);alpha[7]=255;}for(int i=0;i<6;i++)alphaBits|=(ulong)block[2+i]<<(8*i);}
            for(int pixel=0;pixel<16;pixel++){int pick=(int)((selectors>>(2*pixel))&3);for(int c=0;c<3;c++)Crc=TpfValidator.Update(Crc,colors[pick,c]);byte opacity=colors[pick,3];if(format==0x33545844)opacity=(byte)(((block[pixel/2]>>((pixel&1)*4))&15)*17);else if(format==0x35545844)opacity=alpha[(int)((alphaBits>>(3*pixel))&7)];Crc=TpfValidator.Update(Crc,opacity);}
        }
        static void Expand(uint v,byte[,] result,int row){uint b=v&31,g=(v>>5)&63,r=(v>>11)&31;result[row,0]=(byte)((b<<3)|(b>>2));result[row,1]=(byte)((g<<2)|(g>>4));result[row,2]=(byte)((r<<3)|(r>>2));result[row,3]=255;}
    }
}
