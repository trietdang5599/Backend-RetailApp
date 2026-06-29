using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Infrastructure.Persistence;
using System.Net;
using Xunit;

namespace ProductManagement.Tests.API;

public class RateLimitTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override rate limit via config — cleaner than removing/re-adding services
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:PermitLimit"] = "5",
                ["RateLimiting:WindowSeconds"] = "60",
                ["RateLimiting:QueueLimit"] = "0"
            });
        });

        // Swap PostgreSQL → InMemory so tests don't need a real DB
        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor is not null) services.Remove(dbDescriptor);
            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("TestDb"));
        });
    }
}

// Each test creates its own factory → fresh rate limiter state, no cross-test bleed
public class RateLimitingTests : IDisposable
{
    private readonly RateLimitTestFactory _factory;
    private readonly HttpClient _client;

    public RateLimitingTests()
    {
        _factory = new RateLimitTestFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Requests_WithinLimit_ShouldSucceed()
    {
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/api/products");
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests,
                because: $"request #{i + 1} is within the 5-request limit");
        }
    }

    [Fact]
    public async Task Request_ExceedingLimit_ShouldReturn429()
    {
        for (var i = 0; i < 5; i++)
            await _client.GetAsync("/api/products");

        var response = await _client.GetAsync("/api/products");

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            because: "the 6th request exceeds the limit of 5 per minute");
    }

    [Fact]
    public async Task Concurrent_RequestsBeyondLimit_ShouldPartiallyReject()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync("/api/products"));

        var responses = await Task.WhenAll(tasks);

        responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .Should().Be(5);

        responses.Count(r => r.StatusCode != HttpStatusCode.TooManyRequests)
            .Should().Be(5);
    }
}
