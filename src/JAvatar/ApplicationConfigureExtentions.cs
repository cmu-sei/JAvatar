using JAvatar;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApplicationConfigureExtensions
    {
        public static IServiceCollection AddJAvatar(
            this IServiceCollection services,
            System.Func<JAvatar.Options> config = null)
        {
            var options = config?.Invoke() ?? new JAvatar.Options();

            // TODO: remove when ImageSharp supports Async
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            services.AddMemoryCache();

            services.AddSingleton<JAvatar.Options>(options);

            services.AddScoped<IFileUploadHandler, FileUploadHandler>();

            return services;
        }
    }
}
