using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteFolderBuilder
    {
        public RemoteFolderBuilder(Type remoteFileType, Type remoteImageType, IServiceCollection services)
        {
            RemoteFileType = remoteFileType;
            RemoteImageType = remoteFileType;
            Services = services;
        }

        #region Properties

        public Type RemoteFileType { get; }

        public Type RemoteImageType { get; }

        public IServiceCollection Services { get; }

        #endregion

        public virtual RemoteFolderBuilder AddRemoteFolderService<T>() where T : class
        {
            var remoteFolderServiceType = typeof(RemoteFolderService<>).MakeGenericType(RemoteFileType);
            var customType = typeof(T);
            if (remoteFolderServiceType == customType ||
                !remoteFolderServiceType.GetTypeInfo().IsAssignableFrom(customType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatInvalidServiceType(customType.Name, nameof(RemoteFolderService<RemoteFile>), RemoteFileType.Name));
            }

            Services.AddScoped(customType, services => services.GetRequiredService(remoteFolderServiceType));
            return AddScoped(remoteFolderServiceType, customType);
        }

        public virtual RemoteFolderBuilder AddRemoteImageFolderService<T>() where T : class
        {
            var remoteImageFolderServiceType = typeof(RemoteImageFolderService<>).MakeGenericType(RemoteImageType);
            var customType = typeof(T);
            if (remoteImageFolderServiceType == customType ||
                !remoteImageFolderServiceType.GetTypeInfo().IsAssignableFrom(customType.GetTypeInfo()))
            {
                throw new InvalidOperationException(Resources.FormatInvalidServiceType(customType.Name, nameof(RemoteImageFolderService<RemoteImage>), RemoteImageType.Name));
            }

            Services.AddScoped(customType, services => services.GetRequiredService(remoteImageFolderServiceType));
            return AddScoped(remoteImageFolderServiceType, customType);
        }

        public virtual RemoteFolderBuilder AddErrorDescriber<T>() where T : RemoteFolderErrorDescriber
        {
            Services.AddScoped<RemoteFolderErrorDescriber, T>();
            return this;
        }

        private RemoteFolderBuilder AddScoped(Type serviceType, Type concreteType)
        {
            Services.AddScoped(serviceType, concreteType);
            return this;
        }
    }
}
