// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Security.Principal;
using System.Text.Json.Serialization;
using AutoMapper.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Steamfitter.Api.Data;
using Steamfitter.Api.Infrastructure.Authorization;
using Steamfitter.Api.Infrastructure.Extensions;
using Crucible.Common.EntityEvents.Extensions;
using Steamfitter.Api.Infrastructure.Filters;
using Steamfitter.Api.Infrastructure.HealthChecks;
using Steamfitter.Api.Infrastructure.Identity;
using Steamfitter.Api.Infrastructure.JsonConverters;
using Steamfitter.Api.Infrastructure.Mapping;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Services;
using Steamfitter.Api.ViewModels;
using Crucible.Common.ServiceDefaults;

namespace Steamfitter.Api;

public class Startup
{
    public Infrastructure.Options.AuthorizationOptions _authOptions = new();
    public Infrastructure.Options.VmTaskProcessingOptions _vmTaskProcessingOptions = new();
    private readonly SignalROptions _signalROptions = new();
    private const string _routePrefix = "api";
    private IConfiguration Configuration { get; }
    private string _pathbase;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _env = env;
        Configuration = configuration;
        Configuration.GetSection("Authorization").Bind(_authOptions);
        Configuration.GetSection("VmTaskProcessing").Bind(_vmTaskProcessingOptions);
        Configuration.GetSection("SignalR").Bind(_signalROptions);
        _pathbase = Configuration["PathBase"];
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Add Azure Application Insights, if connection string is supplied
        string appInsights = Configuration["ApplicationInsights:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(appInsights))
        {
            services.AddApplicationInsightsTelemetry();
        }

        services.AddSingleton<TaskMaintenanceServiceHealthCheck>();
        services.AddSingleton<StartupHealthCheck>();
        services.AddHealthChecks()
            .AddCheck<TaskMaintenanceServiceHealthCheck>(
                "task_service_responsive",
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
                services.AddEventPublishingDbContextFactory<SteamfitterContext>((sp, builder) =>
                    builder.UseInMemoryDatabase("api"));
                break;
            case "Sqlite":
            case "SqlServer":
            case "PostgreSQL":
                services.AddEventPublishingDbContextFactory<SteamfitterContext>((sp, builder) =>
                    builder.UseConfiguredDatabase(Configuration));
                break;
        }

        var connectionString = Configuration.GetConnectionString(DatabaseExtensions.DbProvider(Configuration));
        switch (provider)
        {
            case "Sqlite":
                services.AddHealthChecks().AddSqlite(connectionString, tags: new[] { "ready", "live" });
                break;
            case "SqlServer":
                services.AddHealthChecks().AddSqlServer(connectionString, tags: new[] { "ready", "live" });
                break;
            case "PostgreSQL":
                services.AddHealthChecks().AddNpgSql(connectionString, tags: new[] { "ready", "live" });
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

        services.AddSignalR(o => o.StatefulReconnectBufferSize = _signalROptions.StatefulReconnectBufferSizeBytes)
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
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddSwagger(_authOptions);
        services.AddPlayerApiClient();
        services.AddPlayerVmApiClient();

        JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = _authOptions.Authority;
            options.RequireHttpsMetadata = _authOptions.RequireHttpsMetadata;
            options.SaveToken = true;

            string[] validAudiences;

            if (_authOptions.ValidAudiences != null && _authOptions.ValidAudiences.Any())
            {
                validAudiences = _authOptions.ValidAudiences;
            }
            else
            {
                validAudiences = _authOptions.AuthorizationScope.Split(' ');
            }

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateAudience = _authOptions.ValidateAudience,
                ValidAudiences = validAudiences
            };
        });

        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });

        services.AddMemoryCache();

        services.AddScoped<IScenarioService, ScenarioService>();
        services.AddScoped<IScenarioMembershipService, ScenarioMembershipService>();
        services.AddScoped<IScenarioRoleService, ScenarioRoleService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IResultService, ResultService>();
        services.AddScoped<IScenarioTemplateService, ScenarioTemplateService>();
        services.AddScoped<IScenarioTemplateMembershipService, ScenarioTemplateMembershipService>();
        services.AddScoped<IScenarioTemplateRoleService, ScenarioTemplateRoleService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IVmCredentialService, VmCredentialService>();
        services.AddScoped<IPrincipal>(p => p.GetService<IHttpContextAccessor>()?.HttpContext?.User);
        services.AddScoped<IScoringService, ScoringService>();
        services.AddScoped<ISteamfitterAuthorizationService, AuthorizationService>();
        services.AddScoped<IIdentityResolver, IdentityResolver>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<ISystemRoleService, SystemRoleService>();
        services.AddSingleton<StackStormService>();
        services.AddSingleton<IHostedService>(x => x.GetService<StackStormService>());
        services.AddSingleton<IStackStormService>(x => x.GetService<StackStormService>());
        services.AddSingleton<ITaskExecutionQueue, TaskExecutionQueue>();
        services.AddHostedService<TaskExecutionService>();
        services.AddHostedService<TaskMaintenanceService>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddHttpClient();

        ApplyPolicies(services);

        services.AddAutoMapper(cfg =>
        {
            cfg.Internal().ForAllPropertyMaps(
                pm => pm.SourceType != null && Nullable.GetUnderlyingType(pm.SourceType) == pm.DestinationType,
                (pm, c) => c.MapFrom<object, object, object, object>(new IgnoreNullSourceValues(), pm.SourceMember.Name));
        }, typeof(Startup));

        services.Configure<VmTaskProcessingOptions>(Configuration.GetSection("VmTaskProcessing"));
        services
            .Configure<ResourceOwnerAuthorizationOptions>(Configuration.GetSection("ResourceOwnerAuthorization"))
            .AddScoped(config => config.GetService<IOptionsMonitor<ResourceOwnerAuthorizationOptions>>().CurrentValue);

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(Startup).Assembly));

        // add Crucible Common Service Defaults with configuration from appsettings
        services.AddServiceDefaults(_env, Configuration, openTelemetryOptions =>
        {
            // Bind configuration from appsettings.json "OpenTelemetry" section
            var telemetrySection = Configuration.GetSection("OpenTelemetry");
            if (telemetrySection.Exists())
            {
                telemetrySection.Bind(openTelemetryOptions);
            }
        });
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
                    context.Request.Headers.Append("Authorization", new[] { $"Bearer {token}" });
            }

            await next.Invoke();

        });

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = _routePrefix;
            c.SwaggerEndpoint($"{_pathbase}/swagger/v1/swagger.json", "Steamfitter v1");
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
                endpoints.MapHub<Hubs.EngineHub>("/hubs/engine", options =>
                    {
                        options.AllowStatefulReconnects = _signalROptions.EnableStatefulReconnect;
                    }
                );
            });
    }


    private void ApplyPolicies(IServiceCollection services)
    {
        services.AddAuthorizationPolicy(_authOptions);
    }
}
