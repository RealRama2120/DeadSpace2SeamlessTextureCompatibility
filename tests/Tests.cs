using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using DeadSpaceTextureLauncher;
internal static class Tests {
    static int count;
    public sealed class Golden {public string Name{get;set;}public uint Format{get;set;}public uint Crc{get;set;}public byte[] Data{get;set;}}
    static byte[] Encrypt(byte[] input,uint crc) {
        uint a=0x12345678,b=0x23456789,c=0x34567890;
        Action<byte> keys=delegate(byte p){unchecked{a=TpfValidator.Update(a,p);b=(b+(a&255))*134775813+1;c=TpfValidator.Update(c,(byte)(b>>24));}};
        foreach(byte p in TpfValidator.Password)keys(p);
        byte[] result=new byte[input.Length+12];byte[] header=new byte[12];header[10]=(byte)((crc>>16)&255);header[11]=(byte)(crc>>24);
        for(int i=0;i<result.Length;i++){byte p=i<12?header[i]:input[i-12];unchecked{uint t=c|2;result[i]=(byte)(p^((t*(t^1))>>8));}keys(p);}return result;
    }
    static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);count++;}
    static byte[] Bitmap(byte red,byte green,byte blue){using(var ms=new MemoryStream())using(var w=new BinaryWriter(ms)){
        w.Write(0x20534444);w.Write(124);w.Write(0x81007);w.Write(64);w.Write(64);w.Write(2048);w.Write(0);w.Write(1);w.Write(new byte[44]);
        w.Write(32);w.Write(4);w.Write(0x31545844);w.Write(new byte[20]);w.Write(0x1000);w.Write(new byte[16]);
        ushort color=(ushort)(((red>>3)<<11)|((green>>2)<<5)|(blue>>3));
        for(int i=0;i<16*16;i++){w.Write(color);w.Write((ushort)0);w.Write(0U);}return ms.ToArray();}}
    internal static void Fixture(string path,byte red,byte green,byte blue,string hash="0x12345678") {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        byte[] data;
        using(var memory=new MemoryStream())using(var w=new BinaryWriter(memory))using(var directory=new MemoryStream())using(var cw=new BinaryWriter(directory)) {
            string textureName="DEADSPACE2.EXE_"+hash+".dds";
            string[] names={textureName,"texmod.def"};byte[][] contents={Bitmap(red,green,blue),Encoding.ASCII.GetBytes(hash+"|"+textureName+"\r\n\0")};
            for(int j=0;j<2;j++) {
                byte[] content=contents[j],name=Encoding.ASCII.GetBytes(names[j]),compressed;uint crc=0xffffffff;foreach(byte p in content)crc=TpfValidator.Update(crc,p);crc^=0xffffffff;
                using(var cm=new MemoryStream()){using(var def=new DeflateStream(cm,CompressionLevel.Optimal,true))def.Write(content,0,content.Length);compressed=Encrypt(cm.ToArray(),crc);}
                uint offset=(uint)memory.Position;
                w.Write(0x04034b50U);w.Write((ushort)20);w.Write((ushort)1);w.Write((ushort)8);w.Write(0U);w.Write(crc);w.Write(compressed.Length);w.Write(content.Length);w.Write((ushort)name.Length);w.Write((ushort)0);w.Write(name);w.Write(compressed);
                cw.Write(0x02014b50U);cw.Write((ushort)20);cw.Write((ushort)20);cw.Write((ushort)1);cw.Write((ushort)8);cw.Write(0U);cw.Write(crc);cw.Write(compressed.Length);cw.Write(content.Length);cw.Write((ushort)name.Length);cw.Write((ushort)0);cw.Write((ushort)0);cw.Write((ushort)0);cw.Write((ushort)0);cw.Write(0U);cw.Write(offset);cw.Write(name);
            }
            uint start=(uint)memory.Position;w.Write(directory.ToArray());byte[] comment=Encoding.ASCII.GetBytes("Rama2120\nDiagnostic\n");
            w.Write(0x06054b50U);w.Write(0U);w.Write((ushort)2);w.Write((ushort)2);w.Write((uint)directory.Length);w.Write(start);w.Write((ushort)comment.Length);w.Write(comment);data=memory.ToArray();
        }
        for(int i=0;i<data.Length;i++)data[i]^=(byte)(i>=data.Length/4*4?0xa4:((i&1)==0?0xa4:0x3f));
        File.WriteAllBytes(path,data);
    }
    static int Main(string[] args){try{
        // Reference pixels decoded independently with Pillow, then hashed with zlib.
        var golden=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Golden[]>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"dds-golden.json")));
        foreach(var example in golden){var decoder=new DdsPixels(example.Format);foreach(byte value in example.Data)decoder.Add(value);Check(decoder.Crc==example.Crc,example.Name+" matches independent pixel reference");}
        string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);AppInfo.DataDirectory=Path.Combine(root,"logs");Log.Init();
        string trace=Path.Combine(root,"tail.log");var tail=new RuntimeLogTail();var records=new System.Collections.Generic.List<string>();
        File.WriteAllText(trace,"first\r\npart",Encoding.ASCII);tail.Poll(trace,records.Add);tail.Poll(trace,records.Add);
        File.AppendAllText(trace,"ial\r\n",Encoding.ASCII);tail.Poll(trace,records.Add);
        Check(records.SequenceEqual(new[]{"first","partial"}),"runtime tail preserves partial records without replaying history");
        File.WriteAllText(trace,"reset\n",Encoding.ASCII);tail.Poll(trace,records.Add);Check(records.Last()=="reset","runtime tail recovers from truncation");
        using(var writer=new StreamWriter(trace,false,Encoding.ASCII)){for(int i=0;i<14000;i++)writer.WriteLine(new string('x',8192));writer.WriteLine("final");}
        tail=new RuntimeLogTail();records.Clear();tail.Poll(trace,records.Add);
        Check(tail.Position==RuntimeLogTail.ReadBudget&&records.Count==0,"large runtime logs use bounded reads and discard oversized records");
        while(tail.Position<new FileInfo(trace).Length)tail.Poll(trace,records.Add);
        Check(records.SequenceEqual(new[]{"final"}),"runtime tail reaches final record after more than 100 MiB of trace");
        string folder=Path.Combine(root,"TexMod Packages");Directory.CreateDirectory(folder);
        Check(PackageCatalog.Scan(Path.Combine(root,"empty installation"),new Settings()).Count==0,"no-package installation has an empty catalog");
        Check(PackageCatalog.DescribeRttSet(new Package[0])==null,"unrelated installations do not claim Return to Titan coverage");
        string partialRtt=PackageCatalog.DescribeRttSet(new[]{new Package{Status="selected",Sha256="f44c57f1b8cb58dcb2e10814b2c351592d05370545b1c155bfdf6758b41013e2"}});
        Check(partialRtt.Contains("selected=1/10")&&partialRtt.Contains("1-4KMainSuitsnew.tpf")&&partialRtt.Contains("2Kaio.tpf"),"partial Return to Titan set names missing packs");
        Fixture(Path.Combine(folder,"a","same.tpf"),255,0,255);Fixture(Path.Combine(folder,"b","same.tpf"),0,255,255);
        TpfValidator.Validate(Path.Combine(folder,"a","same.tpf"));Check(true,"valid TPF definition, bitmap and CRC");
        File.Copy(Path.Combine(folder,"a","same.tpf"),Path.Combine(folder,"duplicate.tpf"));
        File.WriteAllText(Path.Combine(folder,"__folder_managed_by_vortex"),"metadata");File.WriteAllText(Path.Combine(folder,"not-a-tpf.zip"),"archive");
        var settings=new Settings();var packages=PackageCatalog.Scan(root,settings);
        Check(packages.Count==3,"ignore metadata and unrelated archives");Check(packages.Count(p=>p.Status=="selected")==2,"retain same filename with different content; collapse byte-identical copies");
        settings.Disabled=new[]{"TexMod Packages/a/same.tpf"};packages=PackageCatalog.Scan(root,settings);
        Check(packages.Single(p=>p.RelativePath=="TexMod Packages/a/same.tpf").Status=="disabled","relative-path disable does not disable other same-named files");
        settings.Priority["TexMod Packages/b/same.tpf"]=123;packages=PackageCatalog.Scan(root,settings);Check(packages.Single(p=>p.RelativePath=="TexMod Packages/b/same.tpf").Priority==123,"optional exact priority rule");
        Fixture(Path.Combine(folder,"later.tpf"),22,44,66);Check(PackageCatalog.Scan(root,settings).Count==4,"rescan discovers newly added package");
        string unicodePack=Path.Combine(folder,"Folder With Spaces \u03A9","Panel \u0142.tpf");Fixture(unicodePack,42,84,126);
        packages=PackageCatalog.Scan(root,settings);
        Check(packages.Any(p=>p.Path==unicodePack&&p.Status=="selected"),"package discovery and validation handle spaces and Unicode paths");
        File.WriteAllText(Path.Combine(folder,"broken.tpf"),"broken");Check(PackageCatalog.Scan(root,settings).Single(p=>p.RelativePath.EndsWith("broken.tpf")).Status=="rejected","corrupt file rejected and reported");
        var corrupt=File.ReadAllBytes(Path.Combine(folder,"later.tpf"));corrupt[70]^=0x40;File.WriteAllBytes(Path.Combine(folder,"corrupt-data.tpf"),corrupt);
        Check(PackageCatalog.Scan(root,settings).Single(p=>p.RelativePath.EndsWith("corrupt-data.tpf")).Status=="rejected","damaged payload rejected");
        settings.PackageFolders=new[]{".."};bool blocked=false;try{PackageCatalog.Scan(root,settings);}catch(LauncherException){blocked=true;}Check(blocked,"folder traversal rejected");
        string diagnostic=Path.Combine(root,"diagnostic-packages");
        // Static audit hash of front-end neuron diffuse texture; actual runtime appearance
        // must still be verified, including the possibility of transformed hashes.
        Fixture(Path.Combine(diagnostic,"Diagnostic-Magenta.tpf"),255,0,255,"0x89637591");
        Fixture(Path.Combine(diagnostic,"Diagnostic-Cyan.tpf"),0,255,255,"0x89637591");
        uint probeHash=0xffffffff;for(int i=0;i<64*64;i++)foreach(byte b in new byte[]{0,0,255,255})probeHash=TpfValidator.Update(probeHash,b);
        Fixture(Path.Combine(root,"graphics-packages","Probe-Magenta.tpf"),255,0,255,"0x"+probeHash.ToString("X8"));
        Fixture(Path.Combine(root,"graphics-packages","Probe-Cyan.tpf"),0,255,255,"0x"+probeHash.ToString("X8"));
        string author=Path.Combine(root,"author-inputs");Directory.CreateDirectory(author);File.WriteAllBytes(Path.Combine(author,"magenta.dds"),Bitmap(255,0,255));
        File.WriteAllText(Path.Combine(author,"texmod.def"),"0x"+probeHash.ToString("X8")+"|"+Path.Combine(author,"magenta.dds")+"\r\n",Encoding.ASCII);
        Console.WriteLine("TOTAL "+count);return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
