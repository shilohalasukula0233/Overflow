using common;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Questionservice.Data;
using Questionservice.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();//添加Aspire服务默认配置，包括注册服务发现、健康检查等功能
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TagService>();//将TagService注册为Scoped服务，这意味着每个HTTP请求将获得一个新的TagService实例，适用于需要在请求范围内维护状态的服务
builder.Services.AddKeyCloakAuthentication();

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDB");//添加Npgsql数据库上下文，配置QuestionDbContext以使用PostgreSQL数据库进行数据访问和操作



await builder.UseWolverineWithRabbitMqAsync(opts =>
{
    opts.PublishAllMessages().ToRabbitExchange("questions");
    opts.ApplicationAssembly = typeof(Program).Assembly;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 必须先启用身份验证中间件，再启用授权中间件
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();//映射控制器路由，使应用程序能够处理来自客户端的HTTP请求，并将其路由到相应的控制器方法进行处理
app.MapDefaultEndpoints();//映射默认端点，通常包括健康检查和服务发现等功能，使应用程序能够响应这些特定的请求

using var scope = app.Services.CreateScope();//使用using语句的原因：一旦scope对象超出其范围（即在using块结束时），它将被自动释放，确保任何在该范围内创建的服务实例都能正确地被清理和释放资源，避免内存泄漏或其他资源管理问题。
var services = scope.ServiceProvider;//用一个变量来获取当前作用域的服务提供者，以便在后续代码中使用它来解析和使用注册的服务，例如数据库上下文、日志记录器等。
try
{ 
    var context = services.GetRequiredService<QuestionDbContext>();//获取一个QuestionDbContext实例，用于与数据库进行交互，执行数据访问和操作
    await context.Database.MigrateAsync();//应用任何未应用的数据库迁移，确保数据库模式与当前的模型定义保持同步
}
catch (Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();//获取一个ILogger<Program>实例，用于记录日志信息，特别是在发生异常时记录错误信息
    logger.LogError(ex,"迁移或预置数据库时发生错误");//记录一个错误级别的日志，包含异常信息和一条描述性消息，指示在数据库种子数据过程中发生了错误
}

app.Run();
