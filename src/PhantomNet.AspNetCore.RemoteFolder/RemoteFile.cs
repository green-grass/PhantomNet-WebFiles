namespace PhantomNet.AspNetCore.RemoteFolder
{
    public class RemoteFile : IRemoteFile
    {
        public int Index { get; set; }

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public string Url { get; set; }

        public string ClientUrl { get; set; }

        public string AbsoluteUrl { get; set; }
    }
}
