using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly:System.Reflection.AssemblyTitle("Dead Space 2 Seamless Texture Compatibility")]
[assembly:System.Reflection.AssemblyProduct("Dead Space 2 Seamless Texture Compatibility")]
[assembly:System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0")]

namespace DeadSpaceTextureLauncher {
    internal static class AppInfo {
        public const string Name="Dead Space 2 Seamless Texture Compatibility";
        public const string Version="1.0.0";
        public static bool TestMode {get{return false;}}
        public static string DataDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Rama2120","DS2SeamlessTextures");
        public static string ToolsDirectory{get{return Path.Combine(DataDirectory,"Tools");}}
        public static string ManagedTexModPath{get{return Path.Combine(ToolsDirectory,"TexMod.exe");}}
    }
    internal static class Log {
        private static readonly object Sync=new object();
        internal static string FilePath;
        internal static void Init(){Directory.CreateDirectory(AppInfo.DataDirectory);FilePath=Path.Combine(AppInfo.DataDirectory,"latest.log");File.WriteAllText(FilePath,"DS2STC "+AppInfo.Version+" "+DateTime.UtcNow.ToString("o")+Environment.NewLine);}
        public static void Info(string message){Write("INFO",message);}public static void Warn(string message){Write("WARN",message);}public static void Error(string message){Write("ERROR",message);}
        private static void Write(string level,string message){if(FilePath==null)return;lock(Sync)File.AppendAllText(FilePath,DateTime.UtcNow.ToString("o")+" "+level+" "+message+Environment.NewLine);}
    }
    internal sealed class LauncherSettings {
        public string GamePath,TexModPath;
        public List<string> Packages=new List<string>();
        public int ActionDelayMs=250;
        public bool KeepTexModOutOfSight=true,CloseTexModWithGame=true;
        public int GameWindowTimeoutMinutes=10;
        public int BootstrapProcessId;
        public bool PrepareOnly;
        public string SessionDirectory;
    }
    internal enum PeArchitecture {Unknown,X86,X64}
    internal static class PeInspector {
        public static PeArchitecture GetArchitecture(string path){using(var stream=File.OpenRead(path))using(var r=new BinaryReader(stream)){if(r.ReadUInt16()!=0x5a4d)return PeArchitecture.Unknown;stream.Position=0x3c;uint pe=r.ReadUInt32();if(pe+6>stream.Length)return PeArchitecture.Unknown;stream.Position=pe;if(r.ReadUInt32()!=0x4550)return PeArchitecture.Unknown;ushort m=r.ReadUInt16();return m==0x14c?PeArchitecture.X86:(m==0x8664?PeArchitecture.X64:PeArchitecture.Unknown);}}
        public static string ValidateDeadSpace2(string path){try {if(!Path.GetFileName(path).Equals("deadspace2.exe",StringComparison.OrdinalIgnoreCase)||GetArchitecture(path)!=PeArchitecture.X86||!File.Exists(Path.Combine(Path.GetDirectoryName(path),"DS2DAT0.DAT")))return "Expected the original x86 deadspace2.exe alongside DS2DAT0.DAT.";return null;}catch(Exception ex){return ex.Message;}}
    }
    internal static class Program {
        [STAThread]private static int Main(string[] args) {
            string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');bool scan=false,prepare=false;int parent=0;
            try {
                for(int i=0;i<args.Length;i++) {
                    if(args[i]=="--game-root")root=Path.GetFullPath(args[++i]);
                    else if(args[i]=="--scan-only")scan=true;
                    else if(args[i]=="--prepare-only")prepare=true;
                    else if(args[i]=="--game-pid")parent=int.Parse(args[++i]);
                    else if(args[i]=="--data-root")AppInfo.DataDirectory=Path.GetFullPath(args[++i]);
                    else if(args[i]!="--bootstrap")throw new LauncherException("Unknown argument: "+args[i]);
                }
                string key;using(var sha=System.Security.Cryptography.SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(root.ToUpperInvariant()))).Replace("-","");
                bool created;
                using(var mutex=new Mutex(true,"Local\\DS2STC_"+key,out created)) {
                    if(!created)return 20;
                    Log.Init();Log.Info("GAME_ROOT "+root+" parent="+parent);
                    var settings=Settings.Read(root);
                    if(!settings.Enabled){Log.Info("DISABLED: continuing original launch");return 10;}
                    var packages=PackageCatalog.Scan(root,settings);
                    File.WriteAllText(Path.Combine(AppInfo.DataDirectory,"packages.json"),new JavaScriptSerializer().Serialize(packages));
                    if(scan)return packages.Any(p=>p.Status=="rejected")?2:0;
                    if(packages.Any(p=>p.Status=="rejected"))throw new LauncherException("One or more texture packages failed validation. See packages.json. No texture-enabled launch was attempted.");
                    var selected=packages.Where(p=>p.Status=="selected").OrderByDescending(p=>p.Priority).ThenBy(p=>p.RelativePath,StringComparer.OrdinalIgnoreCase).ToList();
                    string rttStatus=PackageCatalog.DescribeRttSet(selected);if(rttStatus!=null)Log.Info(rttStatus);
                    if(selected.Count==0){Log.Info("NO_PACKAGES: continuing original launch");return 10;}
                    string game=Path.Combine(root,"deadspace2.exe");string error=PeInspector.ValidateDeadSpace2(game);if(error!=null)throw new LauncherException(error);
                    if(parent>0)using(var process=Process.GetProcessById(parent))if(!string.Equals(process.MainModule.FileName,game,StringComparison.OrdinalIgnoreCase))throw new LauncherException("Bootstrap parent does not match the intended game executable.");
                    string texmod=new[]{settings.TrustedTexModPath,Path.Combine(root,"Texmod.exe"),AppInfo.ManagedTexModPath}.FirstOrDefault(p=>!string.IsNullOrWhiteSpace(p)&&TexModAcquisition.IsVerifiedOriginal(p));
                    if(texmod==null) {
                        if(MessageBox.Show("TexMod is not bundled. Download the original TexMod 0.9b from the archived project and verify its pinned checksums?\n\n"+TexModAcquisition.OriginalArchiveUrl,AppInfo.Name,MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)throw new LauncherException("TexMod download was declined. Textures were not enabled.");
                        texmod=TexModAcquisition.DownloadVerified(null);
                    }
                    Environment.SetEnvironmentVariable("DS2STC_TEXMOD_CHILD","1");
                    Environment.SetEnvironmentVariable("DS2STC_TRACE",settings.TraceTextures?"1":null);
                    var runtime=new LauncherSettings{GamePath=game,TexModPath=texmod,Packages=selected.Select(p=>p.Path).ToList(),ActionDelayMs=settings.UiDelayMs,KeepTexModOutOfSight=!settings.ShowTexMod&&!prepare,BootstrapProcessId=parent,PrepareOnly=prepare};
                    Log.Info("PRIORITY_STATUS=TOP_ROW_WINS_VERIFIED_IN_CONTROLLED_D3D9_AND_DGVOODOO_TESTS; game-specific coverage remains separately measured");
                    var guards=new List<FileStream>();
                    try {
                        foreach(var package in selected) {
                            guards.Add(new FileStream(package.Path,FileMode.Open,FileAccess.Read,FileShare.Read));
                            if(PackageCatalog.Hash(package.Path)!=package.Sha256)throw new LauncherException("A texture package changed after validation. Relaunch after deployment finishes: "+package.RelativePath);
                        }
                        if(!prepare){runtime.SessionDirectory=Path.Combine(AppInfo.DataDirectory,"sessions",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(runtime.SessionDirectory);
                            var claimed=new HashSet<uint>();var lines=new List<string>();
                            foreach(var package in selected)foreach(var group in package.Textures.GroupBy(t=>t.TargetHash)){
                                if(!claimed.Add(group.Key))continue;var texture=group.First();bool supported=texture.Supported&&group.All(t=>t.Supported&&t.PayloadHash==texture.PayloadHash&&t.Format==texture.Format&&t.Width==texture.Width&&t.Height==texture.Height);
                                lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0:X8} {1} {2} {3:X8} {4:X8} {5}",texture.TargetHash,texture.Width,texture.Height,texture.Format,texture.PayloadHash,supported?1:0));
                            }
                            File.WriteAllLines(Path.Combine(runtime.SessionDirectory,"expected.txt"),lines,Encoding.ASCII);Environment.SetEnvironmentVariable("DS2STC_SESSION",runtime.SessionDirectory);Log.Info("RUNTIME_EVIDENCE "+runtime.SessionDirectory);
                        }
                        new TexModAutomation(runtime,null).Run(CancellationToken.None);
                    }finally{foreach(var guard in guards)guard.Dispose();}
                    return 0;
                }
            }catch(Exception ex){Log.Error(ex.ToString());if(!scan)MessageBox.Show(ex.Message+"\n\nTextures are not verified.\nLog: "+Log.FilePath,AppInfo.Name,MessageBoxButtons.OK,MessageBoxIcon.Error);return 1;}
        }
    }
}
