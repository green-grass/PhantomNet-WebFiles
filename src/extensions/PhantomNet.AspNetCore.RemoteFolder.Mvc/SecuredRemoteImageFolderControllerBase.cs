using System;

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

        protected virtual void ValidateToken(string actionName, string data)
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
            if (!TokenProvider.ValidateToken(SecretKey, actionName, data, timeStamp, token))
            {
                // TODO:: Log error, error message
                throw new UnauthorizedAccessException();
            }
        }
    }
}
