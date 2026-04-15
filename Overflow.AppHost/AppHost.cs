using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("production")
    .WithDashboard(dashboard => dashboard.WithHostPort(8080)); // 配置仪表盘的端口为 8080，允许用户通过该端口访问和监控分布式应用程序的状态和性能

var keycloakPassword = builder.AddParameter("keycloak-password", secret: true); // 添加一个参数化密码，名称为 "keycloak-password"，并将其标记为秘密（secret: true），以确保在使用时不会直接暴露密码值

var keycloak = builder.AddKeycloak("keycloak", 6001)
    .WithDataVolume("keycloak-data")
    .WithRealmImport("../infra/realms")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KC_HOSTNAME_STRICT", "false")
    .WithEndpoint(6001, 8080, "keycloak", isExternal: true);


var postgres = builder.AddPostgres("postgres", port: 5432) // 添加一个名为 "postgres" 的 PostgreSQL 数据库服务，并将其默认暴露的 5432 端口映射到主机上的同一端口
    .WithDataVolume("postgres-data") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithPgAdmin(); // 添加 PgAdmin 管理工具，方便管理 PostgreSQL 数据库

var questionDb = postgres.AddDatabase("questionDb"); // 用于QuestionService的数据库

//var typesenseApikey = builder.AddParameter("typesense-api-key", secret: true);



var typesenseApikey =builder.Environment.IsDevelopment()
    ? builder.Configuration["Parameters:typesense-api-key"] // 在开发
    ?? throw new InvalidOperationException("无法获取Typesense API密钥")
    : "${TYPESENSE_API_KEY}"; // 在生产环境中使用环境变量来获取 Typesense API 密钥，确保安全性和灵活性

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0") // 添加一个名为 "typesense" 的容器服务，使用 "typesense/typesense" 镜像的 30.0 版本
    .WithArgs("--data-dir", "/data", "--api-key", typesenseApikey, "--enable-cors") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithVolume("typesense-data", "/data") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithEnvironment("TYPESENSE_API_KEY", typesenseApikey)
    .WithHttpEndpoint(8108, 8108, name: "typesense"); // 将容器内的 8108 端口映射到主机上的同一端口，方便访问 Typesense 服务)

var typesenseContainer=typesense.GetEndpoint("typesense"); // 获取 "typesense" 服务的端点信息，方便在其他服务中引用和访问该服务


var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin(port: 15672); // 添加 RabbitMQ 管理插件，默认暴露 15672 端口用于访问管理界面

var questionService = builder.AddProject<Projects.Questionservice>("question-svc")  // 添加一个名为 "question-svc" 的项目服务
    .WithReference(keycloak) // 将 "question-svc" 项目服务与 "keycloak" 服务建立引用关系，确保 "question-svc" 可以访问 "keycloak" 提供的功能
    .WithReference(questionDb)// 将 "question-svc" 项目服务与 "questionDb" 数据库服务建立引用关系，确保 "question-svc" 可以访问 "questionDb" 提供的数据库功能
    .WithReference(rabbitmq) // 将 "question-svc" 项目服务与 "messaging" RabbitMQ 服务建立引用关系，确保 "question-svc" 可以访问 RabbitMQ 提供的消息队列功能
    .WithEndpoint(7001, 8080, "question-svc", isExternal: true)
    .WaitFor(keycloak) // 配置 "question-svc" 项目服务在启动时等待 "keycloak" 服务完全启动后再继续执行，确保依赖关系得到满足
    .WaitFor(questionDb) // 配置 "question-svc" 项目服务在启动时等待 "questionDb" 服务完全启动后再继续执行，确保依赖关系得到满足
    .WaitFor(rabbitmq); // 配置 "question-svc" 项目服务在启动时等待 "messaging" RabbitMQ 服务完全启动后再继续执行，确保依赖关系得到满足


var searchService =builder.AddProject<Projects.SearchService>("search-svc") // 添加一个名为 "search-svc" 的项目服务
    .WithEnvironment("typesense-api-key",typesenseApikey)
    .WithReference(typesenseContainer) // 将 "search-svc" 项目服务与 "typesense" 服务建立引用关系，确保 "search-svc" 可以访问 "typesense" 提供的功能
    .WithReference(rabbitmq)
    .WithEndpoint(7003, 8080, "search-svc", isExternal: true)
    .WaitFor(typesense)
    .WaitFor(rabbitmq); // 配置 "search-svc" 项目服务在启动时等待 "typesense" 服务完全启动后再继续执行，确保依赖关系得到满足

var yarp = builder.AddYarp("gateway")
    .WithConfiguration(yarpBuilder =>
    {
        yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
        yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
    })// 添加一个名为 "gateway" 的 YARP 反向代理服务，并通过配置路由将特定的请求路径（如 "/questions/{**catch-all}"、"/tags/{**catch-all}" 和 "/search/{**catch-all}"）转发到相应的后端服务（如 "question-svc" 和 "search-svc"），实现请求的负载均衡和路由转发
    .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")// 配置 "gateway" 服务的环境变量，设置 ASP.NET Core 应用程序的 URL 绑定为 "http://*:8001"，允许该服务在主机上的 8001 端口上接受来自任何 IP 地址的 HTTP 请求
    .WithEndpoint(port: 8001, targetPort: 8001, scheme: "http", name: "gateway", isExternal: true);// 配置 "gateway" 服务的端点信息，将容器内的 8001 端口映射到主机上的同一端口，并指定使用 HTTP 协议，方便外部访问该服务

//var yarp = builder.AddYarp("gateway")
//    .WithConfiguration(yarpBuilder =>
//    {
//        yarpBuilder.AddRoute("/questions/{**catch-all}", questionService);
//        yarpBuilder.AddRoute("/tags/{**catch-all}", questionService);
//        yarpBuilder.AddRoute("/search/{**catch-all}", searchService);
//        // 添加 Keycloak 路由（可选，如果你想通过 Gateway 访问）
//        yarpBuilder.AddRoute("/auth/{**catch-all}", keycloak);
//        yarpBuilder.AddRoute("/realms/{**catch-all}", keycloak);
//    })
//    .WithEnvironment("ASPNETCORE_URLS", "http://*:8001")
//    .WithEndpoint(port: 8001, targetPort: 8001, scheme: "http", name: "gateway", isExternal: true);

builder.Build().Run();
