var builder = DistributedApplication.CreateBuilder(args);

var keycloakPassword = builder.AddParameter("keycloak-password", secret: true); // 添加一个参数化密码，名称为 "keycloak-password"，并将其标记为秘密（secret: true），以确保在使用时不会直接暴露密码值

var keycloak = builder.AddKeycloak(
        name: "keycloak", // 设置用于服务发现的名称，可直接使用该名称来引用该服务；
        adminPassword: keycloakPassword,  // 使用参数化密码
        port: 6001  // 该容器默认暴露8080端口，此处设置为6001
    )
    .WithDataVolume("keycloak-data");//指定数据卷,即使关闭容器数据也会保留

var postgres = builder.AddPostgres("postgres", port: 5432) // 添加一个名为 "postgres" 的 PostgreSQL 数据库服务，并将其默认暴露的 5432 端口映射到主机上的同一端口
    .WithDataVolume("postgres-data") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithPgAdmin(); // 添加 PgAdmin 管理工具，方便管理 PostgreSQL 数据库

var typesenseApikey = builder.AddParameter("typesense-api-key", secret: true);

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0") // 添加一个名为 "typesense" 的容器服务，使用 "typesense/typesense" 镜像的 30.0 版本
    .WithArgs("--data-dir","/data","--api-key", typesenseApikey, "--enable-cors") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithVolume("typesense-data", "/data") // 指定数据卷，确保即使容器关闭，数据也会保留
    .WithHttpEndpoint(8108,8108,name:"typesense"); // 将容器内的 8108 端口映射到主机上的同一端口，方便访问 Typesense 服务)

var typesenseContainer=typesense.GetEndpoint("typesense"); // 获取 "typesense" 服务的端点信息，方便在其他服务中引用和访问该服务

var questionDb = builder.AddPostgres("questionDb"); // 用于QuestionService的数据库

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin(port: 15672); // 添加 RabbitMQ 管理插件，默认暴露 15672 端口用于访问管理界面

var questionService = builder.AddProject<Projects.Questionservice>("question-svc")  // 添加一个名为 "question-svc" 的项目服务
    .WithReference(keycloak) // 将 "question-svc" 项目服务与 "keycloak" 服务建立引用关系，确保 "question-svc" 可以访问 "keycloak" 提供的功能
    .WithReference(questionDb)// 将 "question-svc" 项目服务与 "questionDb" 数据库服务建立引用关系，确保 "question-svc" 可以访问 "questionDb" 提供的数据库功能
    .WithReference(rabbitmq) // 将 "question-svc" 项目服务与 "messaging" RabbitMQ 服务建立引用关系，确保 "question-svc" 可以访问 RabbitMQ 提供的消息队列功能
    .WaitFor(keycloak) // 配置 "question-svc" 项目服务在启动时等待 "keycloak" 服务完全启动后再继续执行，确保依赖关系得到满足
    .WaitFor(questionDb) // 配置 "question-svc" 项目服务在启动时等待 "questionDb" 服务完全启动后再继续执行，确保依赖关系得到满足
    .WaitFor(rabbitmq); // 配置 "question-svc" 项目服务在启动时等待 "messaging" RabbitMQ 服务完全启动后再继续执行，确保依赖关系得到满足


var searchService =builder.AddProject<Projects.SearchService>("search-svc") // 添加一个名为 "search-svc" 的项目服务
    .WithEnvironment("typesense-api-key",typesenseApikey)
    .WithReference(typesenseContainer) // 将 "search-svc" 项目服务与 "typesense" 服务建立引用关系，确保 "search-svc" 可以访问 "typesense" 提供的功能
    .WithReference(rabbitmq)
    .WaitFor(typesense)
    .WaitFor(rabbitmq); // 配置 "search-svc" 项目服务在启动时等待 "typesense" 服务完全启动后再继续执行，确保依赖关系得到满足



builder.Build().Run();
