using System;
using System.IO;
using GentlyPick.Updates;
class LogTests {
    static int Main() {
        string root=Path.Combine(Path.GetTempPath(),"gentlypick-log-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            string blocked=Path.Combine(root,"blocked");File.WriteAllText(blocked,"x");string path;
            using(var log=new UpdateLog(Path.Combine(blocked,"logs"),Path.Combine(root,"fallback"))) {
                if(!log.UsingFallback)throw new Exception("fallback not used");path=log.PathName;
                log.Write("token=abc password: xyz secret=hidden Authorization: Bearer supersecret https://example.com/file?X-Amz-Signature=signingsecret");
            }
            string text=File.ReadAllText(path);
            if(text.Contains("abc")||text.Contains("xyz")||text.Contains("hidden")||text.Contains("supersecret")||text.Contains("signingsecret")||!text.Contains("[redacted]"))throw new Exception("redaction failed");
            Console.WriteLine("PASS startup logging, fallback and redaction");return 0;
        } finally {Directory.Delete(root,true);}
    }
}
