﻿using System;
using System.Globalization;
using System.IO;
using System.Threading;

using Microsoft.Extensions.Logging;

using NUnit.Framework;

using NSPersonalCloud;
using System.Threading.Tasks;
using EmbedIO;
using NSPersonalCloud.Apps.Album;
using System.Collections.Generic;
using Newtonsoft.Json;
using Zio.FileSystems;

namespace LocalHosted
{
#pragma warning disable CA1303 // Do not pass literals as localized parameters
    public class LocalServiceTest
    {
        public static Zio.IFileSystem Getfs(string path)
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            var fs = new PhysicalFileSystem();
#pragma warning restore CA2000 // Dispose objects before losing scope
            fs.CreateDirectory(fs.ConvertPathFromInternal(path));
            return new SubFileSystem(fs, fs.ConvertPathFromInternal(path), true);   
        }

        [Test]
        public void SimpleCreate()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt")))
            {
                var inf = new HostPlatformInfo();
                using (var srv = new PCLocalService(inf,
                    loggerFactory, Getfs(inf.GetConfigFolder()), null))
                {
                    srv.StartService();
                    var pc = srv.CreatePersonalCloud("test", "testfolder");
                    Thread.Sleep(1000);
                    var lis = pc.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                    Assert.AreEqual( 1, lis.Count);
                }
            }
        }

        [Test]
        public void SimpleShare()
        {

            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt",LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(inf1,
                 loggerFactory, Getfs(inf1.GetConfigFolder()), null))
                {
                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2,
                    loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv1.StartService();
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        var pc1 = srv1.CreatePersonalCloud("test", "test1");

                        var ret = srv1.SharePersonalCloud(pc1);
                        Thread.Sleep(3000);
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        Thread.Sleep(1000);

                        SimpleShareCheckContent(pc2, 2,2);
                        SimpleShareCheckContent(pc1, 2,2);
                    }
                }
            }
        }


        [Test]
        public async Task SimpleApp()
        {

            var my = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dic = Path.Combine(my, "TestConsoleApp", "webapps");

            var t1 = new SimpleConfigStorage(
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TestConsoleApp", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)));
            Directory.CreateDirectory(dic);


            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(t1, loggerFactory, Getfs(t1.RootPath), dic))
                {
                    srv1.InstallApps().Wait();

                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2, loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv1.StartService();
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        var pc1 = srv1.CreatePersonalCloud("test", "test1");

                        var strcfig = JsonConvert.SerializeObject(new List<AlbumConfig>() {
                            new AlbumConfig {
                                MediaFolder= @"F:\pics",
                                Name="test",
                                ThumbnailFolder=@"D:\Projects\out"
                            } });
                        await srv1.SetAppMgrConfig("Album", pc1.Id, strcfig).ConfigureAwait(false);

                        Assert.AreEqual(pc1.Apps?.Count, 1);

                        var ret = srv1.SharePersonalCloud(pc1);
                        Thread.Sleep(3000);
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        Thread.Sleep(1000);

                        Assert.AreEqual(pc2.Apps?.Count, 1);
                        foreach (var item in pc2.Apps)
                        {
                            var url = pc2.GetWebAppUri(item);
                            if (string.IsNullOrWhiteSpace(url?.AbsoluteUri))
                            {
                                Assert.Fail();
                            }
                        }
                    }
                }
            }
        }


        [Test]
        public async Task SimpleAppinFS()
        {

            var my = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var dic = Path.Combine(my, "TestConsoleApp", "webapps");

            var t1 = new SimpleConfigStorage(
                Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "TestConsoleApp", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)));
            Directory.CreateDirectory(dic);


            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(t1, loggerFactory, Getfs(t1.RootPath), dic))
                {
                    srv1.InstallApps().Wait();

                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2, loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv1.StartService();
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        var pc1 = srv1.CreatePersonalCloud("test", "test1");


                        var strcfig = JsonConvert.SerializeObject(new List<AlbumConfig>() {
                            new AlbumConfig {
                                MediaFolder= @"F:\pics",
                                Name="test",
                                ThumbnailFolder=@"D:\Projects\out"
                            } });
                        await srv1.SetAppMgrConfig("Album", pc1.Id, strcfig).ConfigureAwait(false);


                        Assert.AreEqual(pc1.Apps?.Count, 1);

                        var ret = srv1.SharePersonalCloud(pc1);
                        Thread.Sleep(3000);
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        Thread.Sleep(1000);

                        Assert.AreEqual(pc2.Apps?.Count, 1);
                        foreach (var item in pc2.Apps)
                        {
                            var url = pc2.GetWebAppUri(item);
                            if (string.IsNullOrWhiteSpace(url?.AbsoluteUri))
                            {
                                Assert.Fail();
                            }
                        }

                        var appinfs = new NSPersonalCloud.FileSharing.AppInFs(l);
                        appinfs.GetApps = () => pc1.Apps;
                        appinfs.GetUrl = (x) => pc1.GetWebAppUri(x).ToString();
                        var ls = await appinfs.EnumerateChildrenAsync("/").ConfigureAwait(false);
                        Assert.AreEqual(ls.Count, 1);
                    }
                }
            }
        }

        [Test]
        public void SimpleShare2()
        {

            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var ran = new Random();
                int nport1 = ran.Next(1000, 10000);
                int nport2 = ran.Next(1000, 10000);

                l.LogInformation($"port 1 is {nport1}  port 2 is {nport2}");
                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(inf1,
                 loggerFactory, Getfs(inf1.GetConfigFolder()), null))
                {
                    srv1.TestSetUdpPort(nport1, new[] { nport2, nport1 });
                    srv1.StartService();
                    var pc1 = srv1.CreatePersonalCloud("test", "test1");
                    var ret = srv1.SharePersonalCloud(pc1);

                    Thread.Sleep(1000);
                    
                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2,
                    loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv2.TestSetUdpPort(nport2, new[] { nport2, nport1 });
                        l.LogInformation($"before srv2.StartService(),port {srv2.ServerPort}");
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        Thread.Sleep(2000);
                        l.LogInformation("before srv2.JoinPersonalCloud();");
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        Thread.Sleep(2000);

                        SimpleShareCheckContent(pc2, 2, 2);
                        SimpleShareCheckContent(pc1, 2, 2);
                    }
                }
            }
        }

        [Test]
        public void CreateMultiple()
        {
            int count = 50;

            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var inf = new HostPlatformInfo[count];
                var srv = new PCLocalService[count];
                var ports = new int[count];
                var pcs = new PersonalCloud[count];
                for (int i = 0; i < count; i++)
                {
                    inf[i] = new HostPlatformInfo();
                    srv[i] = new PCLocalService(inf[i], loggerFactory, Getfs(inf[i].GetConfigFolder()), null);
                    ports[i] = 2000 + i;
                }

                Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 3 },
                    i => {
                        srv[i].TestSetUdpPort(ports[i], ports);
                        srv[i].StartService();
                        l.LogInformation($"StartService {i}");
                    });
            }
        }

        [Test]
        public void ShareToMultiple()
        {
            int count = 20;

            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var inf = new HostPlatformInfo[count];
                var srv = new PCLocalService[count];
                var ports = new int[count];
                var pcs = new PersonalCloud[count];
                for (int i = 0; i < count; i++)
                {
                    inf[i] = new HostPlatformInfo();
                    srv[i] = new PCLocalService(inf[i], loggerFactory, Getfs(inf[i].GetConfigFolder()), null);
                    ports[i] = 2000 + i;
                }

                Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 3 },
                    i => {
                        srv[i].TestSetUdpPort(ports[i], ports);
                        Thread.Sleep(500);
                        srv[i].StartService();
                        l.LogInformation($"guid {srv[i].NodeId} is test{i}");
                    });

                pcs[0] = srv[0].CreatePersonalCloud("test", "test0");
                var ret = srv[0].SharePersonalCloud(pcs[0]);
                l.LogInformation("srv0 is sharing");
                Thread.Sleep(2000* count/2);

                var fret = Parallel.For(1, count, new ParallelOptions { MaxDegreeOfParallelism = 2 },
                    i => {
                        pcs[i] = srv[i].JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), $"test{i}").Result;
                    });
                while (!fret.IsCompleted)
                {
                    Thread.Sleep(500);
                }
                Thread.Sleep(5000 * count / 10);
                l.LogInformation("Exam the result");

                for (int i = 0; i < count; i++)
                {
                    SimpleShareCheckContent(pcs[i], 2, count);
                }

            }
        }

        static private void SimpleShareCheckContent(PersonalCloud pc, int expectedCount, int nodes)
        {
            var fs2 = pc.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
            Assert.AreEqual(nodes, fs2.Count);
            foreach (var item in fs2)
            {
                var f = pc.RootFS.EnumerateChildrenAsync($"/{item.Name}").AsTask().Result;
                Assert.AreEqual(expectedCount, f.Count);
            }
        }


        [Test]
        public void TestRepublish()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt")))
            {
                var inf = new HostPlatformInfo();
                using (var srv = new PCLocalService(inf,
                    loggerFactory, Getfs(inf.GetConfigFolder()), null))
                {
                    srv.StartService();
                    var pc = srv.CreatePersonalCloud("test", "testfolder");
                    Thread.Sleep(1000);
                    for (int i = 0; i < 100; i++)
                    {
                        var lis = pc.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                        Assert.AreEqual(1, lis.Count);
                        srv.NetworkMayChanged(false);
                        Thread.Sleep(100);
                        srv.NetworkMayChanged(true);
                        Thread.Sleep(200);
                    }
                }
            }
        }



