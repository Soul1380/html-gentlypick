using System;
using System.IO;

namespace GentlyPick.Updates {
    public sealed class UpdateGate : IDisposable {
        readonly FileStream stream;
        UpdateGate(FileStream stream) { this.stream=stream; }
        public static UpdateGate Acquire(string root) {
            string user=Path.Combine(Path.GetFullPath(root),"UserData");
            if(Directory.Exists(user) && (File.GetAttributes(user)&FileAttributes.ReparsePoint)!=0)throw new IOException("数据目录是链接，无法确认更新锁位置。");
            Directory.CreateDirectory(user);
            string updates=Path.Combine(user,"updates");
            if(Directory.Exists(updates) && (File.GetAttributes(updates)&FileAttributes.ReparsePoint)!=0)throw new IOException("更新目录是链接，无法确认更新锁位置。");
            Directory.CreateDirectory(updates);
            string file=Path.Combine(updates,"install.lock");
            if(File.Exists(file) && (File.GetAttributes(file)&FileAttributes.ReparsePoint)!=0)throw new IOException("更新锁是链接，无法使用。");
            try { return new UpdateGate(new FileStream(file,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)); }
            catch(IOException e) { throw new IOException("当前安装目录正在更新或准备启动，请稍后再试。",e); }
        }
        public void Dispose() { stream.Dispose(); }
    }
}
