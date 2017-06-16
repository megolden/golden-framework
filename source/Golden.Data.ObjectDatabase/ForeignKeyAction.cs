using System;

namespace Golden.Data.ObjectDatabase
{
	public enum ForeignKeyAction
	{
		NoAction = 0,
		Cascade = 1,
		SetNull = 2,
		SetDefault = 3
	}
}
