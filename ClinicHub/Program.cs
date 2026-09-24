using ClinicHub.Services;
using ClinicHub.Services.Options;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;
using Serilog;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ClinicHub
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Root fix for shared-host port conflicts: when IIS/ANCM launches the
            // app it assigns a random port (ASPNETCORE_PORT) and that always wins.
            // Only when NO port comes from the host (direct `dotnet *.dll` launch)
            // fall back to this site's own loopback port, so the dashboard (:5000)
            // and the API (:5001) can never collide with each other.
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_PORT"))
                && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
            {
                builder.WebHost.UseUrls("http://127.0.0.1:5000");
            }

            var env = builder.Environment;


            builder.Configuration.Sources.Clear();
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

            if (env.IsDevelopment() || env.EnvironmentName == "Test")
            {
                var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                if (appAssembly != null) builder.Configuration.AddUserSecrets(appAssembly, optional: true);
            }

            builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateBootstrapLogger();

            Log.Information("ClinicHub API is starting up and connecting to Seq at {Time}", DateTime.Now);

            builder.Host.UseSerilog();

            // Persist Data Protection keys to disk so auth cookies survive
            // restarts and redeploys. Without this the key ring is ephemeral
            // and every restart logs all users out.
            var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "keys");
            Directory.CreateDirectory(keysDirectory);
            builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
                .SetApplicationName("ClinicHub");

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(
                        new JsonStringEnumConverter());
                });

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);
            builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = System.IO.Compression.CompressionLevel.Fastest);

            builder.Services.AddOptions();

            builder.Services.AddHttpClient();

            builder.Services.Configure<Doctory>(builder.Configuration.GetSection("Doctory"));
            builder.Services.Configure<GoogleMapsOptions>(builder.Configuration.GetSection("GoogleMaps"));
            builder.Services.Configure<FirebaseWebOptions>(builder.Configuration.GetSection("FirebaseWeb"));
            builder.Services.AddServices();

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            // Compress responses (Brotli/Gzip) — must run before static files
            // and endpoints so CSS, JS, and JSON all ship compressed.
            app.UseResponseCompression();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Static assets with a version fingerprint (?v=...) never change —
            // cache them forever. Everything else gets a week.
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] =
                        ctx.Context.Request.Query.ContainsKey("v")
                            ? "public, max-age=31536000, immutable"
                            : "public, max-age=604800";
                }
            });

            app.UseRouting();

            app.UseAuthorization();

            app.MapReverseProxy();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
