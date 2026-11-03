using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GentlyPick.Updates {
    public sealed class ProcessIdentity {
        [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,int pid);
        [DllImport("kernel32.dll",SetLastError=true,CharSet=CharSet.Unicode)]static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder name,ref int size);
        [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll",SetLastError=true)]static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
        [DllImport("kernel32.dll",SetLastError=true)]static extern bool GetProcessTimes(IntPtr process,out long created,out long exited,out long kernel,out long user);
        static IntPtr OpenTracked(int pid) {
            IntPtr handle=OpenProcess(0x101000,false,pid);
            if(handle!=IntPtr.Zero)return handle;
            int code=Marshal.GetLastWin32Error();if(code==87||code==1168)return IntPtr.Zero;
            throw new Win32Exception(code,"无法确认相关进程身份。");
        }
        static long CreationTicks(IntPtr handle) {
            long created,exited,kernel,user;
            if(!GetProcessTimes(handle,out created,out exited,out kernel,out user))throw new Win32Exception(Marshal.GetLastWin32Error(),"无法确认进程启动时间。");
            return DateTime.FromFileTimeUtc(created).Ticks;
        }
        static string ImageFromHandle(IntPtr handle) {
            var name=new StringBuilder(32768);int size=name.Capacity;
            if(!QueryFullProcessImageName(handle,0,name,ref size))throw new Win32Exception(Marshal.GetLastWin32Error(),"无法读取进程路径。");
            return name.ToString();
        }
        public static ProcessIdentity Capture(int pid) {
            IntPtr h=OpenTracked(pid);if(h==IntPtr.Zero)return null;
            try {uint status=WaitForSingleObject(h,0);if(status==0)return null;if(status!=0x102)throw new Win32Exception(Marshal.GetLastWin32Error(),"无法确认进程状态。");return new ProcessIdentity(pid,CreationTicks(h),ImageFromHandle(h));}
            finally {CloseHandle(h);}
        }
        public bool WaitForExit(int timeoutMilliseconds) {
            IntPtr h=OpenTracked(Id);if(h==IntPtr.Zero)return true;
            try {
                uint initial=WaitForSingleObject(h,0);if(initial==0)return true;if(initial!=0x102)throw new Win32Exception(Marshal.GetLastWin32Error(),"无法确认相关进程状态。");
                if(CreationTicks(h)!=StartTicks || !Path.GetFullPath(ImageFromHandle(h)).Equals(Executable,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("进程身份已变化，已取消更新。");
                uint status=WaitForSingleObject(h,(uint)Math.Max(0,timeoutMilliseconds));if(status==0)return true;if(status==0x102)return false;throw new Win32Exception(Marshal.GetLastWin32Error(),"无法等待相关进程。");
            } finally {CloseHandle(h);}
        }
        public static string ImagePath(int pid) {
            IntPtr handle=OpenProcess(0x1000,false,pid);
            if(handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"无法确认进程路径。");
            try {return ImageFromHandle(handle);}
            finally {CloseHandle(handle);}
        }
        public readonly int Id;
        public readonly long StartTicks;
        public readonly string Executable;
        public ProcessIdentity(int id,long startTicks,string executable) { Id=id;StartTicks=startTicks;Executable=Path.GetFullPath(executable); }
        public Process Resolve() {
            Process process;
            try { process=Process.GetProcessById(Id); }
            catch(ArgumentException) { return null; }
            try {
                if(process.HasExited){process.Dispose();return null;}
                if(process.StartTime.ToUniversalTime().Ticks!=StartTicks || !Path.GetFullPath(ImagePath(Id)).Equals(Executable,StringComparison.OrdinalIgnoreCase)) {
                    throw new InvalidOperationException("进程身份不匹配，已取消更新。");
                }
                return process;
            } catch(Win32Exception) { if(process.HasExited){process.Dispose();return null;}process.Dispose();throw; }
              catch(InvalidOperationException) { if(process.HasExited){process.Dispose();return null;}process.Dispose();throw; }
              catch { process.Dispose();throw; }
        }
        public void RequestExitAndWait(int timeoutMilliseconds) {
            using(Process p=Resolve()) {
                if(p==null)return;
                if(p.HasExited)return;
                if(!p.CloseMainWindow())throw new InvalidOperationException("无法向主程序发送关闭请求，已取消更新。");
                if(!p.WaitForExit(timeoutMilliseconds))throw new TimeoutException("主程序未在规定时间内退出，已取消更新。");
            }
        }
    }
}
