using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Interfaces;
using ProductManagement.Infrastructure.Caching;
using ProductManagement.Infrastructure.MongoDB;
using ProductManagement.Infrastructure.Persistence;
using ProductManagement.Infrastructure.Repositories;
using StackExchange.Redis;

namespace ProductManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // PostgreSQL — support both Npgsql format and postgres:// URL (e.g. from Render)
        var pgConn = ToNpgsqlConnectionString(config.GetConnectionString("PostgreSQL")!);
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(pgConn,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // MongoDB
        services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(config.GetConnectionString("MongoDB")));
        services.AddScoped<IProductAttributeService>(sp =>
            new ProductAttributeService(
                sp.GetRequiredService<IMongoClient>(),
                config["MongoDB:DatabaseName"] ?? "product_management"));

        // Redis
        var redisConn = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
            services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }

        // Repositories & UoW
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    // Converts postgres://user:pass@host:port/db to Npgsql key=value format
    private static string ToNpgsqlConnectionString(string connStr)
    {
        if (!connStr.StartsWith("postgres://") && !connStr.StartsWith("postgresql://"))
            return connStr;

        var uri = new Uri(connStr);
        var userInfo = uri.UserInfo.Split(':');
        var builder = new System.Text.StringBuilder();
        builder.Append($"Host={uri.Host};");
        if (uri.Port > 0) builder.Append($"Port={uri.Port};");
        builder.Append($"Database={uri.AbsolutePath.TrimStart('/')};");
        builder.Append($"Username={userInfo[0]};");
        if (userInfo.Length > 1) builder.Append($"Password={userInfo[1]};");
        builder.Append("SSL Mode=Require;Trust Server Certificate=true;");
        return builder.ToString();
    }
}
