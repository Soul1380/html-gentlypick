using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace LightExcerpt {
// Mouse actions do not leave a focus rectangle. Keyboard focus remains visible.
class ChromeActionButton : Button {
 bool keyboardFocus;
 protected override bool ShowFocusCues { get { return Focused && keyboardFocus; } }
 public override void NotifyDefault(bool value) { base.NotifyDefault(false); }
 protected override void OnMouseDown(MouseEventArgs e) { keyboardFocus=false; base.OnMouseDown(e); Invalidate(); }
 protected override void OnGotFocus(EventArgs e) { keyboardFocus=Control.MouseButtons==MouseButtons.None; base.OnGotFocus(e); Invalidate(); }
 protected override void OnKeyDown(KeyEventArgs e) { keyboardFocus=true; base.OnKeyDown(e); Invalidate(); }
 protected override void OnLostFocus(EventArgs e) { keyboardFocus=false; base.OnLostFocus(e); Invalidate(); }
}
static class ModernFolderPicker {
 [ComImport, Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")] class FileOpenDialog {}
 [ComImport, Guid("42F85136-DB7E-439C-85F1-E4075D135FC8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IFileDialog {
  [PreserveSig] int Show(IntPtr owner);
  void SetFileTypes(uint count,IntPtr types); void SetFileTypeIndex(uint index); void GetFileTypeIndex(out uint index);
  void Advise(IntPtr events,out uint cookie); void Unadvise(uint cookie); void SetOptions(uint options); void GetOptions(out uint options);
  void SetDefaultFolder(IShellItem folder); void SetFolder(IShellItem folder); void GetFolder(out IShellItem folder); void GetCurrentSelection(out IShellItem item);
  void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name); void GetFileName(out IntPtr name);
  void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title); void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text); void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
  void GetResult(out IShellItem item); void AddPlace(IShellItem item,uint alignment); void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
  void Close(int result); void SetClientGuid(ref Guid guid); void ClearClientData(); void SetFilter(IntPtr filter);
 }
 [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)] interface IShellItem {
  void BindToHandler(IntPtr bind,ref Guid handler,ref Guid iid,out IntPtr result); void GetParent(out IShellItem parent);
  void GetDisplayName(uint type,out IntPtr name); void GetAttributes(uint mask,out uint attributes); void Compare(IShellItem other,uint hint,out int order);
 }
 [DllImport("shell32.dll",CharSet=CharSet.Unicode,PreserveSig=false)] static extern void SHCreateItemFromParsingName(string path,IntPtr bind,ref Guid iid,out IShellItem item);
 public static string Show(IntPtr owner,string initial,string title) {
  var dialog=(IFileDialog)new FileOpenDialog();
  try {
   dialog.SetOptions(0x20|0x40|0x800|0x8); dialog.SetTitle(title); dialog.SetOkButtonLabel("选择此文件夹");
   if(Directory.Exists(initial)){IShellItem folder;Guid iid=typeof(IShellItem).GUID;SHCreateItemFromParsingName(initial,IntPtr.Zero,ref iid,out folder);try{dialog.SetFolder(folder);}finally{Marshal.ReleaseComObject(folder);}}
   int result=dialog.Show(owner);if(result==unchecked((int)0x800704C7))return "";if(result<0)Marshal.ThrowExceptionForHR(result);
   IShellItem item;dialog.GetResult(out item);try{IntPtr name;item.GetDisplayName(0x80058000,out name);try{return Marshal.PtrToStringUni(name);}finally{Marshal.FreeCoTaskMem(name);}}finally{Marshal.ReleaseComObject(item);}
  }finally{Marshal.ReleaseComObject(dialog);}
 }
}
}
