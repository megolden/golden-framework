using System;
using Autofac;
using System.Reflection;
using System.Linq;
using Golden.Mvvm;
using Golden.Win.Sample.Services;
using System.Windows;
using Golden.Win.Sample.Views;

namespace Golden.Win.Sample
{
    public static class DIConfig
    {
        private static Lazy<IContainer> container = new Lazy<IContainer>(() => builder.Build());
        private static ContainerBuilder builder = new ContainerBuilder();

        public static IContainer Injector
        {
            get { return container.Value; }
        }

        static DIConfig()
        {
            builder.Register(c => Injector).As<IContainer>().SingleInstance();
        }
        public static void Register()
        {
            builder.Register(_ => Application.Current).As<Application>();
            builder.RegisterType<Shell>().AsSelf();

            //Services
            builder.RegisterType<ModalService>().As<IModalService>();
            builder.RegisterType<ApplicationService>().As<IApplicationService>();

            //Components
            //builder.RegisterInstance(App.Current).As<IApplication>().SingleInstance();

            //Views
            //Assembly.GetCallingAssembly().ExportedTypes
            //    .Where(t => t.IsClass && !t.IsInterface && !t.IsAbstract && typeof(IView).IsAssignableFrom(t))
            //    .ForEach(t => builder.RegisterType(t).AsImplementedInterfaces());

            //ViewModels
            Assembly.GetCallingAssembly().ExportedTypes
                .Where(t => t.IsClass && !t.IsInterface && !t.IsAbstract && typeof(ViewModelBase).IsAssignableFrom(t))
                .ForEach(t => builder.RegisterType(MvvmHelper.CreateViewModelProxyType(t)).As(t));
        }
    }
}