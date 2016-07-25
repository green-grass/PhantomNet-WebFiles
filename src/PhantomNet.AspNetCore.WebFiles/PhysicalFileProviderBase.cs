using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace PhantomNet.AspNetCore.WebFiles
{
    public abstract class PhysicalFileProviderBase
    {
        public PhysicalFileProviderBase(string defaultVirtualBasePath, IHostingEnvironment env, IOptions<PhysicalFileProviderOptions> optionsAccessor)
        {
            VirtualBasePath = optionsAccessor.Value.VirtualBasePath ?? defaultVirtualBasePath;
            PhysicalBasePath = env.WebRootPath;
        }

        protected string VirtualBasePath { get; }

        protected string PhysicalBasePath { get; }

        protected string GenerateVirtualPath(string key)
            => Path.Combine(VirtualBasePath, key)
                   .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        protected string GeneratePhysicalPath(string key)
            => Path.Combine(PhysicalBasePath, GenerateVirtualPath(key).TrimStart('~').TrimStart('/'))
                   .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        protected string FormatVirtualPath(string key, string physicalPath)
        {
            return Path.Combine(GenerateVirtualPath(key), Path.GetFileName(physicalPath)).Replace('\\', '/');
        }

        protected string FormatVirtualPath(string key, string relativePath, string physicalPath)
        {
            return Path.Combine(GenerateVirtualPath(key), relativePath, Path.GetFileName(physicalPath)).Replace('\\', '/');
        }
    }
}
