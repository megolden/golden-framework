using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Golden.Utility
{
	internal static class PInvokeHelper
	{
		[DllImport("Wininet.dll")]
		public static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
		[DllImport("User32.dll")]
		public static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);
		[DllImport("Kernel32.dll")]
		public extern static uint SetSystemTime(ref SYSTEMTIME lpSystemTime);

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEMTIME
		{
			public ushort wYear;
			public ushort wMonth;
			public ushort wDayOfWeek;
			public ushort wDay;
			public ushort wHour;
			public ushort wMinute;
			public ushort wSecond;
			public ushort wMilliseconds;
		}
	}
}
