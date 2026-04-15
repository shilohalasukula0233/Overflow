using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using Wolverine;
using Wolverine.RabbitMQ;
namespace common
{
    public static class WolverineExtensions
    {
        public static async Task UseWolverineWithRabbitMqAsync(this IHostApplicationBuilder builder,Action<WolverineOptions> configureMessaging)
        {

            var retryPolicy = Policy
                .Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                    retryCount: 20, // 重试次数
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 指数退避策略
                    onRetry: (exception, timeSpan, retryCount) =>
                    {
                        Console.WriteLine($"正在进行第 {retryCount} 次重试，等待 {timeSpan.TotalSeconds} 秒后再次尝试...");
                    }
                );

            await retryPolicy.ExecuteAsync(async () =>
            {
                var endpoint = builder.Configuration.GetConnectionString("messaging") ?? throw new InvalidOperationException("找不到连接字符串");

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(endpoint)
                };
                await using var connection = await factory.CreateConnectionAsync();
            });//确保启动Wolverine之前，应用程序能够成功连接到RabbitMQ消息队列服务，如果连接失败，将按照定义的重试策略进行重试，直到成功连接或达到最大重试次数为止

            builder.Services.AddOpenTelemetry().WithTracing(TracerProviderBuilder =>
            {
                TracerProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
                    .AddService(builder.Environment.ApplicationName))//配置OpenTelemetry跟踪提供程序，设置资源构建器以包含应用程序名称，帮助识别和区分不同服务的跟踪数据
                    .AddSource("Wolverine");//一个跟踪源，名称为 "Wolverine"，用于生成和收集与Wolverine相关的跟踪数据，以便在分布式追踪系统中进行分析和监控
            });


            builder.UseWolverine(opts =>
            {
                opts.UseRabbitMqUsingNamedConnection("messaging")
                    .AutoProvision()
                    .DeclareExchange("questions");

                configureMessaging(opts);
            });
        }

    }
}
