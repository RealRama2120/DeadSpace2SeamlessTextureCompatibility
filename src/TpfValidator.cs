using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DeadSpaceTextureLauncher {
    // Streaming integrity validation; never extracts a package into the game.
    internal static class TpfValidator {
        internal sealed class TextureExpected {
            public uint TargetHash,PayloadHash,Width,Height,Format;
            public long TopBytes;
            public bool Supported;
            public string Name;
        }
        private static TextureExpected DescribeDds(byte[] h,string name) {
            var e=new TextureExpected{Name=name};
            if(h.Length<128||BitConverter.ToUInt32(h,0)!=0x20534444||BitConverter.ToUInt32(h,4)!=124)return e;
            e.Height=BitConverter.ToUInt32(h,12);e.Width=BitConverter.ToUInt32(h,16);
            uint flags=BitConverter.ToUInt32(h,80),fourcc=BitConverter.ToUInt32(h,84);
            if(e.Width==0||e.Height==0||e.Width>16384||e.Height>16384||BitConverter.ToUInt32(h,24)>1||BitConverter.ToUInt32(h,112)!=0)return e;
            if((flags&4)!=0&&(fourcc==0x31545844||fourcc==0x33545844||fourcc==0x35545844)){e.Format=fourcc;e.TopBytes=((long)e.Width+3)/4*((e.Height+3)/4)*(fourcc==0x31545844?8:16);}
            else if((flags&0x40)!=0&&BitConverter.ToUInt32(h,88)==32&&BitConverter.ToUInt32(h,92)==0xff0000&&BitConverter.ToUInt32(h,96)==0xff00&&BitConverter.ToUInt32(h,100)==0xff){e.Format=(flags&1)!=0?21U:22U;e.TopBytes=(long)e.Width*e.Height*4;}
            e.Supported=e.TopBytes>0;return e;
        }
        internal static readonly byte[] Password={0x73,0x2a,0x63,0x7d,0x5f,0x0a,0xa6,0xbd,0x7d,0x65,0x7e,0x67,0x61,0x2a,0x7f,0x7f,0x74,0x61,0x67,0x5b,0x60,0x70,0x45,0x74,0x5c,0x22,0x74,0x5d,0x6e,0x6a,0x73,0x41,0x77,0x6e,0x46,0x47,0x77,0x49,0x0c,0x4b,0x46,0x6f};
        private static readonly uint[] Table=MakeTable();
        private static uint[] MakeTable(){var t=new uint[256];for(uint i=0;i<256;i++){uint c=i;for(int j=0;j<8;j++)c=(c&1)!=0?0xedb88320U^(c>>1):c>>1;t[i]=c;}return t;}
        internal static uint Update(uint crc,byte b){return Table[(crc^b)&255]^(crc>>8);}
        internal sealed class Entry {internal string Name;internal uint Size,Compressed,Crc,Offset;internal ushort Flags,Method;}
        internal static List<TextureExpected> Validate(string path) {
            using(var stream=new XorStream(File.OpenRead(path)))using(var reader=new BinaryReader(stream)) {
                if(stream.Length<22||reader.ReadUInt32()!=0x04034b50)throw new InvalidDataException("Not a TexMod package (invalid header)");
                long tail=Math.Max(0,stream.Length-131072);stream.Position=tail;byte[] data=reader.ReadBytes((int)(stream.Length-tail));
                int end=-1;for(int i=data.Length-22;i>=0;i--)if(BitConverter.ToUInt32(data,i)==0x06054b50){end=i;break;}
                if(end<0)throw new InvalidDataException("Truncated TPF: no ZIP directory");
                int count=BitConverter.ToUInt16(data,end+10);uint dirSize=BitConverter.ToUInt32(data,end+12),offset=BitConverter.ToUInt32(data,end+16);
                if(count<1||count==65535||(long)offset+dirSize>tail+end)throw new InvalidDataException("Invalid or unsupported TPF directory");
                stream.Position=offset;var entries=new List<Entry>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for(int i=0;i<count;i++) {
                    if(reader.ReadUInt32()!=0x02014b50)throw new InvalidDataException("Broken TPF directory entry");
                    reader.ReadUInt32();var e=new Entry();e.Flags=reader.ReadUInt16();e.Method=reader.ReadUInt16();reader.ReadUInt32();
                    e.Crc=reader.ReadUInt32();e.Compressed=reader.ReadUInt32();e.Size=reader.ReadUInt32();int len=reader.ReadUInt16(),extra=reader.ReadUInt16(),comment=reader.ReadUInt16();
                    reader.ReadUInt16();reader.ReadUInt16();reader.ReadUInt32();e.Offset=reader.ReadUInt32();
                    e.Name=Encoding.UTF8.GetString(reader.ReadBytes(len)).Replace('\\','/');stream.Position+=extra+comment;
                    if(e.Method!=0&&e.Method!=8)throw new InvalidDataException("Unsupported TPF compression");
                    if(e.Size>1073741824)throw new InvalidDataException("TPF entry exceeds the 1 GiB validation limit");
                    if(!names.Add(e.Name)) {
                        if(e.Name.Equals("texmod.def",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Ambiguous duplicate texmod.def");
                        Log.Warn("Repeated internal texture entry retained: "+e.Name);
                    }
                    entries.Add(e);
                }
                if(!names.Contains("texmod.def"))throw new InvalidDataException("TPF is missing texmod.def");
                string definition=null;byte[] buffer=new byte[65536];var textures=new Dictionary<string,TextureExpected>(StringComparer.OrdinalIgnoreCase);
                foreach(Entry e in entries) {
                    stream.Position=e.Offset;if(reader.ReadUInt32()!=0x04034b50)throw new InvalidDataException("Broken TPF local header");
                    stream.Position=e.Offset+26;int name=reader.ReadUInt16(),extra=reader.ReadUInt16();stream.Position+=name+extra;
                    if(stream.Position+e.Compressed>offset)throw new InvalidDataException("TPF payload overlaps directory");
                    using(var payload=new PayloadStream(stream,e.Compressed,(e.Flags&1)!=0)) {
                        Stream decoded=e.Method==8?(Stream)new DeflateStream(payload,CompressionMode.Decompress,true):payload;
                        using(var def=new MemoryStream()) {
                            uint crc=0xffffffff;long total=0;bool isDef=e.Name.Equals("texmod.def",StringComparison.OrdinalIgnoreCase);
                            byte[] header=new byte[128];int headerBytes=0;TextureExpected expected=null;DdsPixels pixels=null;
                            int n;while((n=decoded.Read(buffer,0,buffer.Length))>0) {
                                long chunkStart=total;
                                total+=n;if(total>e.Size)throw new InvalidDataException("Expanded TPF entry exceeds declared size");
                                if(!isDef) {
                                    if(headerBytes<128){int copy=Math.Min(n,128-headerBytes);Array.Copy(buffer,0,header,headerBytes,copy);headerBytes+=copy;if(headerBytes==128){expected=DescribeDds(header,e.Name);pixels=new DdsPixels(expected.Format);}}
                                    if(expected!=null&&expected.Supported){int from=(int)Math.Max(0,128-chunkStart),to=(int)Math.Min(n,128+expected.TopBytes-chunkStart);for(int k=from;k<to;k++)pixels.Add(buffer[k]);}
                                }
                                for(int k=0;k<n;k++)crc=Update(crc,buffer[k]);
                                if(isDef){if(total>4*1024*1024)throw new InvalidDataException("TPF definition is too large");def.Write(buffer,0,n);}
                            }
                            if(decoded!=payload)decoded.Dispose();
                            if(total!=e.Size||(crc^0xffffffff)!=e.Crc)throw new InvalidDataException("TPF checksum failed: "+e.Name);
                            if(isDef)definition=Encoding.UTF8.GetString(def.ToArray());
                            else if(expected!=null){expected.PayloadHash=pixels.Crc;expected.Supported&=total>=128+expected.TopBytes;if(!textures.ContainsKey(e.Name))textures.Add(e.Name,expected);else textures[e.Name].Supported=false;}
                        }
                    }
                }
                int mappings=0;var result=new List<TextureExpected>();
                foreach(string raw in (definition??"").Split('\n')) {
                    string line=raw.Trim('\r','\0',' ','\ufeff');if(line.Length==0)continue;
                    int split=line.IndexOf('|');if(split<=0)throw new InvalidDataException("Invalid TPF mapping");
                    string hash=line.Substring(0,split);uint parsed;
                    if(!uint.TryParse(hash.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?hash.Substring(2):hash,hash.StartsWith("0x",StringComparison.OrdinalIgnoreCase)?System.Globalization.NumberStyles.HexNumber:System.Globalization.NumberStyles.Integer,System.Globalization.CultureInfo.InvariantCulture,out parsed))throw new InvalidDataException("Invalid texture hash");
                    if(!names.Contains(line.Substring(split+1).Replace('\\','/')))throw new InvalidDataException("TPF mapping references a missing texture");
                    string imageName=line.Substring(split+1).Replace('\\','/');TextureExpected texture;
                    if(textures.TryGetValue(imageName,out texture))result.Add(new TextureExpected{TargetHash=parsed,PayloadHash=texture.PayloadHash,Width=texture.Width,Height=texture.Height,Format=texture.Format,TopBytes=texture.TopBytes,Name=imageName,Supported=texture.Supported});
                    else result.Add(new TextureExpected{TargetHash=parsed,Name=imageName});
                    mappings++;
                }
                if(mappings==0)throw new InvalidDataException("No texture mappings in TPF");
                return result;
            }
        }
        private sealed class XorStream:Stream {
            private readonly Stream inner;private readonly long length;
            internal XorStream(Stream source){inner=source;length=source.Length;}
            public override int Read(byte[] b,int o,int n){long p=inner.Position;int read=inner.Read(b,o,n);for(int i=0;i<read;i++){long pos=p+i;b[o+i]^=(byte)(pos>=Length/4*4?0xa4:((pos&1)==0?0xa4:0x3f));}return read;}
            public override bool CanRead{get{return true;}}public override bool CanSeek{get{return true;}}public override bool CanWrite{get{return false;}}
            public override long Length{get{return length;}}public override long Position{get{return inner.Position;}set{inner.Position=value;}}
            public override long Seek(long o,SeekOrigin s){return inner.Seek(o,s);}public override void Flush(){}public override void SetLength(long v){throw new NotSupportedException();}public override void Write(byte[] b,int o,int n){throw new NotSupportedException();}
            protected override void Dispose(bool disposing){if(disposing)inner.Dispose();base.Dispose(disposing);}
        }
        private sealed class PayloadStream:Stream {
            private readonly Stream inner;private long remaining;private readonly bool encrypted;private uint a=0x12345678,b=0x23456789,c=0x34567890;
            internal PayloadStream(Stream source,long size,bool decrypt){inner=source;remaining=size;encrypted=decrypt;if(decrypt){foreach(byte p in Password)Keys(p);if(size<12)throw new InvalidDataException("Truncated encrypted header");var h=new byte[12];if(Read(h,0,12)!=12)throw new InvalidDataException("Truncated encrypted payload");}}
            private void Keys(byte value){unchecked{a=Update(a,value);b=(b+(a&255))*134775813+1;c=Update(c,(byte)(b>>24));}}
            public override int Read(byte[] buffer,int offset,int count){int n=inner.Read(buffer,offset,(int)Math.Min(count,remaining));remaining-=n;if(encrypted)for(int i=0;i<n;i++){unchecked{uint t=c|2;byte plain=(byte)(buffer[offset+i]^((t*(t^1))>>8));buffer[offset+i]=plain;Keys(plain);}}return n;}
            public override bool CanRead{get{return true;}}public override bool CanSeek{get{return false;}}public override bool CanWrite{get{return false;}}public override long Length{get{throw new NotSupportedException();}}public override long Position{get{throw new NotSupportedException();}set{throw new NotSupportedException();}}public override long Seek(long o,SeekOrigin s){throw new NotSupportedException();}public override void Flush(){}public override void SetLength(long v){throw new NotSupportedException();}public override void Write(byte[] b,int o,int n){throw new NotSupportedException();}
        }
    }
}
