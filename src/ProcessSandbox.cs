using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BlockEngine
{
    // Owns the Windows Job Object for one child process. Closing the handle
    // terminates descendants as well, even when the child spawned a shell.
    // This is process-tree/resource lifetime isolation; it is deliberately not
    // advertised as a file-system or network sandbox.
    internal sealed class ProcessSandbox : IDisposable
    {
        private const uint JobObjectExtendedLimitInformation = 9;
        private const uint JobObjectLimitActiveProcess = 0x00000008;
        private const uint JobObjectLimitKillOnJobClose = 0x00002000;
        private const uint JobObjectLimitProcessMemory = 0x00000100;
        private const uint JobObjectLimitJobMemory = 0x00000200;
        private const uint ActiveProcessLimit = 64;
        private IntPtr jobHandle;

        private ProcessSandbox(IntPtr handle, bool attached)
        {
            jobHandle = handle;
            IsAttached = attached;
        }

        public bool IsAttached { get; private set; }

        public static ProcessSandbox Attach(Process process)
        {
            if (process == null) throw new ArgumentNullException("process");
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return new ProcessSandbox(IntPtr.Zero, false);

            IntPtr handle = CreateJobObject(IntPtr.Zero, null);
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Windows process limits could not be created; execution was stopped for safety.", new Win32Exception(Marshal.GetLastWin32Error()));

            JOBOBJECT_EXTENDED_LIMIT_INFORMATION limits = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            limits.BasicLimitInformation.LimitFlags = JobObjectLimitActiveProcess |
                JobObjectLimitKillOnJobClose | JobObjectLimitProcessMemory | JobObjectLimitJobMemory;
            limits.BasicLimitInformation.ActiveProcessLimit = ActiveProcessLimit;
            limits.ProcessMemoryLimit = new UIntPtr(SecurityLimits.ChildProcessMemoryLimitBytes);
            limits.JobMemoryLimit = new UIntPtr(SecurityLimits.ChildJobMemoryLimitBytes);
            if (!SetInformationJobObject(handle, JobObjectExtendedLimitInformation, ref limits, (uint)Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION))))
            {
                int error = Marshal.GetLastWin32Error();
                CloseHandle(handle);
                throw new InvalidOperationException("Windows process limits could not be configured; execution was stopped for safety.", new Win32Exception(error));
            }

            if (!AssignProcessToJobObject(handle, process.Handle))
            {
                int error = Marshal.GetLastWin32Error();
                CloseHandle(handle);
                throw new InvalidOperationException("Windows process limits could not be attached; execution was stopped for safety. Close restrictive parent jobs or retry from a normal terminal.", new Win32Exception(error));
            }

            return new ProcessSandbox(handle, true);
        }

        public void Dispose()
        {
            if (jobHandle == IntPtr.Zero) return;
            IntPtr handle = jobHandle;
            jobHandle = IntPtr.Zero;
            IsAttached = false;
            CloseHandle(handle);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr jobAttributes, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr job, uint infoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info, uint length);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
