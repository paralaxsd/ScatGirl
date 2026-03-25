using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScatGirl.Mcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();

builder.Services
    .AddMcpServer(options => {
        options.ServerInfo = new()
        {
            Name = "ScatGirl - C# Source Navigator",
            Version = ThisAssembly.AssemblyInformationalVersion
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly(typeof(ScatGirlTools).Assembly);

await builder.Build().RunAsync();
