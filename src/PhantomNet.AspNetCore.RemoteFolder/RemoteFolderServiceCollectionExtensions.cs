using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PhantomNet.AspNetCore.RemoteFolder
{
    public static class RemoteFolderServiceCollectionExtensions
    {
        public static RemoteFolderBuilder AddRemoteFolder(
            this IServiceCollection services, Action<RemoteFolderOptions> setupAction = null)
        {
            return services.AddRemoteFolder<RemoteFile, RemoteImage>(setupAction);
        }

        public static RemoteFolderBuilder AddRemoteFolder<TRemoteFile, TRemoteImage>(
            this IServiceCollection services, Action<RemoteFolderOptions> setupAction = null)
            where TRemoteFile : class, IRemoteFile, new()
            where TRemoteImage : class, IRemoteImage, new()
        {
            // Services used by remote folder
            services.AddOptions();

            // Hosting doesn't add IHttpContextAccessor by default
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // RemoteFolder services
            services.TryAddScoped<RemoteFolderService<TRemoteFile>, RemoteFolderService<TRemoteFile>>();
            services.TryAddScoped<RemoteImageFolderService<TRemoteImage>, RemoteImageFolderService<TRemoteImage>>();
            services.TryAddScoped<RemoteFolderErrorDescriber>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return new RemoteFolderBuilder(typeof(TRemoteFile), typeof(TRemoteImage), services);
        }
    }
}
