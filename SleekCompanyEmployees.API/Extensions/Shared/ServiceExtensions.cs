using SleekCompanyEmployees.Presentation.Controllers;
using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Repository;
using Service;
using Service.Contracts;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace SleekCompanyEmployees.API.Extensions.Shared;

public static class ServiceExtensions
{
    public static void ConfigureCors(this IServiceCollection services) =>
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("X-Pagination"));
        });

    public static void ConfigureIISIntegration(this IServiceCollection services) =>
        services.Configure<IISOptions>(options =>
        {
        });

    public static void ConfigureRepositoryManager(this IServiceCollection services) =>
        services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static void ConfigureServiceManager(this IServiceCollection services) =>
        services.AddScoped<IServiceManager, ServiceManager>();

    //public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
    //services.AddDbContext<RepositoryContext>(opts =>
    //{
    //    var connectionString = configuration.GetConnectionString("sqlConnection");
    //    opts.UseSqlServer(connectionString, b => b.MigrationsAssembly("CompanyEmployees")));
    //}

    public static void ConfigureSqlContext(this IServiceCollection services, IConfiguration configuration) =>
    services.AddDbContextFactory<RepositoryContext>(opts =>
    {
        var postgressConnectionString = configuration.GetValue<string>("ConnectionStrings:sqlConnection");
        var connectionString = configuration.GetConnectionString("sqlConnection");
        opts.UseNpgsql(connectionString, o =>
        {
            o.MigrationsAssembly("SleekCompanyEmployees.API");
        });
    });



public static void ConfigureVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(opt =>
        {
            opt.ReportApiVersions = true;
            opt.AssumeDefaultVersionWhenUnspecified = true;
            opt.DefaultApiVersion = new ApiVersion(1, 0);
            opt.ApiVersionReader = new HeaderApiVersionReader("api-version");
            opt.Conventions.Controller<CompaniesController>()
                .HasApiVersion(new ApiVersion(1, 0));
            opt.Conventions.Controller<CompaniesV2Controller>()
                .HasDeprecatedApiVersion(new ApiVersion(2, 0));
        });
    }

    public static void ConfigureOutputCaching(this IServiceCollection services) =>
        services.AddOutputCache(opt =>
        {
            //opt.AddBasePolicy(bp => bp.Expire(TimeSpan.FromSeconds(10)));
            opt.AddPolicy("120SecondsDuration", p => p.Expire(TimeSpan.FromSeconds(120)));
        });

    public static void ConfigureRateLimitingOptions(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter("GlobalLimiter",
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 5,
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    Window = TimeSpan.FromMinutes(1)
                }));

            opt.AddPolicy("SpecificPolicy", context =>
                RateLimitPartition.GetFixedWindowLimiter("SpecificLimiter",
                partition => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10)
                }));

            opt.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = 429;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    await context.HttpContext.Response
                        .WriteAsync($"Too many requests. Please try again after {retryAfter.TotalSeconds} second(s).", token);
                else
                    await context.HttpContext.Response
                        .WriteAsync("Too many requests. Please try again later.", token);
            };
        });
    }

    public static void ConfigureIdentity(this IServiceCollection services)
    {
        var builder = services.AddIdentity<User, IdentityRole>(o =>
        {
            o.Password.RequireDigit = true;
            o.Password.RequireLowercase = false;
            o.Password.RequireUppercase = false;
            o.Password.RequireNonAlphanumeric = false;
            o.Password.RequiredLength = 4;
            o.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<RepositoryContext>()
        .AddDefaultTokenProviders();
    }

    public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtConfiguration = new JwtConfiguration();
        configuration.Bind(jwtConfiguration.Section, jwtConfiguration);

        var secretKey = Environment.GetEnvironmentVariable("SECRET") ?? "CodeMazeSecretKey";

        services.AddAuthentication(opt =>
        {
            opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtConfiguration.ValidIssuer,
                ValidAudience = jwtConfiguration.ValidAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
            };
        });
    }

    public static void AddJwtConfiguration(this IServiceCollection services, IConfiguration configuration) =>
        services.Configure<JwtConfiguration>(configuration.GetSection("JwtSettings"));

    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(s =>
        {
            s.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Sleek DevOps Master Series API",
                Version = "v1",
                Description = "SlekkCompanyEmployees API by SleekDevOps",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = "John Doe",
                    Email = "John.Doe@gmail.com",
                    Url = new Uri("https://twitter.com/johndoe"),
                },
                License = new OpenApiLicense
                {
                    Name = "SlekkCompanyEmployees API LICX",
                    Url = new Uri("https://example.com/license"),
                }
            });
            s.SwaggerDoc("v2", new OpenApiInfo { Title = "Code Maze API", Version = "v2" });

            var xmlFile = $"{typeof(Presentation.AssemblyReference).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            s.IncludeXmlComments(xmlPath);

            s.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Place to add JWT with Bearer",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            s.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Name = "Bearer",
                    },
                    new List<string>()
                }
            });
        });
    }
}
