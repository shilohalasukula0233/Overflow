using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SearchService.Data;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;
using Typesense.Setup;
using Wolverine;
using Wolverine.RabbitMQ;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry().WithTracing(TracerProviderBuilder =>
{
    TracerProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName))//配置OpenTelemetry跟踪提供程序，设置资源构建器以包含应用程序名称，帮助识别和区分不同服务的跟踪数据
        .AddSource("Wolverine");//一个跟踪源，名称为 "Wolverine"，用于生成和收集与Wolverine相关的跟踪数据，以便在分布式追踪系统中进行分析和监控
});

builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("messaging").AutoProvision();//配置Wolverine使用RabbitMQ作为消息传递机制，指定连接名称为 "messaging"，并启用自动配置功能，以便在应用程序启动时自动设置所需的RabbitMQ资源（如交换机、队列等）
    opts.ListenToRabbitQueue("questions.search",cfg=>
    {
        cfg.BindExchange("questions");//配置Wolverine监听名为 "questions.search" 的RabbitMQ队列，并将其绑定到名为 "questions" 的交换机，以便接收和处理来自该交换机的消息
    });
});


var typesenseUri=builder.Configuration["services:typesense:typesense:0"];
if (string.IsNullOrEmpty(typesenseUri))
    throw new InvalidOperationException("找不到typesenseUri");

var typesenseApiKey = builder.Configuration["typesense-api-key"];
if (string.IsNullOrEmpty(typesenseApiKey))
    throw new InvalidOperationException("找不到typesense的api密钥");

var uri = new Uri(typesenseUri);
builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typesenseApiKey;
    config.Nodes = new List<Node>
    {
        new(uri.Host,uri.Port.ToString(),uri.Scheme),
    };
});


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapDefaultEndpoints();

app.MapGet("/search", async (string query, ITypesenseClient client) =>
{
    string? tag = null;
    var tagMatch = Regex.Match(query, @"\[(.*?)\]");
    if (tagMatch.Success)
    {
        tag = tagMatch.Groups[1].Value;
        query = query.Replace(tagMatch.Value, "").Trim();
    }

    var searchParams = new SearchParameters(query, "title,content"); //创建搜索参数，指定搜索的字段为title和content

    if (!string.IsNullOrWhiteSpace(tag))
    {
        searchParams.FilterBy = $"tags:=[{tag}]";
    }

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);
        return Results.Ok(result.Hits.Select(hit => hit.Document));
    }
    catch (Exception e)
    {
        return Results.Problem("typesense搜索失败", e.Message);
    }
});

app.MapGet("/Search/similar-titles", async (string query, ITypesenseClient client) =>
{
    var searchParams = new SearchParameters(query, "title");//创建搜索参数，指定搜索的字段为title，以便在搜索时只考虑标题字段，从而找到与查询字符串在标题上相似的问题

    try
    {
        var result = await client.Search<SearchQuestion>("questions", searchParams);//执行搜索操作，使用指定的搜索参数在 "questions" 集合中进行搜索，并将结果存储在result变量中
        return Results.Ok(result.Hits.Select(hit => hit.Document));//返回搜索结果，提取每个命中的文档并将其作为响应的一部分返回给客户端
    }
    catch (Exception e)
    {
        return Results.Problem("typesense搜索失败", e.Message);
    }
});

using var scope = app.Services.CreateScope();
var client = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();
await Searchinitializer.EnsureIndexExists(client);


app.Run();


