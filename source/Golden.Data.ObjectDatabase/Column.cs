using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
    public class Column : DbObjectBase
    {
        public DataType DataType { get; internal set; }
        public bool IsNullable { get; internal set; }
        public bool IsIdentity { get; internal set; }
        public bool IsComputed { get; internal set; }
        public bool InPrimaryKey { get; internal set; }
        public bool IsForeignKey { get; internal set; }
        /// <summary>
        /// Determines whether the current column has unique constraint.
        /// </summary>
        public bool IsUnique { get; internal set; }

        public Column() : this(null)
        {
        }
        public Column(string name, DataType dataType = null) : this(null, name, dataType)
        {
        }
        public Column(DbObjectBase parent, string name, DataType dataType = null) : base(name)
        {
            this.Parent = parent;
            this.DataType = dataType;
        }
    }
}
