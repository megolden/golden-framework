using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
	public abstract class DbSchemaObject : DbObjectBase
	{
		public virtual string Schema { get; internal set; }

		public string FullName
		{
			get
			{
				if (string.IsNullOrEmpty(this.Schema)) return this.Name;
				return string.Concat(this.Schema, ".", this.Name);
			}
		}
		public string QuoteFullName
		{
			get { return this.QuoteIdentifier(this.FullName); }
		}

		public DbSchemaObject() : this(null)
		{
		}
		public DbSchemaObject(string name)
		{
			if (string.Empty.Equals(name))
			{
				this.Name = name;
			}
			else if (name != null)
			{
				var i = name.LastIndexOf('.');
				if (i >= 0)
				{
					this.Name = name.Remove(0, i + 1);
					this.Schema = name.Remove(i);
				}
				else
				{
					this.Name = name;
				}
			}
		}
		public DbSchemaObject(string name, string schema) : this((string.IsNullOrEmpty(schema) ? name : string.Concat(schema, ".", name)))
		{
		}
		public override string ToString()
		{
			return this.QuoteFullName;
		}
	}
}
