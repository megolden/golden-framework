using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Golden.Win.Sample.Components
{
    public interface IErrorLogger
    {
        bool Handle(Exception ex);
    }
}
