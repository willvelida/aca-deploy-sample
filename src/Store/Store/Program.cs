using Microsoft.FeatureManagement;
using Refit;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
   WebRootPath = "wwwroot"
});

// Add services to the container.
builder.Configuration.AddEnvironmentVariables();
var appConfig = builder.Configuration.GetValue<string>("AzureAppConfig");
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(appConfig)
    .UseFeatureFlags(featureFlagOptions => featureFlagOptions.Label = builder.Configuration.GetValue<string>("RevisionLabel"));
});
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient("Products", (httpClient) => httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("ProductsApi")));
builder.Services.AddHttpClient("Inventory", (httpClient) => httpClient.BaseAddress = new Uri(builder.Configuration.GetValue<string>("InventoryApi")));
builder.Services.AddScoped<IStoreBackendClient, StoreBackendClient>();
builder.Services.AddMemoryCache();
builder.Services.AddFeatureManagement();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public class Product
{
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
}

public interface IStoreBackendClient
{
    [Get("/products")]
    Task<List<Product>> GetProducts();

    [Get("/inventory/{productId}")]
    Task<int> GetInventory(string productId);
}

public class StoreBackendClient : IStoreBackendClient
{
    IHttpClientFactory _httpClientFactory;

    public StoreBackendClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<int> GetInventory(string productId)
    {
        var client = _httpClientFactory.CreateClient("Inventory");
        return await RestService.For<IStoreBackendClient>(client).GetInventory(productId);
    }

    public async Task<List<Product>> GetProducts()
    {
        var client = _httpClientFactory.CreateClient("Products");
        return await RestService.For<IStoreBackendClient>(client).GetProducts();
    }
}
