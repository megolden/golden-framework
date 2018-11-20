using System;

namespace Golden.Annotations
{
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
	public class IgnoreAttribute : Attribute
	{
	}
}
