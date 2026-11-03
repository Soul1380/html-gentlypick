using System;
using System.Diagnostics;
using GentlyPick.Updates;
class IdentityTests {
    static int Main() {
        using(var p=Process.GetCurrentProcess()) {
            var correct=new ProcessIdentity(p.Id,p.StartTime.ToUniversalTime().Ticks,p.MainModule.FileName);
            var captured=ProcessIdentity.Capture(p.Id);if(captured==null||captured.StartTicks!=correct.StartTicks)throw new Exception("native identity timestamp mismatch");
            using(var found=correct.Resolve())if(found==null||found.Id!=p.Id)throw new Exception("identity lookup failed");
            bool rejected=false;
            try { new ProcessIdentity(p.Id,p.StartTime.ToUniversalTime().Ticks+1,p.MainModule.FileName).Resolve(); }
            catch(InvalidOperationException) { rejected=true; }
            if(!rejected)throw new Exception("incorrect start time accepted");
            rejected=false;
            try { new ProcessIdentity(p.Id,p.StartTime.ToUniversalTime().Ticks,p.MainModule.FileName+".other").Resolve(); }
            catch(InvalidOperationException) { rejected=true; }
            if(!rejected)throw new Exception("incorrect executable accepted");
        }
        Console.WriteLine("PASS process identity and PID reuse protection");return 0;
    }
}
