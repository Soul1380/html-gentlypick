using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace GentlyPick.Updates {
    public static class ProcessTree {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        struct PROCESSENTRY32 {
            public uint size,usage,processId; public UIntPtr defaultHeapId; public uint moduleId,threads,parentProcessId; public int basePriority; public uint flags;
            [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string exeFile;
        }
        [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr CreateToolhelp32Snapshot(uint flags,uint processId);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Process32FirstW(IntPtr snapshot,ref PROCESSENTRY32 entry);
        [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool Process32NextW(IntPtr snapshot,ref PROCESSENTRY32 entry);
        [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
        public static List<ProcessIdentity> Capture(ProcessIdentity root, int excludedPid) {
            using(var verified=root.Resolve()) { if(verified==null)throw new InvalidOperationException("主程序已经退出，无法确认进程树，已取消更新。"); }
            var children=new Dictionary<int,List<int>>();
            IntPtr snapshot=CreateToolhelp32Snapshot(2,0);
            if(snapshot==new IntPtr(-1))throw new Win32Exception(Marshal.GetLastWin32Error(),"无法获取进程快照。");
            try {
                var row=new PROCESSENTRY32();row.size=(uint)Marshal.SizeOf(typeof(PROCESSENTRY32));
                if(!Process32FirstW(snapshot,ref row))throw new Win32Exception(Marshal.GetLastWin32Error(),"无法读取进程快照。");
                do { int id=(int)row.processId,parent=(int)row.parentProcessId;List<int> list;
                    if(!children.TryGetValue(parent,out list)){list=new List<int>();children[parent]=list;}list.Add(id);
                } while(Process32NextW(snapshot,ref row));
                int error=Marshal.GetLastWin32Error();if(error!=18)throw new Win32Exception(error,"读取进程快照失败。");
            } finally { CloseHandle(snapshot); }
            var result=new List<ProcessIdentity>();var queue=new Queue<int>();var visited=new HashSet<int>();queue.Enqueue(root.Id);visited.Add(root.Id);
            while(queue.Count>0) {
                int parent=queue.Dequeue();List<int> list;if(!children.TryGetValue(parent,out list))continue;
                foreach(int id in list) {
                    if(id==excludedPid || !visited.Add(id))continue;
                    var captured=ProcessIdentity.Capture(id);
                    if(captured!=null){result.Add(captured);queue.Enqueue(id);}
                }
            }
            return result;
        }
        public static void WaitForExit(IEnumerable<ProcessIdentity> identities,TimeSpan timeout,Action<string> log) {
            var watch=Stopwatch.StartNew();
            foreach(var identity in identities) {
                int left=(int)Math.Max(0,Math.Min(int.MaxValue,(timeout-watch.Elapsed).TotalMilliseconds));
                if(!identity.WaitForExit(left))throw new TimeoutException("相关进程未退出，已取消更新："+identity.Id);
                log("进程已退出："+identity.Id);
            }
        }
    }
}
