using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace GentlyPick.Updates {
    public sealed class TransactionException : Exception {
        public bool RollbackSucceeded;
        public TransactionException(string message, Exception inner, bool rollbackSucceeded) : base(message,inner) { RollbackSucceeded=rollbackSucceeded; }
    }
    public static class FileTransaction {
        static string Hash(string path) { using(var f=File.OpenRead(path))using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(f)); }
        public static void Run(string root,string stage,string backup,IList<string> files,Action<string> log,Action<int,string> faultForTests) {
            if(log==null)throw new ArgumentNullException("log");
            var originals=new Dictionary<string,bool>(StringComparer.OrdinalIgnoreCase);
            var touched=new List<string>();
            log("检查目标文件占用");
            foreach(string name in files) {
                string dst=PackageValidator.CheckedPath(root,name), src=PackageValidator.CheckedPath(stage,name);
                if(!File.Exists(src))throw new IOException("暂存文件不存在："+name);
                if(File.Exists(dst)) using(var probe=new FileStream(dst,FileMode.Open,FileAccess.ReadWrite,FileShare.None)) { }
            }
            log("开始备份");
            foreach(string name in files) {
                string dst=PackageValidator.CheckedPath(root,name), saved=PackageValidator.CheckedPath(backup,name);
                bool exists=File.Exists(dst);originals.Add(name,exists);
                log("备份前："+name);
                if(exists) { Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(dst,saved,false);if(Hash(dst)!=Hash(saved))throw new IOException("备份校验失败："+name); }
                log("备份完成："+name);
            }
            try {
                int index=0;
                foreach(string name in files) {
                    string src=PackageValidator.CheckedPath(stage,name), dst=PackageValidator.CheckedPath(root,name);
                    log("替换前："+name);
                    touched.Add(name);
                    if(faultForTests!=null)faultForTests(index,name);
                    Directory.CreateDirectory(Path.GetDirectoryName(dst));File.Copy(src,dst,true);
                    if(Hash(src)!=Hash(dst))throw new IOException("文件校验失败："+name);
                    log("替换并校验完成："+name);index++;
                }
            } catch(Exception failure) {
                bool restored=true;var errors=new List<string>();
                for(int i=touched.Count-1;i>=0;i--) {
                    string name=touched[i];
                    try {
                        log("回滚前："+name);
                        string dst=PackageValidator.CheckedPath(root,name), saved=PackageValidator.CheckedPath(backup,name);
                        if(originals[name]) { File.Copy(saved,dst,true);if(Hash(saved)!=Hash(dst))throw new IOException("回滚校验失败"); }
                        else if(File.Exists(dst))File.Delete(dst);
                        log("回滚完成："+name);
                    } catch(Exception e) { restored=false;errors.Add(name+": "+e.Message);try{log("回滚失败："+name+" "+e.Message);}catch{} }
                }
                throw new TransactionException(restored?"替换失败，已恢复旧文件。":"替换失败且部分文件未能恢复："+String.Join("; ",errors),failure,restored);
            }
        }
    }
}
