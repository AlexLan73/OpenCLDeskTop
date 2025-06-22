using gRPCAPI01.Models;
using gRPCAPI01.Services;

using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

//Read configuration
var appSettingsSection = builder.Configuration.GetSection(nameof(AppSettings));
if (appSettingsSection == null)
    throw new ArgumentNullException("No configuration found for AppSettings");

var appSettings = appSettingsSection.Get<AppSettings>();
if (appSettings == null)
{
    appSettings = new AppSettings()
    {
        EnableReflection = true,
        SwaggerTitle = "gRPCAPI01",
        Version = "v1"
    };
}

builder.Services.Configure<AppSettings>(appSettingsSection);

// Add services to the container.
if (appSettings.EnableJSONTranscoding)
    builder.Services.AddGrpc().AddJsonTranscoding();
else
    builder.Services.AddGrpc();

if (appSettings.EnableReflection)
    builder.Services.AddGrpcReflection();

if (appSettings.EnableSwagger)
{
    builder.Services.AddGrpcSwagger();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(appSettings.Version, new OpenApiInfo { Title = appSettings.SwaggerTitle, Version = appSettings.Version });
    }
        );
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (appSettings.EnableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint(appSettings.SwaggerEndpoint, $"{appSettings.SwaggerTitle} {appSettings.Version}"));
}

if (appSettings.EnableReflection)
    app.MapGrpcReflectionService();

//Register and mad business services
app.MapGrpcService<GreeterService>();
app.MapGrpcService<UserService>();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
