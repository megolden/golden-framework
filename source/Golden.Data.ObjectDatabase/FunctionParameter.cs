using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
    public class FunctionParameter : DbObjectBase
    {
        public bool IsOutput { get; private set; }
        public DataType DataType { get; private set; }
        public bool IsReadOnly { get; private set; }

        public FunctionParameter(string name = null, DataType type = null, bool isReadOnly = false, bool isOutput = false) : base(name)
        {
            this.DataType = type;
            this.IsReadOnly = isReadOnly;
            this.IsOutput = isOutput;
        }
    }
}
