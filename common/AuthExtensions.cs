using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public static class AuthExtensions
    {
        public static IServiceCollection AddKeyCloakAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication()
                .AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Audience = "overflow";
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        //ValidIssuers =
                        //[
                        //    "http://localhost:6001/realms/overflow",
                        //    "http://keycloak/realms/overflow",
                        //    "http://id.overflow.local/realms/overflow",
                        //]
                        ValidIssuers = new[]
                        {
                            "http://localhost:6001/realms/overflow",     // 外部访问
                            "http://keycloak:8080/realms/overflow",      // ✅ 容器内部（必须带 8080）
                            "http://host.docker.internal:6001/realms/overflow", // Docker Desktop
                        }
                    };
                });

            return services;
        }


    }
}
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.IdentityModel.Tokens;

//namespace common
//{
//    public static class AuthExtensions
//    {
//        public static IServiceCollection AddKeyCloakAuthentication(
//    this IServiceCollection services,
//    IConfiguration configuration)
//        {
//            var authority = configuration["Keycloak:Authority"]
//                            ?? "http://localhost:6001/realms/overflow";

//            Console.WriteLine($">>> Keycloak Authority: {authority}"); // 确认配置读取正确

//            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//                .AddJwtBearer(options =>
//                {
//                    options.RequireHttpsMetadata = false;
//                    options.Audience = "overflow";
//                    options.Authority = authority;
//                    options.TokenValidationParameters = new TokenValidationParameters
//                    {
//                        ValidateIssuer = true,
//                        ValidIssuers =
//                        [
//                            "http://localhost:6001/realms/overflow",
//                    "http://keycloak/realms/overflow",
//                    "http://id.overflow.local/realms/overflow",
//                        ],
//                        ValidateAudience = true,
//                        ValidAudiences = ["overflow"],
//                        ValidateLifetime = true,
//                    };
//                    options.Events = new JwtBearerEvents
//                    {
//                        OnAuthenticationFailed = ctx =>
//                        {
//                            Console.WriteLine($">>> Auth失败: {ctx.Exception.GetType().Name}");
//                            Console.WriteLine($">>> 详细错误: {ctx.Exception.Message}");
//                            return Task.CompletedTask;
//                        },
//                        OnTokenValidated = ctx =>
//                        {
//                            Console.WriteLine(">>> Token验证成功!");
//                            return Task.CompletedTask;
//                        }
//                    };
//                });
//            return services;
//        }
//    }
//}