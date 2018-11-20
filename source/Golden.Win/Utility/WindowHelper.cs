namespace Golden.Win.Utility
{
	using System;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;

	public static class WindowHelper
	{
		#region Constants

		private const int GWL_STYLE = -16;
		private const int WS_MAXIMIZEBOX = 0x10000;
		private const int WS_MINIMIZEBOX = 0x20000;
		private const int WS_SYSMENU = 0x80000;
		private const int GWL_EXSTYLE = -20;
		private const int WS_EX_DLGMODALFRAME = 0x0001;
		//private const int WS_EX_LEFT = 0x0;
		private const int WS_EX_RIGHT = 0x1000;
		private const int WS_EX_RTLREADING = 0x2000;
		//private const int WS_EX_LAYOUTRTL = 0x400000;
		private const int SC_RESTORE = 0xF120;
		private const int SC_MAXIMIZE = 0xF030;
		private const int SC_MINIMIZE = 0xF020;
		private const int SC_CLOSE = 0xF060;
		private const int SC_MOVE = 0xF010;
		private const int SC_SIZE = 0xF000;
		private const int SM_SEPARATOR = 0x0;

		#endregion
		#region APIFunctions

		[DllImport("user32.dll")]
		private static extern int GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hwnd, bool revert);
		[DllImport("user32.dll")]
		private static extern bool DeleteMenu(IntPtr hMenu, uint position, uint flags);
		[DllImport("user32.dll")]
		private static extern int InvalidateRect(IntPtr hwnd, IntPtr lpRect, int bErase);
		[DllImport("user32.dll")]
		private static extern int DrawMenuBar(IntPtr hwnd);
		[DllImport("user32.dll")]
		private static extern int EnableMenuItem(IntPtr hMenu, int wIDEnableItem, int wEnable);

		#endregion
		#region Methods

		public static void RemoveIcon(IntPtr handle)
		{
			int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
			SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_DLGMODALFRAME);

			InvalidateWindow(handle);
		}
		public static void RemoveMaximizeButton(IntPtr handle)
		{
			int style = GetWindowLong(handle, GWL_STYLE);
			SetWindowLong(handle, GWL_STYLE, style & ~WS_MAXIMIZEBOX);

			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_MAXIMIZE, 0);

			InvalidateMenu(handle);
		}
		public static void RemoveMinimizeButton(IntPtr handle)
		{
			int style = GetWindowLong(handle, GWL_STYLE);
			SetWindowLong(handle, GWL_STYLE, style & ~WS_MINIMIZEBOX);

			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_MINIMIZE, 0);

			InvalidateMenu(handle);
		}
		public static void RemoveControlBox(IntPtr handle)
		{
			int style = GetWindowLong(handle, GWL_STYLE);
			SetWindowLong(handle, GWL_STYLE, style & ~WS_SYSMENU);

			InvalidateWindow(handle);
		}
		public static void RemoveCloseButton(IntPtr handle)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_CLOSE, 0);

			InvalidateMenu(handle);
		}
		public static void EnableCloseButton(IntPtr handle, bool enable)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			EnableMenuItem(hMenu, SC_CLOSE, (enable ? 0 : 1));

			InvalidateMenu(handle);
		}
		public static void RemoveResizeMenuItem(IntPtr handle)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_SIZE, 0);

			InvalidateMenu(handle);
		}
		public static void RemoveMoveMenuItem(IntPtr handle)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_MOVE, 0);

			InvalidateMenu(handle);
		}
		public static void RemoveRestoreMenuItem(IntPtr handle)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SC_RESTORE, 0);

			InvalidateMenu(handle);
		}
		public static void RemoveSeparatorMenuItem(IntPtr handle)
		{
			IntPtr hMenu = GetSystemMenu(handle, false);
			DeleteMenu(hMenu, SM_SEPARATOR, 0);

			InvalidateMenu(handle);
		}
		public static void SetRightAlignment(IntPtr handle)
		{
			int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
			SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_RIGHT | WS_EX_RTLREADING);

			InvalidateWindow(handle);
		}
		public static IntPtr GetHandle(this System.Windows.Window window)
		{
			if (window == null) throw new ArgumentNullException("window");
			return (new System.Windows.Interop.WindowInteropHelper(window)).Handle;
		}
		private static void InvalidateMenu(IntPtr handle)
		{
			DrawMenuBar(handle);
		}
		private static void InvalidateWindow(IntPtr handle)
		{
			InvalidateRect(handle, IntPtr.Zero, 0);
		}

		#endregion
	}
}