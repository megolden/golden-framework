using System;
using System.Security.Permissions;

namespace System.Windows.Threading
{
	public static class DispatcherHelper
	{
		/// <summary>
		/// Execute the event queue of the dispatcher.
		/// </summary>
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		public static void DoEvents()
		{
			var frame = new DispatcherFrame();
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
			Dispatcher.PushFrame(frame);
		}
		private static object ExitFrame(object frame)
		{
			((DispatcherFrame)frame).Continue = false;
			return null;
		}
	}
}
