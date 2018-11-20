using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Golden.Win.Sample.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly Application app;

        public ApplicationService(Application app)
        {
            this.app = app;
        }
        public void Shutdown()
        {
            app.Shutdown();
        }
    }
}
