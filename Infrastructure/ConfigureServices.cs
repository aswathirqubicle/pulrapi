using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Core.Application.Helpers;
using Core.Application.Interfaces;
using Core.Application.Services;
using Core.Domain.Entities;
using Core.Infrastructure.Persistence;
using Core.Infrastructure.Persistence.Interceptors;
using Core.Infrastructure.Security;
using Core.Infrastructure.Services;
using Core.Infrastructure.Services.Users;
using Core.Infrastructure.Swagger;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace Core.Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment, string MyAllowSpecificOrigins)
        {
            services.AddSignalR(cfg => cfg.EnableDetailedErrors = true);

            // Add memory cache for Apple public key caching
            services.AddMemoryCache();

            services.AddScoped<EntitySaveChangesInterceptor>();

            var connectionString = configuration.GetConnectionString("Pulr");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>());

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyAllowSpecificOrigins,
                    builder =>
                    {
                        builder
                            //.WithOrigins(configuration["Cors:Origins:Origin1"],
                            //         configuration["Cors:Origins:Origin2"],
                            //         configuration["Cors:Origins:Origin3"])
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowAnyOrigin();
                        //.SetIsOriginAllowed(origin => true);

                    });
            });

            services.AddHangfire(hf => hf.UsePostgreSqlStorage(connectionString, new PostgreSqlStorageOptions() { SchemaName = "cron" }));
            // Limit worker count to 2 to prevent CPU exhaustion from video transcoding
            services.AddHangfireServer(options => {
                options.WorkerCount = 2;
            });

            services
                .AddControllers(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddHttpClient();
            services.AddHttpContextAccessor();
            services.AddTransient<IHttpClientService, HttpClientService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddSingleton<IEmailLogoService, EmailLogoService>();
            services.AddTransient<IExchangeRateService, ExchangeRateService>();

            services.AddScoped<IUserBlockService, UserBlockService>();

            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IQueryHelperService, QueryHelperService>();
            //services.AddScoped<ISearchRepository, SearchRepository>();

            services.AddSingleton<IFacebookAuthService, FacebookAuthService>();
            services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            services.AddSingleton<IAppleAuthService, AppleAuthService>();
            // Register TokenBlacklistService
            services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();

            #region Auth Config

            //Configuration from AppSettings
            services.Configure<JWT>(configuration.GetSection("JWT"));
            services.AddIdentity<User, IdentityRole>(o =>
                {
                    o.Password.RequiredLength = 6;
                    //o.Password.RequireUppercase = true;
                    //o.Password.RequireDigit = true;
                    //o.Password.RequireNonAlphanumeric = true;
                    //o.Password.RequiredUniqueChars = 3;

                    o.User.RequireUniqueEmail = true;
                    //o.SignIn.RequireConfirmedEmail = true;

                    o.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                    o.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddTokenProvider<CustomEmailConfirmationTokenProvider<User>>("CustomEmailConfirmation");

            services.Configure<DataProtectionTokenProviderOptions>(o => { o.TokenLifespan = TimeSpan.FromHours(6); });

            // The following AddAuthentication and AddJwtBearer block has been removed as it is duplicated in Program.cs
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false;
                    o.SaveToken = true; // check this
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ClockSkew = TimeSpan.FromMinutes(5), // Allow small time drift
                        ValidIssuer = configuration["JWT:Issuer"],
                        ValidAudience = configuration["JWT:Audience"],
                        IssuerSigningKey = CreateJwtSigningKey(configuration["JWT:Key"])
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
            
                            // If the request is for our hubs...
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs")))
                            {
                                // Read the token out of the query string
                                context.Token = accessToken;
                            }
            
                            return Task.CompletedTask;
                        }
                    };
                });

            services.AddAuthorization(options =>
            {
                // protect all routes
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

                //options.AddPolicy("SuperAdmin", policy => { policy.Requirements.Add(new SuperAdminRequirement()); });
            });

            #endregion

            #region Services

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProfileService, ProfileService>();
            services.AddScoped<IStoreService, StoreService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IProfileSettingsService, ProfileSettingsService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IExpoNotificationService, ExpoNotificationService>();
            services.AddScoped<IBrandService, BrandService>();
            services.AddTransient<IFileUploadService, FileUploadService>();
            services.AddTransient<IImageProcessingService, ImageProcessingService>();
            services.AddTransient<IVideoTranscodingService, VideoTranscodingService>();
            services.AddTransient<IQueryHelperService, QueryHelperService>();
            services.AddHttpClient<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IBookmarkCollectionService, BookmarkCollectionService>();
            services.AddScoped<IPostPurgeService, PostPurgeService>();
            services.AddScoped<IStoryCleanupService, StoryCleanupService>();
            services.AddScoped<IStripeService, Core.Infrastructure.Services.Stripe.StripeService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IVideoProcessingService, VideoProcessingService>();

            // Background services
            services.AddHostedService<OrderCountdownService>();

            #endregion

            #region Swagger

            // Only register Swagger services in Development environment
            if (webHostEnvironment.IsDevelopment())
            {
                services.AddSingleton<DevOnlyEndpointsDocumentFilter>();
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "API",
                        Version = "v1",
                    });
                    // Hide dev-only delete-all-posts and delete-all-user-posts from Swagger when not in Development
                    options.DocumentFilter<DevOnlyEndpointsDocumentFilter>();
                    //TODO Fix swagger authorize role filter
                    //options.DocumentFilter<SwaggerAuthorizeRoleFilter>();
                    options.AddSecurityDefinition("BearerDefinition", new OpenApiSecurityScheme()
                    {
                        Name = "Authorization",
                        Description = "Type \'Bearer\' (no quotes) followed by space and paste your JWT token here.",
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Scheme = "Bearer"
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "BearerDefinition"
                                }
                            },
                            new List<string>()
                        }
                    });
                });
            }

            #endregion
            
            #region Hangfire
            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(configuration.GetConnectionString("Pulr"),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire", // For recurring jobs (ExchangeRate, Cleanup, etc.)
                        InvisibilityTimeout = TimeSpan.FromMinutes(5),
                        QueuePollInterval = TimeSpan.FromMilliseconds(200),
                        DistributedLockTimeout = TimeSpan.FromMinutes(1)
                    }));

            // Separate client for video processing jobs (cron schema - for PulrWorker)
            var videoJobStorage = new Hangfire.PostgreSql.PostgreSqlStorage(
                configuration.GetConnectionString("Pulr"),
                new PostgreSqlStorageOptions
                {
                    SchemaName = "cron", // PulrWorker listens to this schema
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.FromMilliseconds(200),
                    DistributedLockTimeout = TimeSpan.FromMinutes(1)
                });
            
            services.AddSingleton<Hangfire.IBackgroundJobClient>(
                new Hangfire.BackgroundJobClient(videoJobStorage));

            #endregion

            return services;
        }

        /// <summary>
        /// Creates a JWT signing key from the provided key string, handling both hex and UTF-8 formats
        /// </summary>
        /// <param name="key">The JWT key string</param>
        /// <returns>A SymmetricSecurityKey for JWT signing</returns>
        private static SymmetricSecurityKey CreateJwtSigningKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("JWT key cannot be null or empty", nameof(key));
            }

            byte[] keyBytes;
            
            // Check if the key is in hex format (even number of characters, all hex digits)
            if (key.All(c => "0123456789ABCDEFabcdef".Contains(c)) && key.Length % 2 == 0)
            {
                // Key is in hex format - use manual conversion for compatibility
                keyBytes = new byte[key.Length / 2];
                for (int i = 0; i < keyBytes.Length; i++)
                {
                    keyBytes[i] = Convert.ToByte(key.Substring(i * 2, 2), 16);
                }
            }
            else
            {
                // Key is in UTF-8 format
                keyBytes = Encoding.UTF8.GetBytes(key);
            }
            
            // Validate key length for HS256 (minimum 32 bytes = 256 bits)
            if (keyBytes.Length < 32)
            {
                throw new ArgumentException($"JWT key is too short. Minimum 32 bytes required, got {keyBytes.Length} bytes.", nameof(key));
            }
            
            return new SymmetricSecurityKey(keyBytes);
        }
    }
}
