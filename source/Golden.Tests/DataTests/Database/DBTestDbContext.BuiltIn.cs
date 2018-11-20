using System;
using Golden.Data.Extensions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Golden.Tests
{
    partial class DBTestDbContext
    {
        [BuiltInFunction("LEFT")]
        public string LEFT(string expression, int? length)
        {
            return this.ExecuteBuiltInFunction<string>(expression, length);
        }
        [BuiltInFunction("RIGHT")]
        public string RIGHT(string expression, int? length)
        {
            return this.ExecuteBuiltInFunction<string>(expression, length);
        }
        [BuiltInFunction("REVERSE")]
        public string REVERSE(string expression)
        {
            return this.ExecuteBuiltInFunction<string>(expression);
        }
    }
}
