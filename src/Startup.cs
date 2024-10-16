using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Serilog;

using NemLoginSigningService.Services;
using NemLoginSignatureValidationService.Service;
using NemLoginSigningCore.Logging;
using NemLoginSigningCore.Configuration;

using NemLoginSigningWebApp.Config;
using NemLoginSigningWebApp.Logic;
using NemLoginSigningWebApp.Utils;

namespace NemLoginSigningWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // Implementation needs a httpcontext accessor, Add method does a try add.
            services.AddHttpContextAccessor();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            services.AddScoped<ICorrelationIdAccessor, CorrelationIdAccessor>();

            // Configuration dependencies
            var configurationSection = Configuration.GetSection("NemloginConfiguration");
            services.Configure<NemloginConfiguration>(configurationSection);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            services.AddTransient<ISignersDocumentLoader, SignersDocumentLoader>();
            services.AddTransient<ISigningPayloadService, SigningPayloadService>();
            services.AddTransient<ITransformationPropertiesService, TransformationPropertiesService>();
            services.AddTransient<IDocumentSigningService, DocumentSigningService>();
            services.AddTransient<ISigningValidationService, SigningValidationService>();

            var nemloginConfiguration = configurationSection.Get<NemloginConfiguration>();

            services.AddHttpClient("ValidationServiceClient", c => c.BaseAddress = new System.Uri(nemloginConfiguration.ValidationServiceURL));
            services.AddTransient<ISigningValidationService, SigningValidationService>();

            var cors = Configuration.GetSection("CORS").Get<CorsConfig>();

            if (cors?.AllowedOrigins is not null && cors.AllowedOrigins.Any())
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(
                        policy =>
                        {
                            policy.WithOrigins(cors.AllowedOrigins);
                        });
                });
            }

            LoadAssemblies();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSerilogRequestLogging();

            loggerFactory.AddSerilog();

            LoggerCreator.LoggerFactory = loggerFactory;

            app.UseRouting();

            app.UseCors();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void LoadAssemblies()
        {
            // Load assembly to be able to do reflection later on
            Assembly.Load("NemLoginSigningXades"); // , Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
            Assembly.Load("NemLoginSigningPades"); // , Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
        }
    }
}