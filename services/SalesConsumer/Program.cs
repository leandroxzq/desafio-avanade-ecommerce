using SalesConsumer;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var productServiceUrl = Environment.GetEnvironmentVariable("PRODUCTSERVICE_URL") ?? "http://localhost:5279/api/";
builder.Services.AddHttpClient("Inventory", client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

var saleServiceUrl = Environment.GetEnvironmentVariable("SALESERVICE_URL") ?? "http://localhost:5078/api/";
builder.Services.AddHttpClient("Sale", client =>
{
    client.BaseAddress = new Uri(saleServiceUrl);
});

var host = builder.Build();
host.Run();
