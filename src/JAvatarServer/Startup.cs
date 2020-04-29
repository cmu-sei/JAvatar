// Copyright 2020 Carnegie Mellon University.
// Released under a MIT (SEI) license. See LICENSE.md in the project root.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace JAvatarServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostEnvironment env)
        {
            Configuration = configuration;

            _options = configuration.Get<Options>();

            _about = String.Format("<!doctype html><html><head><title>{0}</title></head><body><h1>{0}</h1><code>{1}</code><a href=\"javatar\">Try JAvatar</a></body></html>",
                Assembly.GetEntryAssembly().GetName().Name,
                Environment.GetEnvironmentVariable("COMMIT")
            );

            if (env.IsDevelopment())
            {
                _options.OpenIdConnect.RequireHttpsMetadata = false;
            }
        }

        public IConfiguration Configuration { get; }
        public Options _options { get; }
        private string _about;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddJAvatar(() => _options.JAvatar);

            services.ConfigureForwarding(_options.Header.Forwarding);

            services.AddCors(
                opt => opt.AddPolicy(
                    _options.Header.Cors.Name,
                    _options.Header.Cors.Build()
                )
            );

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = _options.OpenIdConnect.Authority;
                    options.Audience = _options.OpenIdConnect.Audience;
                    options.RequireHttpsMetadata = _options.OpenIdConnect.RequireHttpsMetadata;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (!string.IsNullOrEmpty(_options.Header.Forwarding.TargetHeaders))
                app.UseForwardedHeaders();

            app.UseRouting();

            app.UseCors(_options.Header.Cors.Name);

            app.UseAuthentication();

            app.UseEndpoints(opt => {
                opt.MapJAvatar(_options.JAvatar.RoutePrefix);
                opt.Map("", async (context) =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(_about);
                });
            });

        }
    }
}
