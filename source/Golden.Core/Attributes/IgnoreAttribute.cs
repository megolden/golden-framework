using System;

namespace Golden.Attributes
{
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public class IgnoreAttribute : Attribute
	{
	}
}
