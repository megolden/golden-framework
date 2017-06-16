using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Golden.Data.ObjectDatabase
{
    public class ForeignKey : DbObjectBase
    {
        private readonly ReadOnlyCollection<ForeignKeyColumn> _Columns;

        public ForeignKeyAction DeleteAction { get; internal set; }
        public ForeignKeyAction UpdateAction { get; internal set; }
        public ReadOnlyCollection<ForeignKeyColumn> Columns { get { return _Columns; } }
        public new Table Parent
        {
            get { return base.Parent as Table; }
            internal set { base.Parent = value; }
        }
        public Table ReferencedTable { get; internal set; }

        public ForeignKey(IEnumerable<ForeignKeyColumn> columns, string name = null) : base(name)
        {
            if (columns == null) throw new ArgumentNullException(nameof(columns));
            var cols = columns.ToArray();
            if (cols.Length == 0) throw new ArgumentException("Invalid foreign key");

            _Columns = new ReadOnlyCollection<ForeignKeyColumn>(cols);
        }
    }
    public class ForeignKeyColumn : DbObjectBase
    {
        public override string Name
        {
            get
            {
                return (this.Column?.Name);
            }
            internal set
            {
                throw new InvalidOperationException();
            }
        }
        public new ForeignKey Parent
        {
            get { return base.Parent as ForeignKey; }
            internal set { base.Parent = value; }
        }
        public Column Column { get; internal set; }
        public Column ReferencedColumn { get; internal set; }
    }
}
