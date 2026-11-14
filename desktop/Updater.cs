using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using GentlyPick.Updates;
[assembly:System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly:System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace GentlyPick.Updater {
    static class Program {
        [STAThread]static void Main(string[] args) {
            AppContext.SetSwitch("Switch.System.IO.UseLegacyPathHandling",false);AppContext.SetSwitch("Switch.System.IO.BlockLongPaths",false);
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            string root=null;int pid=0;long ticks=0;
            for(int i=0;i<args.Length-1;i++) { if(args[i]=="--root")root=args[++i];else if(args[i]=="--pid")int.TryParse(args[++i],out pid);else if(args[i]=="--start")long.TryParse(args[++i],out ticks); }
            if(String.IsNullOrEmpty(root)||pid<=0||ticks<=0) {MessageBox.Show("请从轻摘中启动更新器。","更新器");return;}
            UpdateLog log;
            try { log=new UpdateLog(Path.Combine(root,"UserData","updates","logs"),Path.Combine(Path.GetTempPath(),"GentlyPick-update-logs")); }
            catch(Exception e){MessageBox.Show("无法创建更新日志，更新已停止："+e.Message,"更新器");return;}
            using(log)Application.Run(new UpdateWindow(root,pid,ticks,log));
        }
    }
    class UpdateWindow:Form {
        readonly string root;readonly ProcessIdentity parent;readonly UpdateLog log;bool running=true,commitStarted=false;readonly CancellationTokenSource cancelSource=new CancellationTokenSource();
        readonly Label phase=new Label(),detail=new Label();readonly ProgressBar bar=new ProgressBar();readonly Button open=new Button(),exit=new Button(),cancel=new Button();
        public UpdateWindow(string root,int pid,long ticks,UpdateLog log) {
            this.root=Path.GetFullPath(root);this.log=log;parent=new ProcessIdentity(pid,ticks,Path.Combine(root,"轻摘.exe"));
            Text="轻摘更新";Size=new Size(680,260);MinimumSize=new Size(600,230);StartPosition=FormStartPosition.CenterScreen;Font=new Font("Microsoft YaHei UI",10);BackColor=Color.White;
            var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(25),RowCount=5,ColumnCount=1};
            phase.Text="正在准备更新";phase.Dock=DockStyle.Fill;phase.AutoSize=true;panel.Controls.Add(phase);
            bar.Dock=DockStyle.Top;bar.Height=20;panel.Controls.Add(bar);
            detail.AutoSize=true;detail.MaximumSize=new Size(600,0);detail.Text="日志："+log.PathName;panel.Controls.Add(detail);
            var buttons=new FlowLayoutPanel{AutoSize=true,Dock=DockStyle.Fill};open.Text="打开日志位置";open.AutoSize=true;open.Click+=delegate{System.Diagnostics.Process.Start("explorer.exe","/select,\""+log.PathName+"\"");};
            exit.Text="退出";exit.AutoSize=true;exit.Enabled=false;exit.Click+=delegate{Close();};cancel.Text="取消更新";cancel.AutoSize=true;cancel.Click+=delegate{if(!running||commitStarted)return;if(MessageBox.Show("确定取消本次更新吗？","取消更新",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes){cancel.Enabled=false;phase.Text="正在取消更新…";cancelSource.Cancel();}};buttons.Controls.Add(cancel);buttons.Controls.Add(open);buttons.Controls.Add(exit);panel.Controls.Add(buttons);Controls.Add(panel);
            if(log.UsingFallback)MessageBox.Show("安装目录中的日志不可写，日志已写入备用目录：\n"+log.PathName,"更新日志");
            FormClosing+=(s,e)=>{if(running)e.Cancel=true;};Shown+=async delegate{await RunUpdate();};
        }
        void ShowProgress(string text,int amount) {
            if(InvokeRequired){BeginInvoke((Action)(()=>ShowProgress(text,amount)));return;}
            phase.Text=text;bar.Value=Math.Max(0,Math.Min(100,amount));
        }
        async Task RunUpdate() {
            try {
                log.Write("启动路径："+System.Reflection.Assembly.GetExecutingAssembly().Location+"；安装目录："+root+"；主程序 PID："+parent.Id);
                ShowProgress("正在获取 GitHub Release 信息",0);
                var local=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(File.ReadAllText(Path.Combine(root,"version.json")));
                Version current=ReleaseFeed.ParseVersion(Convert.ToString(local["version"]));
                log.Write("当前版本："+current);
                ReleaseInfo release=await ReleaseClient.Latest(current);cancelSource.Token.ThrowIfCancellationRequested();if(release==null||release.Version<=current)throw new InvalidOperationException("没有可用的新版本。");
                Action beforeCommit=()=>{Action disable=()=>{cancelSource.Token.ThrowIfCancellationRequested();commitStarted=true;cancel.Enabled=false;};if(InvokeRequired)Invoke(disable);else disable();};
                var result=await UpdateCoordinator.Run(root,release,parent,ShowProgress,log,false,cancelSource.Token,beforeCommit);
                if(result.Cancelled){running=false;ShowProgress("更新已取消",0);detail.Text="主程序和文件未更改。\n日志："+log.PathName;exit.Enabled=true;return;}
                if(result.Installed && !result.Restarted) {running=false;ShowProgress("安装成功，但重启失败",100);detail.Text=result.Message+"\n日志："+log.PathName;exit.Enabled=true;return;}
                if(!result.Installed)throw new InvalidOperationException(result.Message);
                running=false;Close();
            } catch(OperationCanceledException) {
                try{log.Write("用户取消更新。");}catch{}running=false;ShowProgress("更新已取消",0);detail.Text="主程序和文件未更改。\n日志："+log.PathName;exit.Enabled=true;
            } catch(Exception e) {
                try{log.Write("更新器异常："+e);}catch{}
                running=false;ShowProgress("更新未完成",0);detail.Text=e.Message+"\n日志："+log.PathName;exit.Enabled=true;
            }
        }
    }
}
