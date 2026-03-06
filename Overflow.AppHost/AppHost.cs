var builder = DistributedApplication.CreateBuilder(args);

var keycloakPassword = builder.AddParameter("keycloak-password", secret: true); // 添加一个参数化密码，名称为 "keycloak-password"，并将其标记为秘密（secret: true），以确保在使用时不会直接暴露密码值

var keycloak = builder.AddKeycloak(
        name: "keycloak", // 设置用于服务发现的名称，可直接使用该名称来引用该服务；
        adminPassword: keycloakPassword,  // 使用参数化密码
        port: 6001  // 该容器默认暴露8080端口，此处设置为6001
    )
    .WithDataVolume("keycloak-data");//指定数据卷,即使关闭容器数据也会保留


builder.Build().Run();
