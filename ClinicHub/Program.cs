using ClinicHub.Services;
using ClinicHub.Services.Options;
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

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
