using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteFolderService<TRemoteFile>
        where TRemoteFile : class, IRemoteFile, new()
    {
        public const string FileIdHeader = "X-File-ID";
        public const string PartIndexHeader = "X-Part-Index";
        public const string PartCountHeader = "X-Part-Count";
        public const string PartSizeHeader = "X-Part-Size";
        public const string FileNameHeader = "X-File-Name";

        public RemoteFolderService(
            IHostingEnvironment env,
            IHttpContextAccessor contextAccessor,
            RemoteFolderErrorDescriber errors)
        {
            Context = contextAccessor.HttpContext;
            PhysicalBasePath = env.WebRootPath;
            ErrorDescriber = errors;
        }

        protected HttpContext Context { get; }

        protected string PhysicalBasePath { get; }

        private RemoteFolderErrorDescriber ErrorDescriber { get; }

        public virtual IEnumerable<TRemoteFile> List(string virtualPath, Func<string, string> createClientUrl)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }

            var physicalPath = MapPath(virtualPath);
            List<string> files;
            try
            {
                if (!Directory.Exists(physicalPath))
                {
                    return new TRemoteFile[0];
                }

                files = Directory.GetFiles(physicalPath).ToList();
            }
            catch
            {
                return null;
            }

            return files.Select(x => {
                var fileName = Path.GetFileName(x);
                var url = Path.Combine(virtualPath, fileName).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var clientUrl = createClientUrl(url);
                var absoluteUrl = CreateAbsoluteUrl(clientUrl);

                return new TRemoteFile {
                    Index = files.IndexOf(x),
                    FileName = fileName,
                    FileSize = new FileInfo(x).Length,
                    Url = url,
                    ClientUrl = clientUrl,
                    AbsoluteUrl = absoluteUrl
                };
            }).ToArray();
        }

        public virtual Task<GenericResult> UploadAsync(IFormFile file, string virtualPath)
        {
            return UploadAsync(file, virtualPath, null, true);
        }

        public virtual async Task<GenericResult> UploadAsync(IFormFile file, string virtualPath,
            string fileNameWithoutExtension, bool urlFriendly)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            string physicalPath;
            try { physicalPath = CreateDirectoryIfNotExists(virtualPath); }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            return await SaveAsync(file, physicalPath, fileNameWithoutExtension, urlFriendly);
        }

        public virtual Task<GenericResult> UploadAsync(IEnumerable<IFormFile> files, string virtualPath, bool single)
        {
            return UploadAsync(files, virtualPath, single, true);
        }

        public virtual async Task<GenericResult> UploadAsync(IEnumerable<IFormFile> files, string virtualPath, bool single,
            bool urlFriendly)
        {
            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }
            if (virtualPath == null)
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }

            string physicalPath;
            try { physicalPath = CreateDirectoryIfNotExists(virtualPath); }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            if (files.Count() == 0)
            {
                return GenericResult.Failed(ErrorDescriber.NoFileToUpload());
            }

            if (single)
            {
                if (files.Count() > 1)
                {
                    return GenericResult.Failed(ErrorDescriber.ToManyFilesToUpload());
                }

                var existingFiles = Directory.EnumerateFiles(physicalPath);
                try
                {
                    foreach (var file in existingFiles)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception e)
                {
                    return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
                }
            }

            List<GenericResult> results = new List<GenericResult>(files.Count());
            foreach (var file in files)
            {
                // Saving files simultaneously would cause stream read error.
                results.Add(await SaveAsync(file, physicalPath, null, urlFriendly));
            }

            if (results.Any(result => !result.Succeeded))
            {
                return GenericResult.Failed(results.Where(result => !result.Succeeded)
                                                   .SelectMany(result => result.Errors)
                                                   .ToArray());
            }

            return GenericResult.Success;
        }

        public virtual Task<GenericResult> ProgressiveUploadAsync(string virtualPath, string partsVirtualPath)
        {
            return ProgressiveUploadAsync(virtualPath, partsVirtualPath, null, true);
        }

        public virtual async Task<GenericResult> ProgressiveUploadAsync(string virtualPath, string partsVirtualPath,
            string fileNameWithoutExtension, bool urlFriendly)
        {
            if (virtualPath == null)
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }
            if (partsVirtualPath == null)
            {
                throw new ArgumentNullException(nameof(partsVirtualPath));
            }

            string partsPath;
            try { partsPath = CreateDirectoryIfNotExists(partsVirtualPath); }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            var fileId = WebUtility.UrlDecode(Context.Request.Headers[FileIdHeader]);
            var partIndex = int.Parse(Context.Request.Headers[PartIndexHeader]);
            var partCount = int.Parse(Context.Request.Headers[PartCountHeader]);
            var partSize = int.Parse(Context.Request.Headers[PartSizeHeader]);
            var buff = new byte[partSize];
            var partFullPath = Path.Combine(partsPath, fileId);

            await Context.Request.Body.ReadAsync(buff, 0, buff.Length);
            try
            {
                using (var stream = File.OpenWrite(partFullPath))
                {
                    stream.Position = stream.Length;
                    await stream.WriteAsync(buff, 0, buff.Length);
                }
            }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            if (partIndex + 1 < partCount)
            {
                return GenericResult.Success;
            }

            // Save the completed file
            string physicalPath;
            try { physicalPath = CreateDirectoryIfNotExists(virtualPath); }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            var fileName = WebUtility.UrlDecode(Context.Request.Headers[FileNameHeader]);
            var fullPath = IncrementFileNameIfExists(physicalPath, fileName, fileNameWithoutExtension, urlFriendly);

            try
            {
                File.Move(partFullPath, fullPath);
            }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            return GenericResult.Success;
        }

        public virtual GenericResult Rename(string virtualPath, string fileName, string newName)
        {
            if (virtualPath == null)
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (newName == null)
            {
                throw new ArgumentNullException(nameof(newName));
            }

            var physicalPath = MapPath(Path.Combine(virtualPath, fileName));
            var physicalNewPath = MapPath(Path.Combine(virtualPath, newName));
            if (File.Exists(physicalPath))
            {
                try
                {
                    File.Move(physicalPath, physicalNewPath);
                }
                catch (Exception e)
                {
                    return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
                }
                return GenericResult.Success;
            }
            else
            {
                return GenericResult.Failed(ErrorDescriber.FileNotFound(fileName));
            }
        }

        public virtual GenericResult Delete(string virtualPath, string fileName)
        {
            if (virtualPath == null)
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            var physicalPath = MapPath(Path.Combine(virtualPath, fileName));
            if (File.Exists(physicalPath))
            {
                try
                {
                    File.Delete(physicalPath);
                }
                catch (Exception e)
                {
                    return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
                }
                return GenericResult.Success;
            }
            else
            {
                return GenericResult.Success;
            }
        }

        #region Helpers

        protected virtual async Task<GenericResult> SaveAsync(IFormFile file, string physicalPath,
            string fileNameWithoutExtension, bool urlFriendly)
        {
            var fullPath = IncrementFileNameIfExists(physicalPath, file.FileName, fileNameWithoutExtension, urlFriendly);

            try
            {
                using (var fileStream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
            }
            catch (Exception e)
            {
                return GenericResult.Failed(ErrorDescriber.FileSystemError(e.Message));
            }

            return GenericResult.Success;
        }

        protected virtual string CreateDirectoryIfNotExists(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath))
            {
                throw new ArgumentNullException(nameof(virtualPath));
            }

            var path = MapPath(virtualPath);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        protected virtual string IncrementFileNameIfExists(string physicalPath, string fileName,
            string fileNameWithoutExtension, bool urlFriendly)
        {
            fileNameWithoutExtension = fileNameWithoutExtension ?? Path.GetFileNameWithoutExtension(fileName);
            if (urlFriendly)
            {
                fileNameWithoutExtension = fileNameWithoutExtension.ToUrlFriendly();
            }
            var extension = Path.GetExtension(fileName);
            fileName = $"{fileNameWithoutExtension}{extension}";

            string fullPath;
            if (File.Exists(fullPath = Path.Combine(physicalPath, fileName)))
            {
                var index = 1;
                while (File.Exists(fullPath = Path.Combine(physicalPath, $"{fileNameWithoutExtension}-{index}{extension}")))
                {
                    index++;
                }
            }

            return fullPath;
        }

        protected virtual string MapPath(string virtualPath)
        {
            return Path.Combine(PhysicalBasePath, virtualPath.TrimStart('~').TrimStart('/'))
                .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        protected virtual string CreateAbsoluteUrl(string clientUrl)
        {
            return string.Concat(
                Context.Request.Scheme,
                "://",
                Context.Request.Host.ToUriComponent(),
                Context.Request.PathBase.ToUriComponent(),
                clientUrl);
        }

        #endregion
    }
}
