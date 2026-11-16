using System;
using System.Reflection;
using System.Runtime.InteropServices;
using LightExcerpt;
class NativeControlsTest {
 [STAThread] static int Main(){object instance=null;try{
  var type=typeof(ModernFolderPicker);var flags=BindingFlags.NonPublic;
  var coclass=type.GetNestedType("FileOpenDialog",flags);var api=type.GetNestedType("IFileDialog",flags);
  instance=Activator.CreateInstance(coclass);var unknown=Marshal.GetIUnknownForObject(instance);IntPtr queried;
  try{Guid iid=api.GUID;int hr=Marshal.QueryInterface(unknown,ref iid,out queried);if(hr!=0)throw new Exception("Common Item Dialog interface unavailable");Marshal.Release(queried);}finally{Marshal.Release(unknown);}
  using(var button=new ChromeActionButton()){button.NotifyDefault(true);if(button.GetType().GetProperty("ShowFocusCues",BindingFlags.Instance|BindingFlags.NonPublic)==null)throw new Exception("focus override missing");}
  Console.WriteLine("PASS actual Windows Common Item Dialog COM availability; shared native controls and focus override compile");return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}finally{if(instance!=null)Marshal.ReleaseComObject(instance);}}
}
