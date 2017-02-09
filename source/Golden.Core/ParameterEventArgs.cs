using System;
using System.ComponentModel;

namespace Golden
{
	public class ParameterEventArgs<T> : EventArgs
	{
		public T Result { get; set; }

		public ParameterEventArgs() : this(default(T)) { }
		public ParameterEventArgs(T parameter)
		{
			this.Result = parameter;
		}
	}
	public class ParameterCancelEventArgs<T> : CancelEventArgs
	{
		public T Parameter { get; set; }

		public ParameterCancelEventArgs() : this(default(T)) { }
		public ParameterCancelEventArgs(T parameter) : this(parameter, false)
		{
		}
		public ParameterCancelEventArgs(bool cancel) : this(default(T), cancel)
		{
		}
		public ParameterCancelEventArgs(T parameter, bool cancel) : base(cancel)
		{
			this.Parameter = parameter;
		}
	}

	public delegate void ParameterEventHandler<T>(object sender, ParameterEventArgs<T> e);
	public delegate void ParameterCancelEventHandler<T>(object sender, ParameterCancelEventArgs<T> e);
}
