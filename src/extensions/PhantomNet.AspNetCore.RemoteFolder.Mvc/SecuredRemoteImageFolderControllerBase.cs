using System;
using Microsoft.AspNetCore.Http;

namespace PhantomNet.AspNetCore.RemoteFolder.Mvc
{
    public abstract class SecuredRemoteImageFolderControllerBase<TRemoteImage> : RemoteImageFolderControllerBase<TRemoteImage>
        where TRemoteImage : class, IRemoteImage, new()
    {
        public SecuredRemoteImageFolderControllerBase(
            RemoteImageFolderService<TRemoteImage> remoteFolderService,
            IApiTokenProvider tokenProvider,
            string secretKey)
            : base(remoteFolderService)
        {
            TokenProvider = tokenProvider;
            SecretKey = secretKey;
        }

        protected IApiTokenProvider TokenProvider { get; }

        protected string SecretKey { get; }

        protected virtual void ValidateToken(string actionName, string key, IFormFileCollection files)
        {
            long checksum = 0;
            foreach (var file in files)
            {
                // TODO:: Calculate checksum
                checksum += file.Length;
            }

            ValidateToken(actionName, key, checksum.ToString());
        }

        protected virtual void ValidateToken(string actionName, params string[] data)
        {
            long timeStamp;
            try
            {
                timeStamp = long.Parse(Request.Headers["timeStamp"]);
            }
            catch
            {
                // TODO:: Log error, error message
                throw new InvalidOperationException();
            }
            var token = Request.Headers["token"];
            if (!TokenProvider.ValidateToken(SecretKey, actionName, string.Join(string.Empty, data), timeStamp, token))
            {
                // TODO:: Log error, error message
                throw new UnauthorizedAccessException();
            }
        }
    }
}
