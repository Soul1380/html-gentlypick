using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using GentlyPick.Updates;
class FixtureApp {
    [STAThread] static void Main(string[] args) {
        string root=AppDomain.CurrentDomain.BaseDirectory;
        try {using(var gate=UpdateGate.Acquire(root)) {}}
        catch {File.WriteAllText(Path.Combine(root,"gate-refused.txt"),"refused");return;}
        string startLog=Path.Combine(root,"UserData","startup.log");
        if(File.Exists(Path.Combine(root,"fail-start.txt"))){File.AppendAllText(startLog,"启动失败：测试失败\n");return;}
        Application.EnableVisualStyles();
        var form=new Form{Text="GentlyPick isolated updater test",Width=250,Height=100,ShowInTaskbar=true,WindowState=FormWindowState.Minimized};
        if(args.Length>0 && args[0]=="--refuse-close")form.FormClosing+=(s,e)=>e.Cancel=true;
        form.Shown+=(s,e)=>{File.WriteAllText(Path.Combine(root,"boot.txt"),Process.GetCurrentProcess().Id+" "+File.ReadAllText(Path.Combine(root,"version.json")));File.AppendAllText(startLog,"启动完成\n");};
        Application.Run(form);
    }
}
