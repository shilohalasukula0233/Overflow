using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Questionservice.Data;
using Questionservice.Services;
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

builder.Services.AddAuthentication()//添加身份验证服务，使应用程序能够处理用户身份验证和授权
    .AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>//配置Keycloak JWT Bearer身份验证选项，指定服务名称为 "keycloak"，领域（realm）为 "overflow"，并通过options参数进行进一步的配置
    {
        options.RequireHttpsMetadata = false;//在开发环境中，允许使用HTTP协议进行身份验证元数据的获取，通常在生产环境中应设置为true以确保安全性
        options.Audience = "overflow";//设置JWT令牌的受众（Audience）为 "overflow"，这意味着只有当令牌的受众与 "overflow" 匹配时，才会被认为是有效的，从而确保只有授权的客户端能够访问受保护的资源
    });

builder.AddNpgsqlDbContext<QuestionDbContext>("questionDB");//添加Npgsql数据库上下文，配置QuestionDbContext以使用PostgreSQL数据库进行数据访问和操作

builder.Services.AddOpenTelemetry().WithTracing(TracerProviderBuilder =>
{
    TracerProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName))//配置OpenTelemetry跟踪提供程序，设置资源构建器以包含应用程序名称，帮助识别和区分不同服务的跟踪数据
        .AddSource("Wolverine");//一个跟踪源，名称为 "Wolverine"，用于生成和收集与Wolverine相关的跟踪数据，以便在分布式追踪系统中进行分析和监控
});

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();//配置Wolverine使用RabbitMQ作为消息传递机制，指定连接名称为 "messaging"，并启用自动配置功能，以便在应用程序启动时自动设置所需的RabbitMQ资源（如交换机、队列等）
    opts.PublishAllMessages().ToRabbitExchange("questions");//配置Wolverine将所有消息发布到名为 "questions" 的RabbitMQ交换机，以便其他服务可以订阅和处理这些消息
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

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
