using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

internal sealed class Installer : Form {
 readonly TextBox path = new TextBox(); readonly Button browse = new Button(), install = new Button(); readonly ProgressBar bar = new ProgressBar(); readonly Label state = new Label(); bool busy;
 public Installer() {
  Text="轻摘安装程序"; ClientSize=new Size(580,260); StartPosition=FormStartPosition.CenterScreen; Font=new Font("Microsoft YaHei UI",9);
  Controls.Add(new Label{Text="安装轻摘",Font=new Font(Font.FontFamily,18,FontStyle.Bold),AutoSize=true,Location=new Point(24,20)});
  Controls.Add(new Label{Text="安装位置（请选择空文件夹，用户数据会保存在此处）",AutoSize=true,Location=new Point(26,76)});
  path.SetBounds(28,104,410,28);path.Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","GentlyPick");Controls.Add(path);
  browse.Text="浏览…";browse.SetBounds(445,101,100,30);browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog()){if(d.ShowDialog()==DialogResult.OK)path.Text=d.SelectedPath;}};Controls.Add(browse);
  bar.SetBounds(28,153,516,12);Controls.Add(bar);state.SetBounds(28,172,516,25);state.Text="准备安装";Controls.Add(state);
  install.Text="安装";install.SetBounds(445,210,100,32);install.Click+=StartInstall;Controls.Add(install);FormClosing+=(s,e)=>{if(busy)e.Cancel=true;};
 }
 void StartInstall(object sender,EventArgs e) {
  string target;try {if(String.IsNullOrWhiteSpace(path.Text))throw new Exception("请选择安装目录。");target=Path.GetFullPath(path.Text.Trim());if(target.TrimEnd('\\')==Path.GetPathRoot(target).TrimEnd('\\'))throw new Exception("不能安装在磁盘根目录。");if(Directory.Exists(target)&&Directory.GetFileSystemEntries(target).Length!=0)throw new Exception("为保护已有文件，请选择空文件夹。");}catch(Exception x){MessageBox.Show(x.Message,"轻摘",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
  busy=true;install.Enabled=false;browse.Enabled=false;path.Enabled=false;state.Text="正在安装…";
  ThreadPool.QueueUserWorkItem(_=>{try{Extract(target);BeginInvoke((Action)(()=>{busy=false;state.Text="安装完成";bar.Value=100;install.Enabled=true;install.Text="启动轻摘";install.Click-=StartInstall;install.Click+=(a,b)=>{try{Process.Start(Path.Combine(target,"轻摘.exe"));Close();}catch(Exception x){MessageBox.Show("已安装，启动失败："+x.Message);}};}));}catch(Exception x){BeginInvoke((Action)(()=>{busy=false;state.Text="安装失败";MessageBox.Show(x.Message,"轻摘安装失败",MessageBoxButtons.OK,MessageBoxIcon.Error);Close();}));}});
 }
 void Extract(string target) {
  Directory.CreateDirectory(target);string root=Path.GetFullPath(target).TrimEnd('\\')+"\\";
  var files=new System.Collections.Generic.List<string>();
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")) using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)) {
   int count=zip.Entries.Count,done=0; foreach(var entry in zip.Entries) {string dest=Path.GetFullPath(Path.Combine(root,entry.FullName.Replace('/','\\')));if(!dest.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("安装包路径无效");if(entry.FullName.EndsWith("/")){Directory.CreateDirectory(dest);}else{Directory.CreateDirectory(Path.GetDirectoryName(dest));using(var source=entry.Open())using(var file=new FileStream(dest,FileMode.CreateNew))source.CopyTo(file);files.Add(entry.FullName);}done++;int value=count==0?0:done*95/count;BeginInvoke((Action)(()=>bar.Value=value));}
  }
  using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("Uninstall.exe"))using(var output=File.Create(Path.Combine(target,"Uninstall.exe")))input.CopyTo(output);
  File.WriteAllLines(Path.Combine(target,"install-files.txt"),files.ToArray());
  string program=Path.Combine(target,"轻摘.exe"); string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"轻摘.lnk");
  Type shellType=Type.GetTypeFromProgID("WScript.Shell"); dynamic shell=Activator.CreateInstance(shellType); dynamic link=shell.CreateShortcut(shortcut);link.TargetPath=program;link.WorkingDirectory=target;link.Save();
  using(var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\GentlyPick")){key.SetValue("DisplayName","轻摘");key.SetValue("DisplayVersion",FileVersion());key.SetValue("InstallLocation",target);key.SetValue("UninstallString","\""+Path.Combine(target,"Uninstall.exe")+"\"");key.SetValue("DisplayIcon",program);key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);}
 }
 string FileVersion(){return typeof(Installer).Assembly.GetName().Version.ToString(3);}
 [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Installer());}
}
