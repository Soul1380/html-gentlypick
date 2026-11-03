using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GentlyPick.Updates;
class ShutdownAndGateTests {
    static int Main(){try{return Run();}catch(Exception e){Console.WriteLine("FAIL "+e.Message);return 1;}}
    static int Run(){
        string root=Path.Combine(Path.GetTempPath(),"gentlypick-shutdown-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Process child=null;
        try{
            string exe=Path.Combine(root,"轻摘.exe");File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"FixtureApp.exe"),exe);File.WriteAllText(Path.Combine(root,"version.json"),"{\"version\":\"1.0.0\"}");
            using(var gate=UpdateGate.Acquire(root)) {
                child=Process.Start(new ProcessStartInfo(exe){UseShellExecute=false,WorkingDirectory=root,WindowStyle=ProcessWindowStyle.Hidden});
                if(!child.WaitForExit(5000))throw new Exception("startup was not blocked");
                if(!File.Exists(Path.Combine(root,"gate-refused.txt")))throw new Exception("gate rejection not observed");child.Dispose();child=null;
            }
            child=Process.Start(new ProcessStartInfo(exe,"--refuse-close"){UseShellExecute=false,WorkingDirectory=root,WindowStyle=ProcessWindowStyle.Hidden});
            var sw=Stopwatch.StartNew();while(!File.Exists(Path.Combine(root,"boot.txt"))&&sw.Elapsed<TimeSpan.FromSeconds(10)){if(child.HasExited)throw new Exception("fixture stopped");Thread.Sleep(100);}
            child.Refresh();if(child.MainWindowHandle==IntPtr.Zero)throw new Exception("fixture window unavailable");
            var identity=new ProcessIdentity(child.Id,child.StartTime.ToUniversalTime().Ticks,exe);bool refused=false;
            try{identity.RequestExitAndWait(300);}catch(TimeoutException){refused=true;}
            if(!refused||child.HasExited)throw new Exception("close timeout not handled");
            Console.WriteLine("PASS update-in-progress startup refusal and main-process exit timeout");return 0;
        }finally{if(child!=null){try{if(!child.HasExited)child.Kill();child.WaitForExit(5000);}catch{}child.Dispose();}try{Directory.Delete(root,true);}catch{}}
    }
}
