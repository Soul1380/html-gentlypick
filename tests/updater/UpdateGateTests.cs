using System;
using System.IO;
using GentlyPick.Updates;
class GateTests {
    static int Main() {
        string root=Path.Combine(Path.GetTempPath(),"gentlypick-gate-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            using(var first=UpdateGate.Acquire(root)) {
                bool refused=false;
                try { using(var second=UpdateGate.Acquire(root)){} } catch(IOException) { refused=true; }
                if(!refused)throw new Exception("concurrent gate accepted");
            }
            using(var retry=UpdateGate.Acquire(root)){}
            Console.WriteLine("PASS exclusive update gate and release");return 0;
        } finally { Directory.Delete(root,true); }
    }
}
