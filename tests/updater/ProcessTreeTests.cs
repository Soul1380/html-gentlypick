using System;
using System.Diagnostics;
using System.Linq;
using GentlyPick.Updates;
class ProcessTreeTests {
    static int Main() { try { return Run(); } catch(Exception e) { Console.WriteLine("FAIL: "+e.GetType().FullName+" "+e.Message); return 1; } }
    static int Run() {
        var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("COMSPEC"),"/c more") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true };
        using(var child=Process.Start(start)) using(var self=Process.GetCurrentProcess()) {
            try {
                var root=new ProcessIdentity(self.Id,self.StartTime.ToUniversalTime().Ticks,self.MainModule.FileName);
                var found=ProcessTree.Capture(root,-1);
                var identity=found.FirstOrDefault(p=>p.Id==child.Id);if(identity==null)throw new Exception("child not captured");
                bool timedOut=false;try { ProcessTree.WaitForExit(new[]{identity},TimeSpan.FromMilliseconds(100),s=>{}); } catch(TimeoutException) { timedOut=true; }
                if(!timedOut)throw new Exception("running child accepted");
                child.StandardInput.Close();
                ProcessTree.WaitForExit(new[]{identity},TimeSpan.FromSeconds(5),s=>{});
                Console.WriteLine("PASS child process identification, timeout and exit confirmation");return 0;
            } finally { if(!child.HasExited)child.Kill(); }
        }
    }
}
