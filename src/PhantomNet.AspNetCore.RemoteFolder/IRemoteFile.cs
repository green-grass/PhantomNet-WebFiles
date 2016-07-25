namespace PhantomNet.AspNetCore.RemoteFolder
{
    public interface IRemoteFile
    {
        int Index { get; set; }

        string FileName { get; set; }

        long FileSize { get; set; }

        string Url { get; set; }

        string ClientUrl { get; set; }

        string AbsoluteUrl { get; set; }
    }
}
