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
        // PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("PostgreSQL"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // MongoDB
        services.AddSingleton<IMongoClient>(_ =>
        {
            var connStr = config.GetConnectionString("MongoDB")!;
            var settings = MongoClientSettings.FromConnectionString(connStr);
            settings.SslSettings = new SslSettings
            {
                EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                                    | System.Security.Authentication.SslProtocols.Tls13
            };
            settings.AllowInsecureTls = true;
            return new MongoClient(settings);
        });
        services.AddScoped<IProductAttributeService>(sp =>
            new ProductAttributeService(
                sp.GetRequiredService<IMongoClient>(),
                config["MongoDB:DatabaseName"] ?? "product_management"));


        // Redis — fallback to InMemory if Redis unavailable
        var redisConn = config.GetConnectionString("Redis");
        var redisConnected = false;
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            try
            {
                var redisConfig = ToRedisConfig(redisConn);
                var mux = ConnectionMultiplexer.Connect(redisConfig);
                services.AddSingleton<IConnectionMultiplexer>(mux);
                services.AddStackExchangeRedisCache(opt => opt.ConfigurationOptions = redisConfig);
                services.AddSingleton<ICacheService, RedisCacheService>();
                redisConnected = true;
            }
            catch { /* fall through to InMemory */ }
        }
        if (!redisConnected)
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, InMemoryCacheService>();
        }

        // Repositories & UoW
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    // Converts rediss://user:pass@host:port to ConfigurationOptions (handles Upstash URL format)
    private static ConfigurationOptions ToRedisConfig(string connStr)
    {
        if (connStr.StartsWith("redis://") || connStr.StartsWith("rediss://"))
        {
            var uri = new Uri(connStr);
            var cfg = new ConfigurationOptions
            {
                EndPoints = { { uri.Host, uri.Port > 0 ? uri.Port : 6380 } },
                Ssl = connStr.StartsWith("rediss://"),
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                             | System.Security.Authentication.SslProtocols.Tls13,
                AbortOnConnectFail = false,
            };
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':', 2);
                if (parts.Length == 2) cfg.Password = Uri.UnescapeDataString(parts[1]);
            }
            return cfg;
        }

        // Native StackExchange.Redis format: host:port,password=...,ssl=true
        var options = ConfigurationOptions.Parse(connStr);
        options.AbortOnConnectFail = false;
        options.SslProtocols = System.Security.Authentication.SslProtocols.Tls12
                             | System.Security.Authentication.SslProtocols.Tls13;
        return options;
    }
}
