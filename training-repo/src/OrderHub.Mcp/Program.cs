using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderHub.Core.Interfaces;
using OrderHub.Core.Services;
using OrderHub.Infrastructure.Data;
using OrderHub.Infrastructure.Repositories;

// 預設走 stdio(MCP client 直接啟動本行程);加 --http 改用 HTTP transport。
var useHttp = args.Contains("--http", StringComparer.OrdinalIgnoreCase);
//var hostArgs = args.Where(a => !string.Equals(a, "--http", StringComparison.OrdinalIgnoreCase)).ToArray();

if (useHttp)
{
    // HTTP 版:給 n8n 等遠端 client 用,streamable HTTP 端點在 http://localhost:3001
    var builder = WebApplication.CreateBuilder(args);
    AddOrderHubServices(builder.Services, builder.Configuration);
    builder.Services.AddMcpServer()
        .WithHttpTransport(options => options.Stateless = true)
        .WithTools<OrderHubTools>()
        .WithResources<OrderHubResources>()
        .WithPrompts<OrderHubPrompts>();

    var app = builder.Build();
    app.MapMcp();
    app.Run("http://localhost:3001"); // 若果port已被暫用則另選port
}
else
{
    // stdio 版:活動 2 的原樣。stdout 是協定通道,log 一律走 stderr
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
    AddOrderHubServices(builder.Services, builder.Configuration);
    builder.Services.AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<OrderHubTools>()
        .WithResources<OrderHubResources>()
        .WithPrompts<OrderHubPrompts>();

    await builder.Build().RunAsync();
}

// 與 OrderHub.Web 相同的分層接線:工具走 service / repository,不直接摸 DbContext
static void AddOrderHubServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<OrderHubDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("Default")
            ?? "Server=localhost;Database=OrderHubTraining;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"));

    // 與 OrderHub.Web 相同的分層接線:工具走 service / repository,不直接摸 DbContext
    services.AddScoped<ICustomerRepository, CustomerRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddScoped<IOrderService, OrderService>();
}
