using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace PhantomNet.AspNetCore.WebFiles
{
    public class WebApiFileProviderBase
    {
        private const int DefaultTokenTimeOut = 15000;

        private readonly HttpContext _context;

        public WebApiFileProviderBase(
            IApiTokenProvider tokenProvider,
            IHttpContextAccessor contextAccessor,
            IOptions<WebApiFileProviderOptions> webApiFileProviderOptions)
        {
            if (tokenProvider == null)
            {
                throw new ArgumentNullException(nameof(tokenProvider));
            }

            if (webApiFileProviderOptions == null)
            {
                throw new ArgumentNullException(nameof(webApiFileProviderOptions));
            }

            TokenProvider = tokenProvider;
            SecretKey = webApiFileProviderOptions.Value.SecretKey ?? string.Empty;
            TokenTimeOut = webApiFileProviderOptions.Value.TokenTimeOut ?? DefaultTokenTimeOut;
            EndPoint = webApiFileProviderOptions.Value.EndPoint;

            _context = contextAccessor?.HttpContext;
        }

        protected IApiTokenProvider TokenProvider { get; }

        protected virtual CancellationToken CancellationToken => _context?.RequestAborted ?? CancellationToken.None;

        protected string SecretKey { get; }

        protected double TokenTimeOut { get; }

        protected string EndPoint { get; }

        protected virtual async Task<dynamic> InternalFileList(string key, string actionName)
        {
            long timeStamp;
            string token;
            TokenProvider.GenerateToken(SecretKey, actionName, key, TokenTimeOut, out timeStamp, out token);
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage() {
                    RequestUri = new Uri(Path.Combine(EndPoint, actionName, $"{key}")),
                    Method = HttpMethod.Get
                };

                request.Headers.Add("timeStamp", timeStamp.ToString());
                request.Headers.Add("token", token);

                try
                {
                    using (var respond = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken))
                    {
                        var respondData = await respond.Content.ReadAsStringAsync();
                        var model = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<dynamic>(respondData));
                        return model;
                    }
                }
                catch (Exception e)
                {
                    // TOTO:: Log error
                    return GenericResult.Failed(new GenericError {
                        Code = actionName,
                        Description = e.Message
                    });
                }
            }
        }

        protected virtual async Task<dynamic> InternalUploadFile(string key, string actionName, IFormFileCollection files)
        {
            long checksum = 0;
            using (var content = new MultipartFormDataContent())
            {
                foreach (var file in files)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    content.Add(fileContent, file.Name, file.FileName);
                    // TODO:: Calculate checksum
                    checksum += file.Length;
                }

                var data = $"{key}{checksum}";
                return await PostContent(key, data, actionName, content);
            }
        }

        protected virtual async Task<dynamic> InternalRenameFile(string key, string actionName, string fileName, string newName)
        {
            var values = new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>(nameof(fileName), fileName),
                    new KeyValuePair<string, string>(nameof(newName), newName)
                };

            var data = $"{key}{fileName}{newName}";
            using (var content = new FormUrlEncodedContent(values))
            {
                return await PostContent(key, actionName, data, content);
            }
        }

        protected virtual async Task<dynamic> InternalDeleteFile(string key, string actionName, string fileName)
        {
            var values = new KeyValuePair<string, string>[] {
                    new KeyValuePair<string, string>(nameof(fileName), fileName),
                };

            var data = $"{key}{fileName}";
            using (var content = new FormUrlEncodedContent(values))
            {
                return await PostContent(key, actionName, data, content);
            }
        }

        protected virtual async Task<dynamic> PostContent(string key, string actionName, string data, HttpContent content)
        {
            long timeStamp;
            string token;
            TokenProvider.GenerateToken(SecretKey, actionName, data, TokenTimeOut, out timeStamp, out token);
            using (var client = new HttpClient())
            {
                content.Headers.Add("timeStamp", timeStamp.ToString());
                content.Headers.Add("token", token);

                var requestUri = Path.Combine(EndPoint, actionName, $"{key}");
                try
                {
                    using (var respond = await client.PostAsync(requestUri, content, CancellationToken))
                    {
                        var respondData = await respond.Content.ReadAsStringAsync();
                        var model = await Task.Factory.StartNew(() => JsonConvert.DeserializeObject<dynamic>(respondData));
                        return model;
                    }
                }
                catch (Exception e)
                {
                    // TOTO:: Log error
                    return GenericResult.Failed(new GenericError {
                        Code = actionName,
                        Description = e.Message
                    });
                }
            }
        }
    }
}
