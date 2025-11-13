// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Steamfitter.Api.Infrastructure.Options;
using Steamfitter.Api.Infrastructure.OperationFilters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Player.Vm.Api;
using System.Net.Http;
using Microsoft.OpenApi;
using Player.Api.Client;

namespace Steamfitter.Api.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddSwagger(this IServiceCollection services, AuthorizationOptions authOptions)
        {
            // XML Comments path
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string commentsFileName = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
            string commentsFile = Path.Combine(baseDirectory, commentsFileName);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Steamfitter API", Version = "v1" });

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(authOptions.AuthorizationUrl),
                            TokenUrl = new Uri(authOptions.TokenUrl),
                            Scopes = new Dictionary<string, string>()
                            {
                                {authOptions.AuthorizationScope, "public api access"}
                            }
                        }
                    }
                });

                c.AddSecurityRequirement((document) => new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference("oauth2", document), [authOptions.AuthorizationScope] }
                });

                c.IncludeXmlComments(commentsFile);
                c.EnableAnnotations();
                c.OperationFilter<DefaultResponseOperationFilter>();
                c.MapType<Optional<Guid?>>(() => new OpenApiSchema
                {
                    OneOf = new List<IOpenApiSchema>
                    {
                        new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" },
                        new OpenApiSchema { Type = JsonSchemaType.Null }
                    }
                });

                c.MapType<JsonElement?>(() => new OpenApiSchema
                {
                    OneOf = new List<IOpenApiSchema>
                    {
                        new OpenApiSchema { Type = JsonSchemaType.Object },
                        new OpenApiSchema { Type = JsonSchemaType.Null }
                    }
                });
            });
        }

        public static void AddPlayerApiClient(this IServiceCollection services)
        {
            services.AddScoped<IPlayerApiClient, PlayerApiClient>(p =>
            {
                var httpContextAccessor = p.GetRequiredService<IHttpContextAccessor>();
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                var clientOptions = p.GetRequiredService<ClientOptions>();

                var playerUri = new Uri(clientOptions.urls.playerApi);

                string authHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"];

                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = playerUri;
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

                var apiClient = new PlayerApiClient(httpClient);
                return apiClient;
            });
        }

        public static void AddPlayerVmApiClient(this IServiceCollection services)
        {
            services.AddScoped<IPlayerVmApiClient, PlayerVmApiClient>(p =>
            {
                var httpContextAccessor = p.GetRequiredService<IHttpContextAccessor>();
                var httpClientFactory = p.GetRequiredService<IHttpClientFactory>();
                var clientOptions = p.GetRequiredService<ClientOptions>();

                if (String.IsNullOrEmpty(clientOptions.urls.vmApi))
                {
                    return null;
                }
                var vmUri = new Uri(clientOptions.urls.vmApi);

                string authHeader = httpContextAccessor.HttpContext.Request.Headers["Authorization"];

                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = vmUri;
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

                var apiClient = new PlayerVmApiClient(httpClient, true)
                {
                    BaseUri = vmUri
                };

                return apiClient;
            });
        }


    }
}
