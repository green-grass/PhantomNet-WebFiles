using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteImageFolderService<TRemoteImage> : RemoteFolderService<TRemoteImage>
        where TRemoteImage : class, IRemoteImage, new()
    {
        public RemoteImageFolderService(
            IHostingEnvironment env,
            IHttpContextAccessor contextAccessor,
            RemoteFolderErrorDescriber errors)
            : base(env, contextAccessor, errors)
        { }

        public override IEnumerable<TRemoteImage> List(string virtualPath, Func<string, string> createClientUrl)
        {
            return List(virtualPath, createClientUrl, false);
        }

        public IEnumerable<TRemoteImage> List(string virtualPath, Func<string, string> createClientUrl,
            bool includeDimensions)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }

            var files = base.List(virtualPath, createClientUrl);

            if (files == null)
            {
                return null;
            }

            foreach (var file in files)
            {
                file.ThumbUrl = file.Url;
                file.ClientThumbUrl = file.ClientUrl;
                file.AbsoluteThumbUrl = file.AbsoluteUrl;
                if (includeDimensions)
                {
                    // TODO:: Waiting for System.Drawing.Graphics
                    //var physicalFile = MapPath(file.Url);
                    //try
                    //{
                    //    using (var image = Image.FromFile(physicalFile))
                    //    {
                    //        file.Width = image.Size.Width;
                    //        file.Height = image.Size.Height;
                    //    }
                    //}
                    //catch
                    //{
                    //    file.Width = 0;
                    //    file.Height = 0;
                    //}
                }
            }

            return files;
        }

        public IEnumerable<TRemoteImage> List(string virtualPath, Func<string, string> createClientUrl,
            string thumbsVirtualPath)
        {
            return List(virtualPath, createClientUrl, thumbsVirtualPath, false);
        }

        public IEnumerable<TRemoteImage> List(string virtualPath, Func<string, string> createClientUrl,
            string thumbsVirtualPath, bool includeDimensions)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }
            if (string.IsNullOrWhiteSpace(thumbsVirtualPath))
            {
                throw new ArgumentNullException(nameof(thumbsVirtualPath));
            }

            var files = List(virtualPath, createClientUrl, includeDimensions);

            if (files == null)
            {
                return null;
            }

            foreach (var file in files)
            {
                file.ThumbUrl = Path.Combine(thumbsVirtualPath, file.FileName);
                try
                {
                    if (!File.Exists(MapPath(file.ThumbUrl)))
                    {
                        throw new FileNotFoundException();
                    }
                }
                catch
                {
                    file.ThumbUrl = file.Url;
                }
                file.ClientThumbUrl = createClientUrl(file.ThumbUrl);
                file.AbsoluteThumbUrl = CreateAbsoluteUrl(file.ThumbUrl);
            }

            return files;
        }
    }
}
