﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using EmbedIO;
using EmbedIO.WebApi;

using Newtonsoft.Json;

using NSPersonalCloud.Interfaces.Apps;

namespace NSPersonalCloud.Apps.Album
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
    public class AlbumManager : IAppManager
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        private static readonly Dictionary<string, AlbumConfig> Cache = new Dictionary<string, AlbumConfig>();
        private static Task IndexerTask;
        CancellationTokenSource cts;



        AlbumConfig GetFromCache(string key)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(key))
                {
                    return Cache[key];
                }
            }
            return null;
        }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        async Task EmbedIOResponseSerializerCallback(IHttpContext context, object? data)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            context.Response.ContentType = MimeType.Json;
            using var text = context.OpenResponseText(new UTF8Encoding(false));
            await text.WriteAsync(System.Text.Json.JsonSerializer.Serialize(data,
                new System.Text.Json.JsonSerializerOptions() { IgnoreNullValues = true })).ConfigureAwait(false);
        }

        public EmbedIO.WebServer ConfigWebController(string id, string path, EmbedIO.WebServer webServer)
        {
            return webServer.WithWebApi(id, path, EmbedIOResponseSerializerCallback,
                module => module.WithController(() => new AlbumWebController(GetFromCache)));
        }


        /// <summary>
        /// Retrieves the specified [embedded] resource file and saves it to disk.  
        /// If only filename is provided then the file is saved to the default 
        /// directory, otherwise the full filepath will be used.
        /// <para>
        /// Note: if the embedded resource resides in a different assembly use that
        /// assembly instance with this extension method.
        /// </para>
        /// </summary>
        /// <example>
        /// <code>
        ///       Assembly.GetExecutingAssembly().ExtractResource("Ng-setup.cmd");
        ///       OR
        ///       Assembly.GetExecutingAssembly().ExtractResource("Ng-setup.cmd", "C:\temp\MySetup.cmd");
        /// </code>
        /// </example>
        /// <param name="assembly">The assembly.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="fileName">Name of the file.</param>
        static void ExtractResource(Assembly assembly, string filename, string path = null)
        {
            //Construct the full path name for the output file
            var outputFile = path ?? $@"{Directory.GetCurrentDirectory()}\{filename}";

            // Pull the fully qualified resource name from the provided assembly
            using (var resource = assembly.GetManifestResourceStream(filename))
            {
                if (resource == null)
                    throw new FileNotFoundException($"Could not find [{filename}] in {assembly.FullName}!");

                using (var file = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }

        public Task InstallWebStatiFiles(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                var bf = Path.Combine(path, "Album.zip");
                ExtractResource(Assembly.GetExecutingAssembly(), "NSPersonalCloud.Apps.Album.album.zip", bf);
                ZipFile.ExtractToDirectory(bf, path, true);
                File.Delete(bf);
            }
            catch (Exception)
            {

                //throw;
            }
            return Task.CompletedTask;
        }

        async Task ImageIndexerScheduler()
        {
            try
            {
                var tk = cts.Token;//local capture
                var exedir = Path.GetDirectoryName(typeof(AlbumManager).Assembly.Location);
                var exepath = Path.Combine(exedir, "PersonalCloud.ImageIndexer.exe");
                while (true)
                {
                    tk.ThrowIfCancellationRequested();
                    List<AlbumConfig> lis = null;
                    lock (Cache)
                    {
                        lis = Cache.Select(x => x.Value).ToList();
                    }
                    if (lis != null)
                    {
                        foreach (var item in lis)
                        {
                            tk.ThrowIfCancellationRequested();
                            await IndexOneAlbum(exepath, item.MediaFolder, item.ThumbnailFolder, tk).ConfigureAwait(false);
                        }
                    }
                    tk.ThrowIfCancellationRequested();
                    await Task.Delay(60 * 60 * 1000, tk).ConfigureAwait(false);//1 hour
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        static async Task IndexOneAlbum(string exepath, string src,string des,  CancellationToken tk)
        {
            var si = new ProcessStartInfo(exepath,$" -O \"{des}\" -I \"{src}\" ");
            using var proc = new Process();
            proc.StartInfo = si;
            proc.Start();
            int i = 0;
            while (!proc.HasExited)
            {
                if (tk.IsCancellationRequested)
                {
                    proc.Kill();
                }
                await Task.Delay(30 * 1000, tk).ConfigureAwait(false);
                
                if (tk.IsCancellationRequested)
                {
                    proc.Kill();
                }
                ++i;
                if (i>(60*2))
                {
                    proc.Kill();
                }
            }
        }
        void CleanCache()
        {
            lock (Cache)
            {
                Cache.Clear();
                if (cts != null)
                {
                    cts.Cancel();
                    cts.Dispose();
                    IndexerTask = null;
                    cts = null;
                }
            }
        }

        public List<AppLauncher> Config(string configjsons)
        {
            CleanCache();
            Console.WriteLine($"Album config {configjsons}");
            var cfgs = JsonConvert.DeserializeObject<List<AlbumConfig>>(configjsons);
            var lis = new List<AppLauncher>();
            foreach (var cfg in cfgs)
            {

                var appl = new AppLauncher();
                appl.AppType = AppType.Web;
                appl.Name = cfg.Name;
                appl.WebAddress = "/AppsStatic/album/";
                appl.AccessKey = Guid.NewGuid().ToString("N");
                appl.AppId = GetAppId();

                lock (Cache)
                {
                    Cache.Add(appl.AccessKey, cfg);
                    if (IndexerTask==null)
                    {
                        cts = new CancellationTokenSource();
                        IndexerTask = Task.Run(ImageIndexerScheduler,cts.Token);
                    }
                }
                lis.Add(appl);
            }
            return lis;
        }

        public string GetAppId()
        {
            return "Album";
        }

    }
}