#if DEBUG
        [Test]
        public void TestRepublishwithoutStop()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt")))
            {
                var inf = new HostPlatformInfo();
                using (var srv = new PCLocalService(inf,
                    loggerFactory, Getfs(inf.GetConfigFolder()), null))
                {
                    srv.StartService();
                    var pc = srv.CreatePersonalCloud("test", "testfolder");
                    Thread.Sleep(1000);
                    for (int i = 0; i < 10; i++)
                    {
                        var lis = pc.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                        Assert.AreEqual(1, lis.Count);
                        srv.NetworkMayChanged(false);
                        Thread.Sleep(200);
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        var lis = pc.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                        Assert.AreEqual(1, lis.Count);
                        srv.TestStopWebServer();
                        srv.NetworkMayChanged(false);
                        Thread.Sleep(200);
                    }
                }
            }
        }
#endif//DEBUG

//         [Test]
//         public void TestStopNetwork()
//         {
//             using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
//             {
//                 var l = loggerFactory.CreateLogger<LocalServiceTest>();
//                 var t = DateTime.Now;
// 
//                 var ran = new Random();
//                 int nport1 = ran.Next(1000, 10000);
//                 int nport2 = ran.Next(1000, 10000);
// 
//                 l.LogInformation($"port 1 is {nport1}  port 2 is {nport2}");
//                 var inf1 = new HostPlatformInfo();
//                 using (var srv1 = new PCLocalService(inf1,
//                  loggerFactory, Getfs(inf1.GetConfigFolder()), null))
//                 {
//                     srv1.TestSetUdpPort(nport1, new[] { nport2, nport1 });
//                     srv1.StartService();
//                     var pc1 = srv1.CreatePersonalCloud("test", "test1");
//                     var ret = srv1.SharePersonalCloud(pc1);
// 
//                     Thread.Sleep(1000);
// 
//                     var inf2 = new HostPlatformInfo();
//                     using (var srv2 = new PCLocalService(inf2,
//                     loggerFactory, Getfs(inf2.GetConfigFolder()), null))
//                     {
//                         srv2.TestSetUdpPort(nport2, new[] { nport2, nport1 });
//                         l.LogInformation($"before srv2.StartService(),port {srv2.ServerPort}");
//                         srv2.StartService();
// 
//                         //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
//                         Thread.Sleep(1000);
//                         l.LogInformation("before srv2.JoinPersonalCloud();");
//                         var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
//                         Thread.Sleep(1000);
// 
//                         SimpleShareCheckContent(pc2, 2, 2);
//                         SimpleShareCheckContent(pc1, 2, 2);
// 
//                         srv2.StopNetwork();
//                         SimpleShareCheckContent(pc2, 0, 0);
//                         srv2.StartNetwork(true);
//                         Thread.Sleep(3000);
//                         SimpleShareCheckContent(pc2, 2, 2);
//                     }
//                 }
//             }
//         }

