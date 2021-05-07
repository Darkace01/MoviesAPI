using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using MoviesAPI.Filters;
using MoviesAPI.Services;

namespace MoviesAPI
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

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(MyExceptionFilter));
            });

            //Response Caching Filter
            services.AddResponseCaching();

            //Authentication
            // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

            //Dependency Injection
            services.AddSingleton<IRepository, InMemoryRepository>();
            services.AddTransient<MyActionFilter>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MoviesAPI", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> _logger)
        {
            // app.Use(async (context, next) =>
            // {
            //     using (var swapStram = new MemoryStream())
            //     {
            //         var originalResponseBody = context.Response.Body;
            //         context.Response.Body = swapStram;

            //         await next.Invoke();

            //         swapStram.Seek(0, SeekOrigin.Begin);
            //         string responseBody = new StreamReader(swapStram).ReadToEnd();
            //         swapStram.Seek(0, SeekOrigin.Begin);

            //         await swapStram.CopyToAsync(originalResponseBody);
            //         context.Response.Body = originalResponseBody;

            //         _logger.LogInformation(responseBody);
            //     }
            // });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MoviesAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseResponseCaching();

            // app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
