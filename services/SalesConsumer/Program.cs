using SalesConsumer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var productServiceUrl = Environment.GetEnvironmentVariable("PRODUCTSERVICE_URL") ?? "http://localhost:5279/api/";
builder.Services.AddHttpClient("Inventory", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

var host = builder.Build();
host.Run();
