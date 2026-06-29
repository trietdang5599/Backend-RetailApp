using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ProductManagement.API.Extensions;
using ProductManagement.Application.Products.Commands.CreateProduct;
using ProductManagement.Infrastructure;
using ProductManagement.Infrastructure.Persistence;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Product Management API", Version = "v1" });
});

builder.Services.AddCors(opt => opt.AddPolicy("AllowFrontend", p =>
{
    if (builder.Environment.IsDevelopment())
        p.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
         .AllowAnyHeader().AllowAnyMethod();
    else
        p.WithOrigins(builder.Configuration["AllowedOrigins"]?.Split(',') ?? [])
         .AllowAnyHeader().AllowAnyMethod();
}));

// MediatR 12 — registration via DI extensions built in
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateProductHandler).Assembly));

builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();

builder.Services.AddInfrastructure(builder.Configuration);

// RateLimiting is built into ASP.NET Core 7+ — no separate package needed
builder.Services.AddRateLimiter(opt =>
{
    var permitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100);
    var windowSeconds = builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60);
    var queueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit", 0);

    opt.AddFixedWindowLimiter("api", lim =>
    {
        lim.Window = TimeSpan.FromSeconds(windowSeconds);
        lim.PermitLimit = permitLimit;
        lim.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        lim.QueueLimit = queueLimit;
    });

    opt.RejectionStatusCode = 429;
});

builder.Services.AddResponseCompression(opt => opt.EnableForHttps = true);

var app = builder.Build();

app.UseAppMiddleware();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Management API v1"));

app.UseRouting();
app.UseCors("AllowFrontend");
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseResponseCompression();
app.UseSerilogRequestLogging();
app.MapControllers().RequireRateLimiting("api");

// Auto-migrate on startup (skipped in Testing — InMemory DB uses EnsureCreated instead)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();

public partial class Program { }
