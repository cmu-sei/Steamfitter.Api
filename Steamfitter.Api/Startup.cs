// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steamfitter.Api.Infrastructure.Extensions;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.JsonConverters;
using Steamfitter.Api.Infrastructure.Mapping;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using System;
using AutoMapper;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Filters;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Principal;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steamfitter.Api.Infrastructure.HealthChecks;

namespace Steamfitter.Api
{
    public class Startup
    {
        public Infrastructure.Options.AuthorizationOptions _authOptions = new Infrastructure.Options.AuthorizationOptions();
        public Infrastructure.Options.VmTaskProcessingOptions _vmTaskProcessingOptions = new Infrastructure.Options.VmTaskProcessingOptions();
        private const string _routePrefix = "api";
        public IConfiguration Configuration { get; }
        private string _pathbase;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Configuration.GetSection("Authorization").Bind(_authOptions);
            Configuration.GetSection("VmTaskProcessing").Bind(_vmTaskProcessingOptions);
            _pathbase = Configuration["PathBase"];
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<TaskMaintenanceServiceHealthCheck>();
            services.AddSingleton<StackStormServiceHealthCheck>();
            services.AddSingleton<StartupHealthCheck>();
            services.AddHealthChecks()
                .AddCheck<TaskMaintenanceServiceHealthCheck>(
                    "task_service_responsive", 
                    failureStatus: HealthStatus.Unhealthy, 
                    tags: new[] { "live" })
                .AddCheck<StackStormServiceHealthCheck>(
                    "stackstorm_service_responsive", 
                    failureStatus: HealthStatus.Unhealthy, 
                    tags: new[] { "live" })
                .AddCheck<StartupHealthCheck>(
                    "startup", 
                    failureStatus: HealthStatus.Degraded, 
                    tags: new[] { "ready" });

            var provider = Configuration["Database:Provider"];
            switch (provider)
            {
                case "InMemory":
                    services.AddDbContextPool<SteamfitterContext>(opt => opt.UseInMemoryDatabase("api"));
                    break;
                case "Sqlite":
                case "SqlServer":
                case "PostgreSQL":
                    services.AddDbContextPool<SteamfitterContext>(builder => builder.UseConfiguredDatabase(Configuration));
                    break;
            }

            var connectionString = Configuration.GetConnectionString(DatabaseExtensions.DbProvider(Configuration));
            switch (provider)
            {
                case "Sqlite":
                    services.AddHealthChecks().AddSqlite(connectionString, tags: new[] { "ready", "live"});
                    break;
                case "SqlServer":
                    services.AddHealthChecks().AddSqlServer(connectionString, tags: new[] { "ready", "live"});
                    break;
                case "PostgreSQL":
                    services.AddHealthChecks().AddNpgSql(connectionString, tags: new[] { "ready", "live"});
                    break;
            }

            services.AddOptions()
                .Configure<DatabaseOptions>(Configuration.GetSection("Database"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<DatabaseOptions>>().CurrentValue)

                .Configure<ClaimsTransformationOptions>(Configuration.GetSection("ClaimsTransformation"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<ClaimsTransformationOptions>>().CurrentValue)

                .Configure<SeedDataOptions>(Configuration.GetSection("SeedData"))
                    .AddScoped(config => config.GetService<IOptionsMonitor<SeedDataOptions>>().CurrentValue);

            services
                .Configure<ClientOptions>(Configuration.GetSection("ClientSettings"))
                .AddScoped(config => config.GetService<IOptionsMonitor<ClientOptions>>().CurrentValue);

            services
                .Configure<FilesOptions>(Configuration.GetSection("Files"))
                .AddScoped(config => config.GetService<IOptionsMonitor<FilesOptions>>().CurrentValue);

            services.AddScoped<IPlayerVmService, PlayerVmService>();
            services.AddScoped<IPlayerService, PlayerService>();
            services.AddScoped<IClaimsTransformation, AuthorizationClaimsTransformer>();
            services.AddScoped<IUserClaimsService, UserClaimsService>();

            services.AddCors(options => options.UseConfiguredCors(Configuration.GetSection("CorsPolicy")));

            services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(typeof(ValidateModelStateFilter));
                options.Filters.Add(typeof(JsonExceptionFilter));

                // Require all scopes in authOptions
                var policyBuilder = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
                Array.ForEach(_authOptions.AuthorizationScope.Split(' '), x => policyBuilder.RequireScope(x));

                var policy = policyBuilder.Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonNullableGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonIntegerConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services.AddSwagger(_authOptions);
            services.AddPlayerApiClient();
            services.AddPlayerVmApiClient();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = _authOptions.Authority;
                options.RequireHttpsMetadata = _authOptions.RequireHttpsMetadata;
                options.SaveToken = true;
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidAudiences = _authOptions.AuthorizationScope.Split(' ')
                };
            });

