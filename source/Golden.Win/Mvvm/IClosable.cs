using System;
using System.ComponentModel;
using System.Windows.Input;

namespace Golden.Mvvm
{
	public interface IClosable
	{
		//bool CanClose { get; set; }
		//ICommand CloseCommand { get; }
		//bool? Result { get; set; }
		
		//void Close(bool? result);
		bool Close();

		//event CancelEventHandler Closing;
		//event EventHandler Closed;
	}
}
