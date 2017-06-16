using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
	public abstract class DbObjectBase
	{
        public int Id { get; internal set; }
		public virtual string Name { get; internal set; }
		public virtual DbObjectBase Parent { get; internal set; }
        public object UserData { get; set; }
        public virtual string QuoteName
		{
			get { return this.QuoteIdentifier(this.Name); }
		}

		public DbObjectBase() : this(null)
		{
		}
		public DbObjectBase(string name = null)
		{
			if (name != null) this.Name = name;
		}
		protected string QuoteIdentifier(string name)
		{
			if (string.IsNullOrEmpty(name) || name.StartsWith("[")) return name;
			return string.Concat("[", name.Replace(".", "].["), "]");

			//if (string.IsNullOrEmpty(name)) return name;
			//var parts = name.Split('.');
			//var temp = new StringBuilder();
			//foreach (var part in parts)
			//{
			//	if (part.StartsWith("["))
			//		temp.AppendFormat("{0}.", part);
			//	else
			//		temp.AppendFormat("[{0}].", part);
			//}
			//if (temp.Length > 0) temp.Remove(temp.Length - 1, 1);
			//return temp.ToString();
		}
		public override string ToString()
		{
			return this.QuoteName;
		}
	}
}
