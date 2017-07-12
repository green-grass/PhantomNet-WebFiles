using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace PhantomNet.AspNetCore.RemoteFolder.Mvc
{
    public abstract class RemoteImageFolderControllerBase<TRemoteImage> : Controller
        where TRemoteImage : class, IRemoteImage, new()
    {
        public RemoteImageFolderControllerBase(RemoteImageFolderService<TRemoteImage> remoteFolderService)
        {
            RemoteFolderService = remoteFolderService;
        }

        protected RemoteImageFolderService<TRemoteImage> RemoteFolderService { get; }

        protected virtual JsonResult InternalImageList(string virtualPath)
        {
            var models = RemoteFolderService.List(virtualPath, url => Url.Content(url));

            if (models == null)
            {
                return Json(new { success = false });
            }

            var list = new List<object>();
            foreach (var model in models)
            {
                list.Add(new {
                    index = list.Count,
                    fileName = model.FileName,
                    fileSize = model.FileSize,
                    url = model.ClientUrl,
                    absoluteUrl = model.AbsoluteUrl,
                    thumbUrl = model.ClientThumbUrl,
                    absoluteThumbUrl = model.AbsoluteThumbUrl
                });
            }

            return Json(new { success = true, items = list });
        }

        protected virtual async Task<JsonResult> InternalUploadImage(string virtualPath, bool single = false)
        {
            var result = await RemoteFolderService.UploadAsync(Request.Form.Files, virtualPath, single);
            return Json(new { success = result.Succeeded, errors = result.Errors });
        }

        protected virtual JsonResult InternalRenameImage(string virtualPath, string fileName, string newName)
        {
            var result = RemoteFolderService.Rename(virtualPath, fileName, newName);
            return Json(new {
                success = result.Succeeded,
                errors = result.Errors,
                newFileName = result.Succeeded ? newName : null,
                fileNotFound = result.Errors.FirstOrDefault()?.Code == nameof(RemoteFolderErrorDescriber.FileNotFound)
            });
        }

        protected virtual JsonResult InternalDeleteImage(string virtualPath, string fileName)
        {
            var result = RemoteFolderService.Delete(virtualPath, fileName);
            return Json(new { success = result.Succeeded, errors = result.Errors });
        }
    }
}
