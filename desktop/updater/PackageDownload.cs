using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GentlyPick.Updates {
    public static class PackageDownload {
        public static async Task Fetch(Uri uri,string destination,long expectedSize,string digest,TimeSpan timeout,Action<long,long,double> progress,bool allowLoopbackForTests,CancellationToken cancellationToken=default(CancellationToken)) {
            cancellationToken.ThrowIfCancellationRequested();
            if(uri==null || !(uri.Scheme==Uri.UriSchemeHttps || (allowLoopbackForTests && uri.Scheme==Uri.UriSchemeHttp && uri.IsLoopback)) || !String.IsNullOrEmpty(uri.UserInfo)) throw new ArgumentException("下载地址不安全。");
            if(expectedSize<=0 || expectedSize>2L*1024*1024*1024)throw new ArgumentOutOfRangeException("expectedSize");
            var request=(HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent="GentlyPick-Updater/1";request.AllowAutoRedirect=false;request.Timeout=(int)timeout.TotalMilliseconds;request.ReadWriteTimeout=(int)timeout.TotalMilliseconds;
            Uri current=uri;
            for(int redirects=0; redirects<=8; redirects++) {
                request=(HttpWebRequest)WebRequest.Create(current);
                request.UserAgent="GentlyPick-Updater/1";request.AllowAutoRedirect=false;request.Timeout=(int)timeout.TotalMilliseconds;request.ReadWriteTimeout=(int)timeout.TotalMilliseconds;
                HttpWebResponse response=null;
                using(var cancel=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)) using(cancel.Token.Register(()=>request.Abort())) {
                    cancel.CancelAfter(timeout);
                    try {
                        response=(HttpWebResponse)await request.GetResponseAsync();
                        int code=(int)response.StatusCode;
                        if(code>=300 && code<400) {
                            string location=response.Headers[HttpResponseHeader.Location]; response.Dispose();response=null;
                            if(String.IsNullOrEmpty(location) || redirects==8)throw new IOException("更新包重定向无效。");
                            current=new Uri(current,location);
                            if(current.Scheme!=Uri.UriSchemeHttps && !(allowLoopbackForTests && current.Scheme==Uri.UriSchemeHttp && current.IsLoopback))throw new IOException("更新包重定向不安全。");
                            continue;
                        }
                        if(code!=200)throw new IOException("更新包下载失败，HTTP "+code);
                        if(response.ContentLength>=0 && response.ContentLength!=expectedSize)throw new IOException("下载大小与 Release 不一致。");
                        using(response) using(var input=response.GetResponseStream()) using(var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None,65536,true)) {
                            var watch=Stopwatch.StartNew();long count=0;byte[] buffer=new byte[65536];int n;
                            while((n=await input.ReadAsync(buffer,0,buffer.Length,cancel.Token))>0) {
                                count+=n;if(count>expectedSize)throw new IOException("下载内容超过声明大小。");
                                await output.WriteAsync(buffer,0,n,cancel.Token);
                                if(progress!=null)progress(count,expectedSize,count/Math.Max(0.001,watch.Elapsed.TotalSeconds));
                            }
                            if(count!=expectedSize)throw new IOException("更新包下载不完整。");
                        }
                        response=null;
                        cancellationToken.ThrowIfCancellationRequested();await Task.Run(()=>PackageValidator.VerifyDownload(destination,expectedSize,digest));cancellationToken.ThrowIfCancellationRequested();return;
                    } catch(WebException ex) { if(cancellationToken.IsCancellationRequested)throw new OperationCanceledException("更新已取消。",ex,cancellationToken);if(cancel.IsCancellationRequested)throw new TimeoutException("下载更新包超时。",ex);throw; }
                    finally { if(response!=null)response.Dispose(); }
                }
            }
            throw new IOException("更新包重定向过多。");
        }
    }
}
