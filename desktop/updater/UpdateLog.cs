using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace GentlyPick.Updates {
    public sealed class UpdateLog : IDisposable {
        readonly StreamWriter writer;
        public string PathName { get; private set; }
        public bool UsingFallback { get; private set; }
        public UpdateLog(string preferredDirectory,string fallbackDirectory) {
            Exception primary=null;
            foreach(string dir in new[]{preferredDirectory,fallbackDirectory}) {
                try {
                    Directory.CreateDirectory(dir);
                    string file=Path.Combine(dir,"update-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".log");
                    writer=new StreamWriter(new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false));writer.AutoFlush=true;
                    PathName=file;UsingFallback=primary!=null;
                    Write("更新器启动。"+(UsingFallback?"首选日志目录不可用，已使用备用目录。":""));return;
                } catch(Exception e) { primary=e; }
            }
            throw new IOException("无法写入更新日志；更新已停止。",primary);
        }
        public void Write(string text) {
            string safe=Regex.Replace(text??"",@"(?i)(authorization\s*[:=]\s*)(?:bearer\s+)?[^\s&]+","$1[redacted]");
            safe=Regex.Replace(safe,@"(?i)(https?://[^\s?#]+)\?[^\s]+","$1?[redacted]");
            safe=Regex.Replace(safe,@"(?i)(token|password|api[_-]?key|secret)(\s*[=:]\s*|\s+)([^\s&]+)","$1$2[redacted]");
            safe=Regex.Replace(safe,@"(?i)([?&](?:token|key|signature|sig|auth|password)=[^&\s]+)","[redacted]");
            writer.WriteLine(DateTime.UtcNow.ToString("o")+" "+safe);
        }
        public void Dispose() { writer.Dispose(); }
    }
}
