using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
    public class Schema : DbObjectBase
    {
        public new Database Parent
        {
            get { return base.Parent as Database; }
            internal set { base.Parent = value; }
        }
        public int? OwnerId { get; internal set; }

        public Schema(int id, string name, int? ownerId = null) : base(name)
        {
            base.Id = id;
            this.OwnerId = ownerId;
        }
    }
}
