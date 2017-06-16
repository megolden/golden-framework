using System;

namespace Golden.Data.ObjectDatabase
{
    public enum ConstraintType
    {
        Check = 0,
        ForeignKey = 1,
        Unique = 2,
        PrimaryKey = 3,
    }
}
