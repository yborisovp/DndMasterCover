using System.Reflection;
using System.Text.Json.Serialization;
using FR.DataAccess.Context;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Filters;
using DndMasterCover.Configuration;
using DndMasterCover.DataAccess.Context;
using DndMasterCover.DataAccess.Interfaces;
using DndMasterCover.DataAccess.Repositories;
using DndMasterCover.DataContracts.Interfaces;
using DndMasterCover.Middleware;
using DndMasterCover.Parsers;
using DndMasterCover.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    configuration.Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", p => p == "/health"));
});

builder.Configuration.AddEnvironmentVariables();

const string baseSectionConfig = "BaseConfiguration";

var swaggerConfig = builder.Configuration.GetSection(baseSectionConfig).Get<BaseConfiguration>()?.SwaggerConfig;
var baseConfiguration = builder.Configuration.GetSection(baseSectionConfig).Get<BaseConfiguration>();
if (baseConfiguration is null)
{
    throw new InvalidDataException("Base configuration was not provided");
}

builder.Services.AddControllers()
       .AddJsonOptions(options =>
                           options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter())
                      );
builder.Services.AddTransient<ProblemDetailsFactory, BaseProblemDetailsFactory>();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var databaseConfig = baseConfiguration.DatabaseConfig;
builder.Services.AddDbContext<DatabaseContext>(options =>
                                                   DatabaseContextFactory
                                                       .CreateDbContext((DbContextOptionsBuilder<DatabaseContext>)options,
                                                                        databaseConfig.FullConnectionString));
builder.Services.AddScoped<IDatabaseContextFactory<DatabaseContext>>(_ => new DatabaseContextFactory(databaseConfig
                                                                         .FullConnectionString));

builder.Services.AddScoped<IDatabaseContextFactory<DatabaseContext>>(_ => new DatabaseContextFactory(databaseConfig
                                                                         .FullConnectionString));

//Add services and repositories here

builder.Services.AddScoped<IEnemyService, EnemyService>();
builder.Services.AddScoped<IEnemyRepository, EnemyRepository>();
builder.Services.AddScoped<IEnemyParser, EnemiesParser>();

const string customPolicyName = "CustomCors";
var origins = (builder.Configuration.GetSection($"CorsConfiguration:Origins")
                      .Get<string[]>() ?? Array.Empty<string>())
              .Where(s => !string.IsNullOrEmpty(s))
              .ToArray();

builder.Services.AddCors(options => options.AddPolicy(name: customPolicyName,
                                                      corsPolicyBuilder =>
                                                      {
                                                          if (!origins.Any() ||
                                                              origins.Any(o => o.ToLower().Equals("all")))
                                                          {
                                                              corsPolicyBuilder.AllowAnyHeader()
                                                                               .AllowAnyMethod()
                                                                               .SetIsOriginAllowed(_ => true)
                                                                               .AllowCredentials();
                                                          }
                                                          else
                                                          {
                                                              corsPolicyBuilder.AllowAnyMethod()
                                                                               .AllowAnyHeader()
                                                                               .AllowCredentials()
                                                                               .WithOrigins(origins.ToArray());
                                                          }
                                                      }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRouting(options => options.LowercaseUrls = true);
if (swaggerConfig is { IsEnabled: true })
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = $"Swagger of Template service",
            Description = "Swagger of Template service build with .NET CORE 8.0"
        });
        options.EnableAnnotations();
        options.SupportNonNullableReferenceTypes();
        var xmlFilename =
            Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
        if (File.Exists(xmlFilename))
        {
            options.IncludeXmlComments(xmlFilename, true);
        }
    });
}


var app = builder.Build();
app.UseDeveloperExceptionPage();
if (swaggerConfig is { IsEnabled: true })
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHealthChecks("/health");

app.UseCors(customPolicyName);
app.UseHttpsRedirection();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();

app.Run();