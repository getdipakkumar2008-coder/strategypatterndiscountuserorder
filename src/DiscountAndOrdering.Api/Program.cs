using System.Text.Json.Serialization;
using DiscountAndOrdering.Application.Discounts;
using DiscountAndOrdering.Application.Exceptions;
using DiscountAndOrdering.Application.Services;
using DiscountAndOrdering.Domain.Interfaces;
using DiscountAndOrdering.Infrastructure.Repositories;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// In-memory repositories (v1) — singletons so data survives across requests for the
// life of the process. Swappable for EF Core implementations later with no change to
// Application/Api (Constitution Principle II).
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();

// Discount configuration — percentages are config-driven (Constitution Principle IV);
// changing a tier's percentage is an appsettings.json edit, no rebuild.
builder.Services.Configure<DiscountSettings>(builder.Configuration.GetSection("DiscountSettings"));
builder.Services.AddScoped<IDiscountStrategyResolver, ConfigurableDiscountStrategyResolver>();

// Application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        // Request-shape validation failures (missing/invalid fields) map to 400 here,
        // so controllers don't need per-action try/catch for entity invariant checks.
        if (exception is ArgumentException or ValidationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = exception.Message });
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { message = "An unexpected error occurred." });
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