            services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });

            services.AddMemoryCache();

            services.AddScoped<IScenarioService, ScenarioService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IResultService, ResultService>();
            services.AddScoped<IScenarioTemplateService, ScenarioTemplateService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddScoped<IFilesService, FilesService>();
            services.AddScoped<IBondAgentService, BondAgentService>();
            services.AddScoped<IVmCredentialService, VmCredentialService>();
            services.AddSingleton<StackStormService>();
            services.AddSingleton<IHostedService>(x => x.GetService<StackStormService>());
            services.AddSingleton<IStackStormService>(x => x.GetService<StackStormService>());
            services.AddSingleton<BondAgentStore>();
            services.AddSingleton<ITaskExecutionQueue, TaskExecutionQueue>();
            services.AddHostedService<TaskExecutionService>();
            services.AddHostedService<TaskMaintenanceService>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IPrincipal>(p => p.GetService<IHttpContextAccessor>().HttpContext.User);
            services.AddHttpClient();

            ApplyPolicies(services);

            services.AddAutoMapper(cfg =>
            {
                cfg.ForAllPropertyMaps(
                    pm => pm.SourceType != null && Nullable.GetUnderlyingType(pm.SourceType) == pm.DestinationType,
                    (pm, c) => c.MapFrom<object, object, object, object>(new IgnoreNullSourceValues(), pm.SourceMember.Name));
            }, typeof(Startup));

            services.Configure<VmTaskProcessingOptions>(Configuration.GetSection("VmTaskProcessing"));
            services
                .Configure<ResourceOwnerAuthorizationOptions>(Configuration.GetSection("ResourceOwnerAuthorization"))
                .AddScoped(config => config.GetService<IOptionsMonitor<ResourceOwnerAuthorizationOptions>>().CurrentValue);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UsePathBase(_pathbase);

            app.UseRouting();
            app.UseCors("default");

            //move any querystring jwt to Auth bearer header
            app.Use(async (context, next) =>
            {
                if (string.IsNullOrWhiteSpace(context.Request.Headers["Authorization"])
                    && context.Request.QueryString.HasValue)
                {
                    string token = context.Request.QueryString.Value
                        .Substring(1)
                        .Split('&')
                        .SingleOrDefault(x => x.StartsWith("bearer="))?.Split('=')[1];

                    if (!String.IsNullOrWhiteSpace(token))
                        context.Request.Headers.Add("Authorization", new[] { $"Bearer {token}" });
                }

                await next.Invoke();

            });

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = _routePrefix;
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Steamfitter v1");
                c.OAuthClientId(_authOptions.ClientId);
                c.OAuthClientSecret(_authOptions.ClientSecret);
                c.OAuthAppName(_authOptions.ClientName);
                c.OAuthUsePkce();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks($"/{_routePrefix}/health/ready", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains("ready"),
                    });

                    endpoints.MapHealthChecks($"/{_routePrefix}/health/live", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains("live"),
                    });
                    endpoints.MapHub<Hubs.EngineHub>("/hubs/engine");
                }
            );
        }


        private void ApplyPolicies(IServiceCollection services)
        {
            services.AddAuthorizationPolicy();
        }
    }
}
