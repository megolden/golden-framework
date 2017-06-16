using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Golden.Mvvm
{
	public class DelegateCommand : DelegateCommand<object>
	{
		public DelegateCommand(Action action) : this(action, null) { }
		public DelegateCommand(Action action, Func<bool> canExecute) : base(param => action(), (canExecute != null ? param => canExecute() : (Func<object, bool>)null))
		{
		}
	}
	public class DelegateCommand<T> : ICommand
	{
		private readonly Action<T> action;
		private readonly Func<T, bool> canExecute;

		public DelegateCommand(Action<T> action) : this(action, (Func<T, bool>)null) { }
		public DelegateCommand(Action<T> action, Func<T, bool> canExecute)
		{
			this.action = action;
			this.canExecute = canExecute;
		}
		public bool CanExecute(object parameter)
		{
			return (this.canExecute != null ? this.canExecute(Golden.Utility.Utilities.Convert<T>(parameter)) : true);
		}
		public void Execute(object parameter)
		{
			this.action(Golden.Utility.Utilities.Convert<T>(parameter));
		}

		event EventHandler ICommand.CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}
	}
}
