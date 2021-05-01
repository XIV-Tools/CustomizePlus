// © Anamnesis.
// Developed by W and A Walsh.
// Licensed under the MIT license.

namespace Anamnesis.Memory
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using Anamnesis.Core.Memory;
	using Serilog;

	public static class MemoryService
	{
		private static readonly Dictionary<string, IntPtr> modules = new Dictionary<string, IntPtr>();

		public static IntPtr Handle { get; private set; }
		public static SignatureScanner? Scanner { get; private set; }
		public static Process? Process { get; private set; }
		public static bool IsProcessAlive { get; private set; }

		public static string GamePath
		{
			get
			{
				if (Process == null)
					throw new Exception("No game process");

				if (Process.MainModule == null)
					throw new Exception("Process has no main module");

				return Path.GetDirectoryName(Process.MainModule.FileName) + "\\..\\";
			}
		}

		public static bool GetIsProcessAlive()
		{
			if (Process == null || Process.HasExited)
				return false;

			if (!Process.Responding)
				return false;

			return true;
		}

		public static IntPtr ReadPtr(IntPtr address)
		{
			byte[] d = new byte[8];
			ReadProcessMemory(Handle, address, d, 8, out _);
			long i = BitConverter.ToInt64(d, 0);
			IntPtr ptr = (IntPtr)i;
			return ptr;
		}

		public static T? Read<T>(UIntPtr address)
			where T : struct
		{
			unsafe
			{
				IntPtr ptr = (IntPtr)address.ToPointer();
				return Read<T>(ptr);
			}
		}

		public static T Read<T>(IntPtr address)
			where T : struct
		{
			if (address == IntPtr.Zero)
				throw new Exception("Invalid address");

			int attempt = 0;
			while (attempt < 10)
			{
				int size = Marshal.SizeOf(typeof(T));
				IntPtr mem = Marshal.AllocHGlobal(size);
				ReadProcessMemory(Handle, address, mem, size, out _);
				T? val = Marshal.PtrToStructure<T>(mem);
				Marshal.FreeHGlobal(mem);
				attempt++;

				if (val != null)
					return (T)val;

				Thread.Sleep(100);
			}

			throw new Exception($"Failed to read memory {typeof(T)} from address {address}");
		}

		public static void Write<T>(IntPtr address, T value)
			where T : struct
		{
			if (address == IntPtr.Zero)
				return;

			// Read the existing memory to oldBuffer
			int size = Marshal.SizeOf(typeof(T));
			////byte[] oldBuffer = new byte[size];
			////ReadProcessMemory(Handle, address, oldBuffer, size, out _);

			// Marshal the struct to newBuffer
			byte[] newbuffer = new byte[size];
			IntPtr mem = Marshal.AllocHGlobal(size);

			Marshal.StructureToPtr<T>(value, mem, false);
			Marshal.Copy(mem, newbuffer, 0, size);
			Marshal.FreeHGlobal(mem);

			// Write the oldBuffer (which has now had newBuffer merged over it) to the process
			WriteProcessMemory(Handle, address, newbuffer, size, out _);
		}

		public static bool Read(UIntPtr address, byte[] buffer, UIntPtr size)
		{
			return ReadProcessMemory(Handle, address, buffer, size, IntPtr.Zero);
		}

		public static bool Read(IntPtr address, byte[] buffer, int size = -1)
		{
			if (size <= 0)
				size = buffer.Length;

			return ReadProcessMemory(Handle, address, buffer, size, out _);
		}

		public static bool Write(IntPtr address, byte[] buffer)
		{
			return WriteProcessMemory(Handle, address, buffer, buffer.Length, out _);
		}

		/// <summary>
		/// Open the PC game process with all security and access rights.
		/// </summary>
		private static void OpenProcess(Process process)
		{
			Process = process;

			if (!Process.Responding)
				throw new Exception("Target process id not responding");

			if (process.MainModule == null)
				throw new Exception("Process has no main module");

			Process.EnterDebugMode();
			int debugPrivilegeCheck = CheckSeDebugPrivilege(out bool isDebugEnabled);
			if (debugPrivilegeCheck != 0)
			{
				throw new Exception($"ERROR: CheckSeDebugPrivilege failed with error: {debugPrivilegeCheck}");
			}
			else if (!isDebugEnabled)
			{
				throw new Exception("ERROR: SeDebugPrivilege not enabled. Please report this!");
			}

			Handle = OpenProcess(0x001F0FFF, true, process.Id);
			if (Handle == IntPtr.Zero)
			{
				int eCode = Marshal.GetLastWin32Error();
			}

			// Set all modules
			modules.Clear();
			foreach (ProcessModule? module in Process.Modules)
			{
				if (module == null)
					continue;

				if (string.IsNullOrEmpty(module.ModuleName))
					continue;

				if (modules.ContainsKey(module.ModuleName))
					continue;

				modules.Add(module.ModuleName, module.BaseAddress);
			}

			Scanner = new SignatureScanner(process.MainModule);
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int processId);

		[DllImport("kernel32.dll")]
		private static extern bool IsWow64Process(IntPtr hProcess, out bool lpSystemInfo);

		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer, UIntPtr nSize, IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll")]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		[DllImport("kernel32.dll")]
		private static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize, out IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll")]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll")]
		private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetCurrentProcess();

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, ref LUID lpLuid);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool PrivilegeCheck(IntPtr clientToken, ref PRIVILEGE_SET requiredPrivileges, out bool pfResult);

		[DllImport("kernel32.dll")]
		private static extern int CloseHandle(IntPtr hObject);

		private static int CheckSeDebugPrivilege(out bool isDebugEnabled)
		{
			isDebugEnabled = false;

			if (!OpenProcessToken(GetCurrentProcess(), 0x8 /*TOKEN_QUERY*/, out IntPtr tokenHandle))
				return Marshal.GetLastWin32Error();

			LUID luidDebugPrivilege = default;
			if (!LookupPrivilegeValue(null, "SeDebugPrivilege", ref luidDebugPrivilege))
				return Marshal.GetLastWin32Error();

			PRIVILEGE_SET requiredPrivileges = new PRIVILEGE_SET
			{
				PrivilegeCount = 1,
				Control = 1 /* PRIVILEGE_SET_ALL_NECESSARY */,
				Privilege = new LUID_AND_ATTRIBUTES[1],
			};

			requiredPrivileges.Privilege[0].Luid = luidDebugPrivilege;
			requiredPrivileges.Privilege[0].Attributes = 2 /* SE_PRIVILEGE_ENABLED */;

			if (!PrivilegeCheck(tokenHandle, ref requiredPrivileges, out bool bResult))
				return Marshal.GetLastWin32Error();

			// bResult == true => SeDebugPrivilege is on; otherwise it's off
			isDebugEnabled = bResult;

			CloseHandle(tokenHandle);

			return 0;
		}

		public static void GetProcess()
		{
			Process[] processes = Process.GetProcesses();
			Process? proc = null;

			// Search for ffxiv process
			foreach (Process process in processes)
			{
				if (process.ProcessName.ToLower().Contains("ffxiv_dx11"))
				{
					proc = process;
				}
			}
			
			// if still no process, shutdown.
			if (proc == null)
			{
				throw new Exception("No process");
			}

			OpenProcess(proc);
			IsProcessAlive = true;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct PRIVILEGE_SET
		{
			public uint PrivilegeCount;
			public uint Control;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
			public LUID_AND_ATTRIBUTES[] Privilege;
		}

		private struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;
			public uint Attributes;
		}
	}
}
