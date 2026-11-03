using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GentlyPick.Updates;
class EndToEndTests {
    static void Put(string root,string name,string value){string p=Path.Combine(root,name);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllText(p,value);}
    static string Hash(string p){using(var f=File.OpenRead(p))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static int Main(string[] args){try{return Run(args.Length>0?args[0]:"");}catch(Exception e){Console.WriteLine("FAIL "+e.GetType().Name+": "+e.Message);return 1;}}
    static int Run(string mode){
        string work=Path.Combine(Path.GetTempPath(),"gentlypick-e2e-"+Guid.NewGuid().ToString("N")),root=Path.Combine(work,"install"),payload=Path.Combine(work,"payload"),zip=Path.Combine(work,"update.zip");Directory.CreateDirectory(root);Directory.CreateDirectory(payload);
        Process old=null,restarted=null;TcpListener server=null;
        try {
            string fixture=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"FixtureApp.exe");File.Copy(fixture,Path.Combine(root,"轻摘.exe"));
            Put(root,"version.json","{\"version\":\"1.0.0\"}");Put(root,"app/server.cjs","old");Put(root,"UserData/data.json","private-data");
            File.Copy(fixture,Path.Combine(payload,"轻摘.exe"));File.Copy(fixture,Path.Combine(payload,"Updater.exe"));Put(payload,"version.json","{\"version\":\"1.0.1\"}");Put(payload,"app/server.cjs","new");Put(payload,"runtime/node.exe","node fixture");ZipFile.CreateFromDirectory(payload,zip);
            old=Process.Start(new ProcessStartInfo(Path.Combine(root,"轻摘.exe")){UseShellExecute=false,WorkingDirectory=root,WindowStyle=ProcessWindowStyle.Hidden});
            var sw=Stopwatch.StartNew();while(!File.Exists(Path.Combine(root,"boot.txt"))&&sw.Elapsed<TimeSpan.FromSeconds(12)){if(old.HasExited)throw new Exception("fixture exited during startup");Thread.Sleep(100);}
            if(!File.Exists(Path.Combine(root,"boot.txt")))throw new Exception("fixture did not start");
            old.Refresh();if(old.MainWindowHandle==IntPtr.Zero)throw new Exception("fixture has no accessible main window");
            var parent=new ProcessIdentity(old.Id,old.StartTime.ToUniversalTime().Ticks,Path.Combine(root,"轻摘.exe"));
            server=new TcpListener(IPAddress.Loopback,0);server.Start();int port=((IPEndPoint)server.LocalEndpoint).Port;byte[] data=File.ReadAllBytes(zip);
            var serve=Task.Run(()=>{using(var c=server.AcceptTcpClient())using(var s=c.GetStream()){byte[] req=new byte[4096];s.Read(req,0,req.Length);byte[] head=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: "+data.Length+"\r\nConnection: close\r\n\r\n");s.Write(head,0,head.Length);s.Write(data,0,data.Length);}});
            var release=new ReleaseInfo{Version=new Version(1,0,1),AssetUrl="http://127.0.0.1:"+port+"/package.zip",Sha256=Hash(zip),Size=data.Length};
            UpdateResult result;using(var log=new UpdateLog(Path.Combine(work,"logs"),Path.Combine(work,"fallback")))result=UpdateCoordinator.Run(root,release,parent,(s,p)=>{if(s=="安装完成，正在重新启动"){if(mode=="--restart-failure")File.Delete(Path.Combine(root,"轻摘.exe"));if(mode=="--startup-failure")File.WriteAllText(Path.Combine(root,"fail-start.txt"),"fail");}},log,true).GetAwaiter().GetResult();
            serve.GetAwaiter().GetResult();server.Stop();server=null;
            if(mode=="--restart-failure"||mode=="--startup-failure"){if(!result.Installed||result.Restarted||!result.Message.Contains("安装已成功"))throw new Exception("restart failure was not distinguished");if(File.ReadAllText(Path.Combine(root,"app/server.cjs"))!="new")throw new Exception("installed content rolled back after restart failure");Console.WriteLine("PASS installed update with restart or startup failure reported separately");return 0;}
            if(!result.Installed||!result.Restarted)throw new Exception(result.Message);
            if(!old.WaitForExit(1000))throw new Exception("old process still running");
            sw.Restart();string marker="";while(sw.Elapsed<TimeSpan.FromSeconds(12)){try{marker=File.ReadAllText(Path.Combine(root,"boot.txt"));if(marker.Contains("1.0.1"))break;}catch(IOException){}Thread.Sleep(100);}
            if(!marker.Contains("1.0.1"))throw new Exception("new process did not signal startup");
            int pid=int.Parse(marker.Split(' ')[0]);restarted=Process.GetProcessById(pid);
            if(!String.Equals(restarted.MainModule.FileName,Path.Combine(root,"轻摘.exe"),StringComparison.OrdinalIgnoreCase))throw new Exception("restart path mismatch");
            if(File.ReadAllText(Path.Combine(root,"app/server.cjs"))!="new"||File.ReadAllText(Path.Combine(root,"UserData/data.json"))!="private-data")throw new Exception("file or user data mismatch");
            Console.WriteLine("PASS isolated running process, download, exit, replacement and restart");return 0;
        } finally {
            if(server!=null)server.Stop();
            if(restarted!=null){try{if(!restarted.HasExited){restarted.CloseMainWindow();if(!restarted.WaitForExit(2000))restarted.Kill();}}catch{}}
            if(old!=null){try{if(!old.HasExited){old.CloseMainWindow();if(!old.WaitForExit(2000))old.Kill();}}catch{}}
            try{Directory.Delete(work,true);}catch{}
        }
    }
}
