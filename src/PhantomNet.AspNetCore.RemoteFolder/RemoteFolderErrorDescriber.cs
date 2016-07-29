namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteFolderErrorDescriber : ErrorDescriber
    {
        public virtual GenericError FileSystemError(string exception)
        {
            return new GenericError {
                Code = nameof(FileSystemError),
                Description = exception
            };
        }

        public virtual GenericError NoFileToUpload()
        {
            return new GenericError {
                Code = nameof(NoFileToUpload),
                Description = Strings.NoFileToUploadError
            };
        }

        public virtual GenericError ToManyFilesToUpload()
        {
            return new GenericError {
                Code = nameof(ToManyFilesToUpload),
                Description = Strings.ToManyFilesToUploadError
            };
        }

        public virtual GenericError FileNotFound(string fileName)
        {
            return new GenericError {
                Code = nameof(FileNotFound),
                Description = Strings.FormatFileNotFoundError(fileName)
            };
        }
    }
}
