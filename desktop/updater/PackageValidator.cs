using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace GentlyPick.Updates {
    public static class PackageValidator {
        static PackageValidator() {
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);
            AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
        }
        const long MaxFile = 1024L*1024*1024, MaxTotal = 3L*1024*1024*1024;
        static readonly HashSet<string> Root = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "轻摘.exe", "Updater.exe", "轻摘更新.exe", "Microsoft.Web.WebView2.Core.dll", "Microsoft.Web.WebView2.WinForms.dll",
            "WebView2Loader.dll", "version.json", "README.md", "THIRD-PARTY-NOTICES.md", "启动轻摘.cmd", "导入旧资料.cmd", "导入旧资料.ps1"
        };
        public static void VerifyDownload(string file, long expectedSize, string expectedSha) {
            if (new FileInfo(file).Length != expectedSize) throw new InvalidDataException("更新包大小不符。");
            string hash;
            using(var s=File.OpenRead(file)) using(var h=SHA256.Create()) hash=BitConverter.ToString(h.ComputeHash(s)).Replace("-","");
            if (!hash.Equals(expectedSha,StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("更新包 SHA-256 不符。");
        }
        public static string CheckedPath(string root,string relative) {
            string s=relative.Replace('\\','/');
            if (String.IsNullOrWhiteSpace(s) || s.StartsWith("/") || s.Contains(":") || s.IndexOf('\0')>=0) throw new InvalidDataException("更新包路径无效。");
            string[] parts=s.Split('/');
            foreach(string part in parts) {
                if(part.Length==0 || part=="." || part==".." || part.EndsWith(".") || part.EndsWith(" ") || part.IndexOfAny(Path.GetInvalidFileNameChars())>=0) throw new InvalidDataException("更新包路径无效："+relative);
                string stem=part.Split('.')[0].ToUpperInvariant();
                if(stem=="CON"||stem=="PRN"||stem=="AUX"||stem=="NUL"||System.Text.RegularExpressions.Regex.IsMatch(stem,@"^(COM|LPT)[1-9]$")) throw new InvalidDataException("更新包含 Windows 保留文件名。");
            }
            string[] protectedFolders={"app/plugins/","app/config/","app/cache/","app/media/","app/videos/","app/userdata/","runtime/plugins/","runtime/config/","runtime/cache/"};
            foreach(string folder in protectedFolders)if(s.StartsWith(folder,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新包不能覆盖用户插件或数据："+relative);
            if(s.Equals("app/data.json",StringComparison.OrdinalIgnoreCase)||s.Equals("app/config.json",StringComparison.OrdinalIgnoreCase)||s.Equals("app/adapter-service-data.json",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新包不能覆盖用户配置："+relative);
            if (!(s.StartsWith("app/",StringComparison.OrdinalIgnoreCase)||s.StartsWith("runtime/",StringComparison.OrdinalIgnoreCase)||Root.Contains(s))) throw new InvalidDataException("更新包含不允许的路径："+relative);
            string basePath=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string dest=Path.Combine(basePath,s.Replace('/',Path.DirectorySeparatorChar));
            if(!dest.StartsWith(basePath,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新包路径越界。");
            string current=basePath;
            foreach(string part in parts) { current=Path.Combine(current,part); string probe=LongPath(current);if ((File.Exists(probe)||Directory.Exists(probe)) && (File.GetAttributes(probe)&FileAttributes.ReparsePoint)!=0) throw new InvalidDataException("目标路径包含链接。"); }
            return LongPath(dest);
        }
        static string LongPath(string path) {
            if(path.Length<240 || path.StartsWith(@"\\?\",StringComparison.Ordinal))return path;
            return path.StartsWith(@"\\",StringComparison.Ordinal)?@"\\?\UNC\"+path.Substring(2):@"\\?\"+path;
        }
        public static List<string> Extract(string zip,string stage,Version expected,System.Threading.CancellationToken cancellationToken=default(System.Threading.CancellationToken)) {
            if(Directory.Exists(stage)) { if(Directory.GetFileSystemEntries(stage).Length>0) throw new IOException("暂存目录不是空目录。"); } else Directory.CreateDirectory(stage);
            var files=new List<string>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);long total=0;
            using(var archive=ZipFile.OpenRead(zip)) {
                if(archive.Entries.Count>20000) throw new InvalidDataException("更新包条目过多。");
                foreach(var entry in archive.Entries) {
                    cancellationToken.ThrowIfCancellationRequested();
                    string name=entry.FullName.Replace('\\','/');
                    int unixType=(entry.ExternalAttributes>>16)&0xF000;
                    if(unixType!=0 && unixType!=0x8000 && unixType!=0x4000) throw new InvalidDataException("更新包含链接或特殊文件。");
                    if((entry.ExternalAttributes&0x400)!=0) throw new InvalidDataException("更新包含链接。");
                    bool directory=name.EndsWith("/");
                    string canonical=directory?name.TrimEnd('/'):name;
                    if (!seen.Add(canonical)) throw new InvalidDataException("更新包含重复路径："+canonical);
                    if(directory) {
                        if(canonical=="app"||canonical=="runtime") continue;
                        CheckedPath(stage,canonical+"/placeholder"); continue;
                    }
                    string dest=CheckedPath(stage,name);
                    if(entry.Length<0||entry.Length>MaxFile||total>MaxTotal-entry.Length)throw new InvalidDataException("更新包解压体积超限。");
                    total+=entry.Length;
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    using(var input=entry.Open()) using(var output=new FileStream(dest,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                        byte[] buffer=new byte[65536];long written=0;int n;
                        while((n=input.Read(buffer,0,buffer.Length))>0) { cancellationToken.ThrowIfCancellationRequested();written+=n; if(written>entry.Length)throw new InvalidDataException("解压大小与声明不符。"); output.Write(buffer,0,n); }
                        if(written!=entry.Length)throw new InvalidDataException("更新包文件不完整。");
                    }
                    files.Add(name);
                }
            }
            foreach(string required in new[]{"轻摘.exe","Updater.exe","version.json","app/server.cjs","runtime/node.exe"}) if(!seen.Contains(required)) throw new InvalidDataException("更新包缺少必要文件："+required);
            var version=new JavaScriptSerializer().DeserializeObject(File.ReadAllText(Path.Combine(stage,"version.json"))) as IDictionary<string,object>;
            object value;if(version==null||!version.TryGetValue("version",out value)||ReleaseFeed.ParseVersion(Convert.ToString(value))!=expected)throw new InvalidDataException("更新包内版本与目标版本不一致。");
            return files;
        }
    }
}
