using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GentlyPick.Updates;
class DownloadTests {
    static string root;
    static async Task<bool> RunServer(string mode,string file,string hash,int size,int timeout) {
        var server=new TcpListener(IPAddress.Loopback,0);server.Start();int port=((IPEndPoint)server.LocalEndpoint).Port;
        var task=Task.Run(()=> { using(var c=server.AcceptTcpClient()) using(var s=c.GetStream()) {
            byte[] request=new byte[2048];s.Read(request,0,request.Length);
            if(mode=="timeout") { Thread.Sleep(1000);return; }
            string headers="HTTP/1.1 200 OK\r\nContent-Length: 3\r\nConnection: close\r\n\r\n";
            byte[] h=Encoding.ASCII.GetBytes(headers);s.Write(h,0,h.Length);
            byte[] body=Encoding.ASCII.GetBytes(mode=="short"?"ab":"abc");s.Write(body,0,body.Length);
        }});
        bool success=true;
        try { await PackageDownload.Fetch(new Uri("http://127.0.0.1:"+port+"/test"),Path.Combine(root,file),size,hash,TimeSpan.FromMilliseconds(timeout),(n,t,r)=>{},true); }
        catch(Exception) { success=false; }
        server.Stop();await task;return success;
    }
    static int Main() {
        root=Path.Combine(Path.GetTempPath(),"gentlypick-download-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try {
            string good="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            if(!RunServer("ok","ok.zip",good,3,2000).GetAwaiter().GetResult())throw new Exception("download failed");
            if(RunServer("short","short.zip",good,3,2000).GetAwaiter().GetResult())throw new Exception("truncated download accepted");
            if(RunServer("ok","hash.zip",new string('0',64),3,2000).GetAwaiter().GetResult())throw new Exception("invalid hash accepted");
            if(RunServer("timeout","timeout.zip",good,3,150).GetAwaiter().GetResult())throw new Exception("timeout accepted");
            TestCancel(good).GetAwaiter().GetResult();
            Console.WriteLine("PASS download, interruption, hash mismatch and timeout");return 0;
        } finally {Directory.Delete(root,true);}
    }
    static async Task TestCancel(string hash) {
        var server=new TcpListener(IPAddress.Loopback,0);server.Start();int port=((IPEndPoint)server.LocalEndpoint).Port;
        var accepted=new TaskCompletionSource<bool>();
        var task=Task.Run(()=>{using(var client=server.AcceptTcpClient())using(var stream=client.GetStream()){var request=new byte[2048];stream.Read(request,0,request.Length);accepted.SetResult(true);Thread.Sleep(500);}});
        using(var cts=new CancellationTokenSource()) {
            var pending=PackageDownload.Fetch(new Uri("http://127.0.0.1:"+port+"/slow"),Path.Combine(root,"cancel.zip"),3,hash,TimeSpan.FromSeconds(5),null,true,cts.Token);
            await accepted.Task;cts.Cancel();bool cancelled=false;try{await pending;}catch(OperationCanceledException){cancelled=true;}if(!cancelled)throw new Exception("cancellation was not honored");
        }
        server.Stop();await task;
    }
}
