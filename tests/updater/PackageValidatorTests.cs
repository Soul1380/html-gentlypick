using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using GentlyPick.Updates;
class PackageTests {
    static string root;
    static void Reject(Action a) { try { a(); } catch(InvalidDataException) { return; } throw new Exception("unsafe input accepted"); }
    static string Zip(string name, string extra, string version, bool missing) {
        string zip=Path.Combine(root,name+".zip");
        using(var z=ZipFile.Open(zip,ZipArchiveMode.Create)) {
            var entries=new List<string>{"轻摘.exe","Updater.exe","version.json","app/server.cjs","runtime/node.exe"};
            if(missing)entries.Remove("app/server.cjs");
            if(extra!=null)entries.Add(extra);
            foreach(string p in entries) { var e=z.CreateEntry(p); using(var w=new StreamWriter(e.Open())) w.Write(p=="version.json"?"{\"version\":\""+version+"\"}":"test"); }
        }
        return zip;
    }
    static int Main() { AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);try { return Run(); } catch(Exception e) { Console.WriteLine("FAIL: "+e.GetType().FullName+" "+e.Message+" "+e.StackTrace); return 1; } }
    static int Run() {
        root=Path.Combine(Path.GetTempPath(),"gentlypick-stage-test-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try {
            string good=Zip("good",null,"1.2.3",false);
            var files=PackageValidator.Extract(good,Path.Combine(root,"good"),new Version(1,2,3));
            if(files.Count!=5 || File.ReadAllText(Path.Combine(root,"good","app","server.cjs"))!="test") throw new Exception("valid extraction failed");
            string longName="app/"+new string('a',70)+"/"+new string('b',70)+"/"+new string('c',70)+"/file.txt";
            string longStage=Path.Combine(root,"long");PackageValidator.Extract(Zip("long",longName,"1.2.3",false),longStage,new Version(1,2,3));
            if(File.ReadAllText(PackageValidator.CheckedPath(longStage,longName))!="test")throw new Exception("long path failed");
            Reject(()=>PackageValidator.Extract(Zip("missing",null,"1.2.3",true),Path.Combine(root,"missing"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("version",null,"1.2.4",false),Path.Combine(root,"version"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("traversal","app/../../escape","1.2.3",false),Path.Combine(root,"traversal"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("absolute","/app/test","1.2.3",false),Path.Combine(root,"absolute"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("duplicate","APP/SERVER.CJS","1.2.3",false),Path.Combine(root,"duplicate"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("protected","UserData/data.json","1.2.3",false),Path.Combine(root,"protected"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("plugin","app/plugins/personal.js","1.2.3",false),Path.Combine(root,"plugin"),new Version(1,2,3)));
            Reject(()=>PackageValidator.Extract(Zip("config","app/config.json","1.2.3",false),Path.Combine(root,"config"),new Version(1,2,3)));
            string link=Zip("link","app/link","1.2.3",false);
            using(var z=ZipFile.Open(link,ZipArchiveMode.Update)) z.GetEntry("app/link").ExternalAttributes=unchecked((int)0xA1FF0000);
            Reject(()=>PackageValidator.Extract(link,Path.Combine(root,"link"),new Version(1,2,3)));
            Reject(()=>PackageValidator.VerifyDownload(good,new FileInfo(good).Length,new string('0',64)));
            Reject(()=>PackageValidator.VerifyDownload(good,new FileInfo(good).Length+1,new string('0',64)));
            Console.WriteLine("PASS package extraction, size, hash, version, missing and unsafe paths");return 0;
        } finally { Directory.Delete(@"\\?\"+root,true); }
    }
}
