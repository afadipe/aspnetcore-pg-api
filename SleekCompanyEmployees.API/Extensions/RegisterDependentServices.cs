using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SleekCompanyEmployees.API.Extensions.Shared;
using Contracts;
using Shared.DataTransferObjects;
using SleekCompanyEmployees.Presentation.ActionFilters;
using SleekCompanyEmployees.API.Utility;
using Service.DataShaping;

namespace SleekCompanyEmployees.API.Extensions;

public static class RegisterDependentServices
{
    static NewtonsoftJsonPatchInputFormatter GetJsonPatchInputFormatter() =>
    new ServiceCollection().AddLogging().AddMvc().AddNewtonsoftJson()
    .Services.BuildServiceProvider()
    .GetRequiredService<IOptions<MvcOptions>>().Value.InputFormatters
    .OfType<NewtonsoftJsonPatchInputFormatter>().First();
    //Custom service
    public static WebApplicationBuilder RegisterServices(this WebApplicationBuilder builder)
    {
        // Register your dependencies
        //builder.Services.Add...
        //builder.Services.AddControllers();

        // Add framework services.
        builder.Services.AddControllers(options =>
        {
            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;
            options.InputFormatters.Insert(0, GetJsonPatchInputFormatter());
            //options.Filters.Add(typeof(HttpGlobalExceptionFilter));
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        })
        .AddXmlDataContractSerializerFormatters()
      //.AddCustomCSVFormatter()
      .AddApplicationPart(typeof(SleekCompanyEmployees.Presentation.AssemblyReference).Assembly);

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.ConfigureCors();
        builder.Services.ConfigureIISIntegration();
        builder.Services.ConfigureRepositoryManager();
        builder.Services.ConfigureServiceManager();
        builder.Services.ConfigureSqlContext(builder.Configuration);
        builder.Services.AddAutoMapper(typeof(Program));
        builder.Services.AddScoped<ValidationFilterAttribute>();
        builder.Services.AddScoped<ValidateMediaTypeAttribute>();
        builder.Services.ConfigureVersioning();
        builder.Services.ConfigureOutputCaching();
        builder.Services.ConfigureRateLimitingOptions();

        builder.Services.AddAuthentication();
        builder.Services.ConfigureIdentity();
        builder.Services.ConfigureJWT(builder.Configuration);
        builder.Services.AddJwtConfiguration(builder.Configuration);

        builder.Services.ConfigureSwagger();
        builder.Services.AddScoped<IDataShaper<EmployeeDto>, DataShaper<EmployeeDto>>();
        builder.Services.AddScoped<IEmployeeLinks, EmployeeLinks>();

        return builder;
    }
}


