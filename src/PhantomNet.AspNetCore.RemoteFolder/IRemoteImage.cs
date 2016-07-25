namespace PhantomNet.AspNetCore.RemoteFolder
{
    public interface IRemoteImage : IRemoteFile
    {
        string ThumbUrl { get; set; }

        string ClientThumbUrl { get; set; }

        string AbsoluteThumbUrl { get; set; }

        int Width { get; set; }

        int Height { get; set; }
    }
}
