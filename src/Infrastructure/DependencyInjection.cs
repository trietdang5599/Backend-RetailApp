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
}
