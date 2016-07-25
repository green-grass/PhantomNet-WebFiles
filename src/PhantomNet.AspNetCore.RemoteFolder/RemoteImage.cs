namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteImage : RemoteFile, IRemoteImage
    {
        public string ThumbUrl { get; set; }

        public string ClientThumbUrl { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public string AbsoluteThumbUrl { get; set; }
    }
}
