using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
internal static class Uninstall {
 [STAThread] static void Main(){Application.EnableVisualStyles();string root=AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
  if(MessageBox.Show("卸载轻摘？\n\n用户资料 UserData 会保留在安装目录。请先关闭轻摘。","卸载轻摘",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
  try {foreach(var p in Process.GetProcessesByName("轻摘")){try{if(String.Equals(Path.GetDirectoryName(p.MainModule.FileName),root,StringComparison.OrdinalIgnoreCase))throw new Exception("请先关闭轻摘，再卸载。");}finally{p.Dispose();}}
   string manifest=Path.Combine(root,"install-files.txt");if(!File.Exists(manifest))throw new Exception("缺少安装文件清单，停止卸载以保护资料。");
   foreach(string line in File.ReadAllLines(manifest)){if(String.IsNullOrWhiteSpace(line))continue;string target=Path.GetFullPath(Path.Combine(root,line));if(!target.StartsWith(root+"\\",StringComparison.OrdinalIgnoreCase)||line.StartsWith("UserData",StringComparison.OrdinalIgnoreCase))throw new Exception("安装文件清单无效。");if(File.Exists(target))File.Delete(target);}
   File.Delete(manifest);foreach(var d in Directory.GetDirectories(root,"*",SearchOption.AllDirectories)){if(!d.StartsWith(Path.Combine(root,"UserData"),StringComparison.OrdinalIgnoreCase))try{Directory.Delete(d,false);}catch{}}
   using(var key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall",true))if(key!=null)key.DeleteSubKeyTree("GentlyPick",false);
   string shortcut=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"轻摘.lnk");if(File.Exists(shortcut))File.Delete(shortcut);
   MessageBox.Show("卸载完成。用户资料保留在：\n"+Path.Combine(root,"UserData"),"卸载轻摘");
  }catch(Exception e){MessageBox.Show("卸载未完成："+e.Message,"卸载轻摘",MessageBoxButtons.OK,MessageBoxIcon.Error);}
 }
}
