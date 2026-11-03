using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GentlyPick.Updates;
class RealAppUpdateTests {
    static string Hash(string p){using(var f=File.OpenRead(p))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(f)).Replace("-","").ToLowerInvariant();}
    static int Main(string[] args){AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);try{return Run(args);}catch(Exception e){Console.WriteLine("FAIL "+e.GetType().Name+": "+e.Message);return 1;}}
    static string ReadLog(string file){try{return File.ReadAllText(file);}catch(IOException){return "";}}
    static int Run(string[] args){
        string here=AppDomain.CurrentDomain.BaseDirectory,root=args.Length>0?Path.GetFullPath(args[0]):Path.Combine(here,"build","real-test-install"),zip=args.Length>1?Path.GetFullPath(args[1]):Path.Combine(here,"build","LightExcerpt-win-x64-update-next2.zip"),main=Path.Combine(root,"轻摘.exe"),logPath=Path.Combine(root,"UserData","startup.log");
        string safe=Path.GetFullPath(Path.Combine(here,"build")).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!root.StartsWith(safe,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("real application test must use the isolated build directory");
        if(!File.Exists(main)||!File.Exists(zip))throw new Exception("test files missing");
        if(File.Exists(logPath))File.Delete(logPath);
        Process old=null;TcpListener server=null;string runLog="";
        try {
            old=Process.Start(new ProcessStartInfo(main){UseShellExecute=true,WorkingDirectory=root,WindowStyle=ProcessWindowStyle.Hidden});
            var sw=Stopwatch.StartNew();while(!ReadLog(logPath).Contains("启动完成")&&sw.Elapsed<TimeSpan.FromSeconds(45)){if(old.HasExited)throw new Exception("real app exited during startup");Thread.Sleep(200);}
            if(!ReadLog(logPath).Contains("启动完成"))throw new Exception("real app startup did not complete");
            old.Refresh();if(old.MainWindowHandle==IntPtr.Zero)throw new Exception("real app window is not available");
            var parent=new ProcessIdentity(old.Id,old.StartTime.ToUniversalTime().Ticks,main);
            server=new TcpListener(IPAddress.Loopback,0);server.Start();int port=((IPEndPoint)server.LocalEndpoint).Port;
            var serve=Task.Run(()=>{using(var c=server.AcceptTcpClient())using(var s=c.GetStream())using(var file=File.OpenRead(zip)) {byte[] req=new byte[4096];s.Read(req,0,req.Length);byte[] head=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: "+file.Length+"\r\nConnection: close\r\n\r\n");s.Write(head,0,head.Length);file.CopyTo(s);}});
            var release=new ReleaseInfo{Version=new Version(1,0,1),AssetUrl="http://127.0.0.1:"+port+"/package.zip",Sha256=Hash(zip),Size=new FileInfo(zip).Length};
            UpdateResult result;using(var log=new UpdateLog(Path.Combine(here,"build","real-test-logs"),Path.Combine(here,"build","real-test-log-fallback"))){runLog=log.PathName;result=UpdateCoordinator.Run(root,release,parent,(s,p)=>{},log,true).GetAwaiter().GetResult();}
            serve.GetAwaiter().GetResult();server.Stop();server=null;
            if(!result.Installed||!result.Restarted)throw new Exception(result.Message+"; log="+runLog);
            if(!old.WaitForExit(1000))throw new Exception("old app still running");
            sw.Restart();while(sw.Elapsed<TimeSpan.FromSeconds(45)){string content=ReadLog(logPath);if(content.Split(new[]{"启动完成"},StringSplitOptions.None).Length>=3)break;Thread.Sleep(200);}
            if(ReadLog(logPath).Split(new[]{"启动完成"},StringSplitOptions.None).Length<3)throw new Exception("restarted application did not complete startup; log="+runLog);
            if(!File.ReadAllText(Path.Combine(root,"version.json")).Contains("1.0.1"))throw new Exception("installed version incorrect");
            if(File.ReadAllText(Path.Combine(root,"UserData","preserve-test.txt"))!="KEEP")throw new Exception("personal data was changed");
            Console.WriteLine("PASS real application start, verified download, process exit, replacement, restart and data preservation");return 0;
        } finally {
            if(server!=null)server.Stop();
            foreach(var p in Process.GetProcessesByName("轻摘"))using(p){try{if(String.Equals(p.MainModule.FileName,main,StringComparison.OrdinalIgnoreCase)){if(!p.CloseMainWindow()||!p.WaitForExit(5000))p.Kill();}}catch{}}
            if(old!=null)old.Dispose();
        }
    }
}
