// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace JAvatar
{
    public class JAvatarMiddleware
    {
        public JAvatarMiddleware (
            RequestDelegate next,
            IWebHostEnvironment env,
            IMemoryCache cache,
            Options options,
            ILogger<JAvatarMiddleware> logger
        )
        {
            _next = next;
            _cache = cache;
            _logger = logger;
            _options = options ?? new Options();

            _options.RootPath = env.WebRootPath;

            if (_options.RoutePrefix.StartsWith("/"))
                _options.RoutePrefix = _options.RoutePrefix.Substring(1);

            if (!_options.Folders.Any())
            {
                _options.Folders = new ImageFolder[]
                {
                    new ImageFolder { Name = "/" }
                };
            }

            Generator.Initialize(_options);
        }

        private readonly RequestDelegate _next;
        private readonly Options _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger _logger;

        public async Task Invoke(HttpContext context, IFileUploadHandler uploader)
        {
            if (context.Request.Method == "POST"
                || context.Request.Method == "PUT")
            {

                await Upload(context, uploader);
                return;

            }

            if (context.Request.Method == "GET")
            {

                if (context.Request.Query.ContainsKey("q"))
                {
                    Query(context);
                    return;
                }

                Fetch(context);
                return;
            }
        }


        private void Fetch(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;

            string basekey = request.Path.Value;
            _cache.TryGetValue(basekey, out string basetag);
            string target = basetag;

            string urltag = "";
            string urlkey = request.Path.Add(request.QueryString);
            if (request.QueryString.HasValue)
            {
                _cache.TryGetValue(urlkey, out urltag);
                target = basetag + urltag;
            }

            string test = request.Headers["If-None-Match"];
            if (test.HasValue() && test == target
            ){
                response.StatusCode = 304;
                AddCacheControl(response);
                return;
            }

            if (!request.Query["z"].Any())
            {
                string ts = DateTime.UtcNow.Ticks.ToString("x");
                if (!basetag.HasValue())
                {
                    basetag = ts;
                    _cache.Set(basekey, basetag);
                }
                if (!urltag.HasValue() && request.QueryString.HasValue)
                {
                    urltag = ts;
                    _cache.Set(urlkey, urltag);
                }

                string etag = request.QueryString.HasValue ? basetag+urltag : basetag;
                response.Headers.Add("ETAG", etag);
                AddCacheControl(response);
            }

            response.StatusCode = 200;
            response.ContentType = "image/png";

            Int32.TryParse(request.Query["x"], out int size);
            Generator.WriteImage(
                request.Path.Value,
                size,
                response.Body
            );
        }

        private void AddCacheControl(HttpResponse response)
        {
            if (_options.CacheSeconds > 0)
            {
                response.Headers.Add("Cache-Control", $"max-age={_options.CacheSeconds}"); //31536000 year
            }
        }

        private void Query(HttpContext context)
        {
            var request = context.Request;
            var response = context.Response;
            response.StatusCode = 200;

            string target = request.Path.Value.Split('/').Skip(2).Take(1).FirstOrDefault() ?? "/";
            if (_options.Folders.Any(f => f.Name == target && f.Browseable))
            {
                string term = request.Query["q"];
                Int32.TryParse(request.Query["s"], out int skip);
                Int32.TryParse(request.Query["t"], out int take);
                string folder = Path.Combine(_options.RootPath, _options.RoutePrefix, target);
                if (!_cache.TryGetValue(folder, out string[] list))
                {
                    list = Directory.GetFiles(folder).Select(f => Path.GetFileName(f)).ToArray();
                    _cache.Set(folder, list);
                }

                response.WriteAsync(
                    JsonSerializer.Serialize(
                        list
                        .Where(x => x.Contains(term))
                        .Skip(skip)
                        .Take((take > 0) ?take : list.Length)
                        .Select(x => $"{request.Scheme}://{request.Host}/{_options.RoutePrefix}/{target}/{x}")
                    )
                );
            } else {
                response.WriteAsync("[]");
            }
        }

        private async Task Upload(HttpContext context, IFileUploadHandler uploader)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                var segments = request.Path.Value.Split('/');
                string target = segments.Skip(2).Take(1).FirstOrDefault() ?? "/";
                var folderOptions = _options.Folders.SingleOrDefault(f => f.Name == target);
                if (folderOptions == null)
                    throw new InvalidOperationException();

                await uploader.Process(
                    request,
                    opts => {
                        opts.MultipartBodyLengthLimit = (long)((_options.MaxFileBytes > 0) ? _options.MaxFileBytes : 1E9);
                    },
                    metadata => {
                        string dest = Path.GetTempFileName();
                        metadata.Add("tempname", dest);
                        return File.Create(dest);
                    },
                    status => {
                        // check for error
                        if (status.Error != null)
                        {
                            string src = status.Metadata["tempname"];
                            if (System.IO.File.Exists(src))
                                System.IO.File.Delete(src);
                        }
                    },
                    async (metadata) => {
                        // complete
                        string original = metadata["filename"];
                        string src = metadata["tempname"];
                        string folder = Path.Combine(_options.RootPath, _options.RoutePrefix, folderOptions.Name);
                        string sub = context.User.FindFirst(c => c.Type == _options.IdClaim)?.Value ?? Guid.Empty.ToString();
                        string fn = NormalizeFileName(original);

                        switch (folderOptions.NameMode)
                        {
                            case FileNameMode.Subject:
                            fn = sub;
                            break;

                            case FileNameMode.SubjectAppend:
                            fn += "_" + sub.Substring(0, 8);
                            break;
                        }

                        string dst = Path.Combine(folder, fn);
                        File.Copy(src, dst, true);
                        File.Delete(src);

                        _logger.LogInformation($"javatar {folderOptions.Name}/{fn} uploaded by {sub}");

                        string path = $"/{_options.RoutePrefix}/{folderOptions.Name}/{fn}";
                        string url = $"{request.Scheme}://{request.Host}{path}";

                        _cache.Set(path, DateTime.UtcNow.Ticks.ToString("x"));
                        _cache.Remove(folder); //TODO: optimize typeahead cache
                        response.StatusCode = 201;
                        await response.WriteAsync($"{{\"url\":\"{url}\"}}");
                    }
                );
            }
            catch (Exception ex)
            {
                response.StatusCode = 400;
                await response.WriteAsync(ex.Message);
            }
        }

        private string NormalizeFileName(string s)
        {
            string r = "";
            foreach (char c in s.Trim().Replace(" ", "-").ToLower().ToCharArray())
                if (Char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
                    r += c;
            return r;
        }
    }
}
