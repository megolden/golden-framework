using System;
using Golden.Data.Extensions;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace Golden.Tests
{
    partial class DBTestDbContext
    {
        [NiladicFunction("CURRENT_USER")]
        public string CURRENT_USER()
        {
            return this.ExecuteNiladicFunction<string>();
        }
        [NiladicFunction("@@ERROR")]
        public int? ERROR()
        {
            return this.ExecuteNiladicFunction<int?>();
        }
        [NiladicFunction("@@TRANCOUNT")]
        public int? TRANCOUNT()
        {
            return this.ExecuteNiladicFunction<int?>();
        }
        [NiladicFunction("@@IDENTITY")]
        public decimal? IDENTITY()
        {
            return this.ExecuteNiladicFunction<decimal?>();
        }
        [NiladicFunction("@@ROWCOUNT")]
        public int? ROWCOUNT()
        {
            return this.ExecuteNiladicFunction<int?>();
        }
    }
}