#if DEBUG
        [Test]
        public void TestExpiredNodes()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var ran = new Random();
                int nport1 = ran.Next(1000, 10000);
                int nport2 = ran.Next(1000, 10000);

                l.LogInformation($"port 1 is {nport1}  port 2 is {nport2}");
                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(inf1,
                 loggerFactory, Getfs(inf1.GetConfigFolder()), null))
                {
                    srv1.TestSetReannounceTime(1 * 1000);
                    srv1.TestSetUdpPort(nport1, new[] { nport2, nport1 });
                    srv1.StartService();
                    var pc1 = srv1.CreatePersonalCloud("test", "test1");
                    var ret = srv1.SharePersonalCloud(pc1);

                    Thread.Sleep(1000);

                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2,
                    loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv2.TestSetReannounceTime(1 * 1000);
                        srv2.TestSetUdpPort(nport2, new[] { nport2, nport1 });
                        l.LogInformation($"before srv2.StartService(),port {srv2.ServerPort}");
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        Thread.Sleep(1000);
                        l.LogInformation("before srv2.JoinPersonalCloud();");
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        srv1.StopSharePersonalCloud(pc1);
                        Thread.Sleep(2000);

                        SimpleShareCheckContent(pc2, 2, 2);
                        SimpleShareCheckContent(pc1, 2, 2);

                        srv2.Dispose();
                        l.LogInformation($"srv2 disposed");

                        Thread.Sleep(20000);
                        _= pc1.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                        Thread.Sleep(3000);
                        SimpleShareCheckContent(pc1, 2, 1);
                    }
                }
            }
        }

        [Test]
        public void TestRepubNodes()
        {
            using (var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddFile("Logs/{Date}.txt", LogLevel.Trace)))
            {
                var l = loggerFactory.CreateLogger<LocalServiceTest>();
                var t = DateTime.Now;

                var ran = new Random();
                int nport1 = ran.Next(1000, 10000);
                int nport2 = ran.Next(1000, 10000);

                l.LogInformation($"port 1 is {nport1}  port 2 is {nport2}");
                var inf1 = new HostPlatformInfo();
                using (var srv1 = new PCLocalService(inf1,
                 loggerFactory, Getfs(inf1.GetConfigFolder()), null))
                {
                    srv1.TestSetReannounceTime(1000);
                    srv1.TestSetUdpPort(nport1, new[] { nport2, nport1 });
                    srv1.StartService();
                    var pc1 = srv1.CreatePersonalCloud("test", "test1");
                    var ret = srv1.SharePersonalCloud(pc1);

                    Thread.Sleep(1000);

                    var inf2 = new HostPlatformInfo();
                    using (var srv2 = new PCLocalService(inf2,
                    loggerFactory, Getfs(inf2.GetConfigFolder()), null))
                    {
                        srv2.TestSetReannounceTime(3 * 1000);
                        srv2.TestSetUdpPort(nport2, new[] { nport2, nport1 });
                        l.LogInformation($"before srv2.StartService(),port {srv2.ServerPort}");
                        srv2.StartService();

                        //l.LogInformation((DateTime.Now - t).TotalSeconds.ToString());
                        Thread.Sleep(1000);
                        l.LogInformation("before srv2.JoinPersonalCloud();");
                        var pc2 = srv2.JoinPersonalCloud(int.Parse(ret, CultureInfo.InvariantCulture), "test2").Result;
                        Thread.Sleep(1000);

                        SimpleShareCheckContent(pc2, 2, 2);
                        SimpleShareCheckContent(pc1, 2, 2);

                        Thread.Sleep(10000);
                        
                        SimpleShareCheckContent(pc2, 2, 2);
                        SimpleShareCheckContent(pc1, 2, 2);

                        SimpleShareCheckContent(pc2, 2, 2);
                        SimpleShareCheckContent(pc1, 2, 2);

                        srv2.Dispose();
                        Thread.Sleep(20000);
                        _ = pc1.RootFS.EnumerateChildrenAsync("/").AsTask().Result;
                        Thread.Sleep(1000);
                        SimpleShareCheckContent(pc1, 2, 1);
                    }
                }
            }
        }
#endif//DEBUG
    }
#pragma warning restore CA1303 // Do not pass literals as localized parameters
}
