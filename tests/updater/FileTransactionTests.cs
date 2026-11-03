using System;
using System.IO;
using System.Collections.Generic;
using GentlyPick.Updates;
class TransactionTests {
    static void Put(string root,string name,string value) { string p=Path.Combine(root,name);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllText(p,value); }
    static int Main() {
        string root=Path.Combine(Path.GetTempPath(),"gentlypick-install-test-"+Guid.NewGuid().ToString("N"));
        string install=Path.Combine(root,"install"),stage=Path.Combine(root,"stage"),backup=Path.Combine(root,"backup");
        Directory.CreateDirectory(root);
        var files=new List<string>{"app/a.txt","app/b.txt"};
        try {
            Put(install,files[0],"old-a");Put(install,files[1],"old-b");Put(install,"UserData/data.json","personal");Put(install,"app/plugins/personal.js","plugin");Put(install,"app/config.json","config");
            Put(stage,files[0],"new-a");Put(stage,files[1],"new-b");
            using(var locked=new FileStream(Path.Combine(install,files[0]),FileMode.Open,FileAccess.ReadWrite,FileShare.None)) {
                bool failed=false;try { FileTransaction.Run(install,stage,backup,files,s=>{},null); } catch(IOException) { failed=true; }
                if(!failed)throw new Exception("locked file accepted");
            }
            bool rolled=false;
            try { FileTransaction.Run(install,stage,backup,files,s=>{},(i,n)=>{if(i==1)throw new IOException("injected failure");}); }
            catch(TransactionException e) { rolled=e.RollbackSucceeded; }
            if(!rolled || File.ReadAllText(Path.Combine(install,files[0]))!="old-a" || File.ReadAllText(Path.Combine(install,files[1]))!="old-b")throw new Exception("rollback failed");
            FileTransaction.Run(install,stage,Path.Combine(root,"backup2"),files,s=>{},null);
            if(File.ReadAllText(Path.Combine(install,files[0]))!="new-a" || File.ReadAllText(Path.Combine(install,"UserData/data.json"))!="personal" || File.ReadAllText(Path.Combine(install,"app/plugins/personal.js"))!="plugin" || File.ReadAllText(Path.Combine(install,"app/config.json"))!="config")throw new Exception("install failed");
            Console.WriteLine("PASS occupied file, rollback, successful install and personal data preservation");return 0;
        } finally { Directory.Delete(root,true); }
    }
}
