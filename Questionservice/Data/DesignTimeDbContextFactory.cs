using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Questionservice.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<QuestionDbContext>
    {
        public QuestionDbContext CreateDbContext(string[] args)
        {
            var projectDir = FindProjectDirectory(Directory.GetCurrentDirectory());
            var basePath = projectDir ?? Directory.GetCurrentDirectory();

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                                   ?? Environment.GetEnvironmentVariable("CONNECTION_STRING");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // 开发时回退（根据本地环境修改），但优先应在 appsettings.json 或 环境变量 提供连接串
                connectionString = "Host=localhost;Database=questiondb;Username=postgres;Password=postgres";
            }

            var optionsBuilder = new DbContextOptionsBuilder<QuestionDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new QuestionDbContext(optionsBuilder.Options);
        }

        // 向上遍历目录，寻找第一个包含 appsettings.json 或 .csproj 的目录，返回该目录路径
        private static string? FindProjectDirectory(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                // 优先寻找 appsettings.json（通常在项目根）
                if (File.Exists(Path.Combine(dir.FullName, "appsettings.json")))
                    return dir.FullName;

                // 或者包含 .csproj 文件
                if (dir.EnumerateFiles("*.csproj").Any())
                    return dir.FullName;

                dir = dir.Parent;
            }

            return null;
        }
    }
}