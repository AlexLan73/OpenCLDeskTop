using Grpc.Core;
using GrpcService1;  // Пространство имён из proto-файла
using System;
using System.Linq;
using System.Threading.Tasks;
using GrpcService1.Services;

/*
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
*/

using Grpc.AspNetCore.Server;
using GrpcService1.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

var app = builder.Build();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<VectorServiceImpl>();
app.MapGet("/", () => "gRPC сервер запущен");

app.Run();