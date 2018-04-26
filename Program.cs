using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using ContactManager.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace ContactManager
{
    public class Program
    {
        private static IConfiguration Config;

        public static void Main(string[] args)
        {
            IWebHost host = BuildWebHost(args);

            using (IServiceScope scope = host.Services.CreateScope())
            {
                IServiceProvider services = scope.ServiceProvider;
                // requires using Microsoft.Extensions.Configuration;
                Config = services.GetRequiredService<IConfiguration>();
                MigradeDatabase(services);
            }

            host.Run();
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(factory =>
                {
                    factory.AddConsole();
                    factory.AddDebug();
                    factory.AddFilter("Console", level => level >= LogLevel.Information);
                    factory.AddFilter("Debug", level => level >= LogLevel.Information);
                })
                .UseKestrel(options =>
                {
                    int port = Config.GetValue<int>("Ssl:Port");
                    options.AddServerHeader = false;
                    options.Listen(IPAddress.Loopback, port, listenOptions =>
                    {
                        // Configure SSL
                        X509Certificate2 serverCert = LoadCertificate();
                        listenOptions.UseHttps(serverCert);
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
        }

        private static void MigradeDatabase(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Initiate database script migration
            dbContext.Database.Migrate();

            // Set password with the Secret Manager tool.
            // dotnet user-secrets set SeedUserPW <pw>
            var testUserPw = Config["Database:SeedUserPW"];

            try
            {
                SeedData.Initialize(services, testUserPw).Wait();
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }

        private static X509Certificate2 LoadCertificate()
        {
            Assembly assembly = typeof(Startup).GetTypeInfo().Assembly;
            EmbeddedFileProvider embeddedFileProvider = new EmbeddedFileProvider(assembly, "final");
            IFileInfo certificateFileInfo = embeddedFileProvider.GetFileInfo(Config["Ssl:Certificate:FileName"]);

            using (Stream certificateStream = certificateFileInfo.CreateReadStream())
            {
                byte[] certificatePayload;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    certificateStream.CopyTo(memoryStream);
                    certificatePayload = memoryStream.ToArray();
                }

                return new X509Certificate2(certificatePayload, Config["Ssl:Certificate:Password"]);
            }

            //string certFileName = Config["Ssl:Certificate:FileName"];
            //string certPassword = Config["Ssl:Certificate:Password"];

            //return new X509Certificate2(certFileName, certPassword);
        }
    }
}
