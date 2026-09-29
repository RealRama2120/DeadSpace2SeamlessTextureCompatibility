using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace DeadSpaceTextureLauncher {
    internal sealed class Package {
        public string Path, RelativePath, Sha256, Status, Reason;
        public long Bytes;
        public int Priority;
        public List<TpfValidator.TextureExpected> Textures;
    }
    internal sealed class Settings {
        public bool Enabled = true;
        public bool RecognizeReturnToTitan = true;
        public string TrustedTexModPath = "";
        public Dictionary<string,int> Priority = new Dictionary<string,int>();
        public string[] Disabled = new string[0];
        public string[] PackageFolders = new string[] { "TexMod Packages", "TexturePacks", "Mods" };
        public bool ShowTexMod = false;
        public bool TraceTextures = false;
        public int UiDelayMs = 250;
        public static Settings Read(string root) {
            string path=System.IO.Path.Combine(root,"DS2SeamlessTextures.json");
            Settings settings=File.Exists(path)?new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(path)):new Settings();
            if(settings==null||settings.Priority==null||settings.Disabled==null||settings.PackageFolders==null)
                throw new LauncherException("Invalid DS2SeamlessTextures.json. Restore the supplied defaults.");
            settings.UiDelayMs=Math.Max(100,Math.Min(3000,settings.UiDelayMs));
            return settings;
        }
    }
    internal static class PackageCatalog {
        private static readonly string[] RttOrder={
            "9381007925e905949c3b78b5e4646ca17d18b35182acfbcc9a7d7fd9fc92785e",
            "f44c57f1b8cb58dcb2e10814b2c351592d05370545b1c155bfdf6758b41013e2",
            "dd02b0f7bf784f6f7929d66e5ed69d699739b17e7a8ea0ffef40adaa571af4b5",
            "cfcf76ed1b440b897ccf9a84e420f3b46ae1f5c01e343493c7f349858945f5c5",
            "2a430a0945d606d4d88062875ee6995f1e96486a3a8c6b5b67c76c0cd78bef24",
            "c9b6fa60bf04a19e72750d712e5bd3c6fe8ff831b2fac0f4f648518ec3d4f12e",
            "e2044ddeed381e0a333d6313657308c32e0cd425e581227c914fb50aaa6aab14",
            "a0c6d94020dceb527779268550db1df3700295df464ae4ed403c5565d6b3e45c",
            "00d3aa918f3b3ff768faac552bfb61db2557d76cb529f8ace32af23dc2329a1e",
            "503122ede1da8c81ec93abde5b65a87bc113969cfc3fd874debee17f3b227dfe"
        };
        private static readonly string[] RttNames={
            "1-4KMainSuitsnew.tpf","1WEP_RTTn.tpf","3INTERACTABLES_RTTn.tpf","4INTERACTABLES_RTTn.tpf","5INTERACTABLES_RTTn.tpf",
            "6INTERACTABLES_RTT.tpf","7INTERACTABLES_RTT.tpf","8INTERACTABLES_RTT.tpf","9INTERACTABLES_RTT.tpf","2Kaio.tpf"
        };
        internal static string DescribeRttSet(IEnumerable<Package> packages) {
            var hashes=new HashSet<string>(packages.Where(p=>p.Status=="selected"&&!string.IsNullOrEmpty(p.Sha256)).Select(p=>p.Sha256),StringComparer.OrdinalIgnoreCase);
            int count=RttOrder.Count(hashes.Contains);
            if(count==0)return null;
            var missing=Enumerable.Range(0,RttOrder.Length).Where(i=>!hashes.Contains(RttOrder[i])).Select(i=>RttNames[i]);
            return "RETURN_TO_TITAN selected="+count+"/"+RttOrder.Length+" missing="+(count==RttOrder.Length?"none":string.Join(",",missing));
        }
        // Exact audited content identities, not filename heuristics.
        private static readonly Dictionary<string,string[]> Superseded=new Dictionary<string,string[]> {
            {"1d2d365817fef4850fcd3c416cc5fa60858d53752c670ee8eee5ed7a8d4c9972",new[]{RttOrder[1]}},
            {"ae71e32085f63319bb4b98fa7635aceeab0b6baba2b95c188a02a57caa1c5e76",new[]{RttOrder[2],RttOrder[5]}},
            {"f5f64a5e6a6a53cd63f69a1b39189b7dc98f896176c0cf6147dbbba05931d2ea",new[]{RttOrder[3]}}
        };
        internal static string Hash(string path) {
            using(var input=File.OpenRead(path)) using(var algorithm=SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(input)).Replace("-","").ToLowerInvariant();
        }
        internal static List<Package> Scan(string root,Settings settings) {
            root=System.IO.Path.GetFullPath(root).TrimEnd('\\','/');
            var candidates=new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Collect(root,false,candidates,visited);
            foreach(string folder in settings.PackageFolders) {
                string full=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,folder));
                if(!full.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase))
                    throw new LauncherException("PackageFolders must stay inside the game directory: "+folder);
                Collect(full,true,candidates,visited);
            }
            var result=new List<Package>();
            foreach(string path in candidates) {
                var p=new Package{Path=path,RelativePath=path.Substring(root.Length+1).Replace('\\','/'),Status="discovered",Reason="",Priority=0};
                result.Add(p);Log.Info("DISCOVERED "+p.RelativePath);
                try {
                    using(var file=File.OpenRead(path))p.Bytes=file.Length;
                    p.Sha256=Hash(path);
                    bool disabled=settings.Disabled.Any(rule=>Matches(rule,p));
                    if(disabled){p.Status="disabled";p.Reason="User rule";continue;}
                    p.Textures=TpfValidator.Validate(path);
                    p.Status="selected";
                    int rtt=Array.IndexOf(RttOrder,p.Sha256);
                    if(settings.RecognizeReturnToTitan&&rtt>=0)p.Priority=1000-rtt;
                    foreach(var rule in settings.Priority)if(Matches(rule.Key,p))p.Priority=rule.Value;
                }catch(Exception ex){p.Status="rejected";p.Reason=ex.Message;Log.Warn("REJECTED "+p.RelativePath+": "+ex.Message);}
            }
            var selectedHashes=new HashSet<string>(result.Where(p=>p.Status=="selected").Select(p=>p.Sha256));
            foreach(Package p in result.Where(p=>p.Status=="selected")) {
                string[] replacements;
                if(settings.RecognizeReturnToTitan&&Superseded.TryGetValue(p.Sha256,out replacements)&&replacements.All(h=>selectedHashes.Contains(h))) {
                    p.Status="superseded";p.Reason="Exact audited replacement hashes are available";
                }
            }
            // Keep same-named different content, collapse only byte-identical packages.
            var accepted=new HashSet<string>();
            foreach(Package p in result.Where(p=>p.Status=="selected").OrderByDescending(p=>p.Priority).ThenBy(p=>p.RelativePath,StringComparer.OrdinalIgnoreCase))
                if(!accepted.Add(p.Sha256)){p.Status="duplicate";p.Reason="Byte-identical selected package";}
            foreach(Package p in result)Log.Info(p.Status.ToUpperInvariant()+" "+p.RelativePath+" sha256="+p.Sha256+" priority="+p.Priority+" "+p.Reason);
            return result;
        }
        internal static bool Matches(string rule,Package p) {
            if(rule.StartsWith("sha256:",StringComparison.OrdinalIgnoreCase))return string.Equals(rule.Substring(7),p.Sha256,StringComparison.OrdinalIgnoreCase);
            return string.Equals(rule.Replace('\\','/'),p.RelativePath,StringComparison.OrdinalIgnoreCase);
        }
        private static void Collect(string folder,bool recurse,SortedSet<string> files,HashSet<string> visited) {
            if(!Directory.Exists(folder))return;
            if(!visited.Add(folder))return;
            try {
                foreach(string path in Directory.EnumerateFiles(folder))if(string.Equals(System.IO.Path.GetExtension(path),".tpf",StringComparison.OrdinalIgnoreCase))files.Add(path);
                if(recurse)foreach(string child in Directory.EnumerateDirectories(folder)) {
                    // Vortex file symlinks are followed normally. Directory reparse
                    // points are not traversed, preventing loops and unrelated trees.
                    if((File.GetAttributes(child)&FileAttributes.ReparsePoint)==0)Collect(child,true,files,visited);
                    else Log.Warn("Skipped directory reparse point: "+child);
                }
            }catch(UnauthorizedAccessException ex){throw new LauncherException("Cannot inspect texture folder: "+folder+". "+ex.Message);}
             catch(IOException ex){throw new LauncherException("Cannot read texture folder: "+folder+". "+ex.Message);}
        }
    }
}
