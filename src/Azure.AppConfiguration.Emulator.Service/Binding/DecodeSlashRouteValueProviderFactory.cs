using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Threading.Tasks;

namespace Azure.AppConfiguration.Emulator.Service
{
    class DecodeSlashRouteValueProviderFactory : IValueProviderFactory
    {
        public Task CreateValueProviderAsync(ValueProviderFactoryContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.ValueProviders.Add(new DecodeSlashRouteValueProvider(
                BindingSource.Path,
                context.ActionContext.RouteData.Values));

            return Task.CompletedTask;
        }
    }
}
