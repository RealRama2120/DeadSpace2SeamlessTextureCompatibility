#pragma once
// Same canonical block-order BGRA representation as the managed package reader.
static DWORD PixelBlockCrc(const BYTE* block,DWORD format,DWORD crc) {
    int offset=format==D3DFMT_DXT1?0:8;WORD a,b;DWORD picks;memcpy(&a,block+offset,2);memcpy(&b,block+offset+2,2);memcpy(&picks,block+offset+4,4);
    BYTE colors[4][4]={};WORD words[]={a,b};for(int i=0;i<2;i++){unsigned v=words[i],blue=v&31,green=(v>>5)&63,red=(v>>11)&31;colors[i][0]=static_cast<BYTE>((blue<<3)|(blue>>2));colors[i][1]=static_cast<BYTE>((green<<2)|(green>>4));colors[i][2]=static_cast<BYTE>((red<<3)|(red>>2));colors[i][3]=255;}
    bool four=a>b||offset==8;for(int c=0;c<3;c++){colors[2][c]=static_cast<BYTE>(four?(2*colors[0][c]+colors[1][c])/3:(colors[0][c]+colors[1][c])/2);colors[3][c]=static_cast<BYTE>(four?(colors[0][c]+2*colors[1][c])/3:0);}colors[2][3]=255;colors[3][3]=four?255:0;
    BYTE alpha[8]={block[0],block[1]};ULONGLONG bits=0;if(format==D3DFMT_DXT5){if(alpha[0]>alpha[1])for(int i=2;i<8;i++)alpha[i]=static_cast<BYTE>(((8-i)*alpha[0]+(i-1)*alpha[1])/7);else{for(int i=2;i<6;i++)alpha[i]=static_cast<BYTE>(((6-i)*alpha[0]+(i-1)*alpha[1])/5);alpha[7]=255;}for(int i=0;i<6;i++)bits|=static_cast<ULONGLONG>(block[2+i])<<(8*i);}
    for(int i=0;i<16;i++){BYTE pixel[4];memcpy(pixel,colors[(picks>>(2*i))&3],4);if(format==D3DFMT_DXT3)pixel[3]=static_cast<BYTE>(((block[i/2]>>((i&1)*4))&15)*17);else if(format==D3DFMT_DXT5)pixel[3]=alpha[(bits>>(3*i))&7];crc=Crc(pixel,4,crc);}return crc;
}
