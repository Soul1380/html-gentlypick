using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Threading;

namespace GentlyPick.Updates {
    public sealed class UpdateResult { public bool Installed, Restarted, Cancelled; public string Message, LogPath; }
    public static class UpdateCoordinator {
        public static async Task<UpdateResult> Run(string root,ReleaseInfo release,ProcessIdentity parent,Action<string,int> report,UpdateLog log,bool allowLoopbackForTests = false,CancellationToken cancellationToken=default(CancellationToken),Action beforeCommit=null) {
            root=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string main=Path.GetFullPath(Path.Combine(root,"轻摘.exe"));
            if(!String.Equals(parent.Executable,main,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("主程序身份与安装路径不一致。");
            string running=Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if(running.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("更新器必须从安装目录外运行。");
            {
                var result=new UpdateResult{LogPath=log.PathName};
                Action<string,int> stage=(name,percent)=>{log.Write(name);report(name,percent);};
                log.Write("安装目录："+root+"；目标版本："+release.Version+"；主进程："+parent.Id);
                using(var gate=UpdateGate.Acquire(root)) {
                    string work=Path.Combine(root,"UserData","updates","work-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(work);
                    string package=Path.Combine(work,"package.zip"),unpacked=Path.Combine(work,"stage"),backup=Path.Combine(work,"backup");
                    try {
                        stage("下载更新包",0);
                        await PackageDownload.Fetch(new Uri(release.AssetUrl),package,release.Size,release.Sha256,TimeSpan.FromMinutes(30),(n,total,speed)=>report("下载 "+(n/1048576.0).ToString("0.00")+" / "+(total/1048576.0).ToString("0.00")+" MB · "+(speed/1048576.0).ToString("0.00")+" MB/s",(int)(n*100/total)),allowLoopbackForTests,cancellationToken);
                        stage("下载大小与 SHA-256 已验证",100);
                        stage("解压并校验更新包",0);
                        List<string> files=await Task.Run(()=>PackageValidator.Extract(package,unpacked,release.Version,cancellationToken));
                        foreach(string file in files)PackageValidator.CheckedPath(root,file);
                        cancellationToken.ThrowIfCancellationRequested();
                        if(beforeCommit!=null)beforeCommit();
                        cancellationToken.ThrowIfCancellationRequested();
                        stage("确认主程序和相关进程",0);
                        var children=ProcessTree.Capture(parent,Process.GetCurrentProcess().Id);
                        log.Write("捕获相关进程数量："+children.Count);
                        parent.RequestExitAndWait(30000);
                        log.Write("主进程退出已确认："+parent.Id);
                        ProcessTree.WaitForExit(children,TimeSpan.FromSeconds(30),log.Write);
                        stage("进程已退出，开始备份与替换",0);
                        int count=0;
                        await Task.Run(()=>FileTransaction.Run(root,unpacked,backup,files,text=>{log.Write(text);report(text,Math.Min(99,++count*100/Math.Max(1,files.Count*4)));},null));
                        log.Write("安装完成。");result.Installed=true;
                    } catch(OperationCanceledException) {
                        log.Write("用户取消更新，尚未关闭主程序或替换文件。");result.Cancelled=true;result.Message="已取消更新。";return result;
                    } catch(Exception e) {
                        try {log.Write("更新失败："+e);}catch{}
                        result.Message=e.Message;return result;
                    }
                }
                try {
                    stage("安装完成，正在重新启动",100);
                    string startupLog=Path.Combine(root,"UserData","startup.log");
                    long offset=File.Exists(startupLog)?new FileInfo(startupLog).Length:0;
                    var started=Process.Start(new ProcessStartInfo(main){UseShellExecute=true,WorkingDirectory=root});
                    if(started==null)throw new IOException("系统没有返回主程序进程。");
                    log.Write("已启动主程序，进程号："+started.Id+"，等待启动完成。");
                    await ConfirmStartup(started,startupLog,offset,TimeSpan.FromSeconds(60));
                    log.Write("新版主程序已确认启动完成。");result.Restarted=true;result.Message="安装完成并已确认主程序启动。";
                } catch(Exception e) { log.Write("安装成功，但重新启动失败："+e);result.Message="安装已成功，但重启失败："+e.Message; }
                return result;
            }
        }
        static async Task ConfirmStartup(Process process,string file,long offset,TimeSpan timeout) {
            var watch=Stopwatch.StartNew();
            while(watch.Elapsed<timeout) {
                process.Refresh();if(process.HasExited)throw new IOException("主程序启动后退出，退出码："+process.ExitCode);
                if(File.Exists(file)) {
                    using(var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.ReadWrite)) {
                        if(stream.Length<offset)throw new IOException("启动日志被替换，无法确认新版启动。");
                        stream.Position=offset;using(var reader=new StreamReader(stream,Encoding.UTF8)) {
                            string lines=await reader.ReadToEndAsync();
                            if(lines.Contains("启动失败："))throw new IOException("新版主程序报告启动失败，请查看启动日志。");
                            if(lines.Contains("启动完成")){process.Refresh();if(process.HasExited)throw new IOException("新版主程序在启动后退出。");return;}
                        }
                    }
                }
                await Task.Delay(250);
            }
            throw new TimeoutException("未能在规定时间内确认新版启动，请查看启动日志。");
        }
    }
}
