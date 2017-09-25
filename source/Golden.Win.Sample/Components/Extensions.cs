namespace Autofac
{
    using System.Linq;
    using System.Collections.Generic;

    public static class ResolutionExtensionsEx
    {
        public static TService Resolve<TService>(this IComponentContext context, params object[] parameters)
        {
            return context.Resolve<TService>(parameters.Select((v, i) => new PositionalParameter(i, v)));
        }
    }
}
