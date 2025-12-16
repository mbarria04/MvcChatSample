
using MvcChatSample.Interfaces;
using MvcChatSample.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();


builder.Services.AddHttpClient<IOllamaClient, OllamaClient>(c =>
{
    c.BaseAddress = new Uri("http://localhost:11434/");
    c.Timeout = Timeout.InfiniteTimeSpan; // streaming
});

builder.Services.AddSingleton<RagService>();

builder.Services.AddSingleton<KnowledgeStore>();

//builder.Services.AddHttpClient("AssistantApi", (sp, client) =>
//{
//    var cfg = sp.GetRequiredService<IConfiguration>();
//    var baseUrl = cfg["AssistantApi:BaseUrl"];

//    client.BaseAddress = new Uri(baseUrl);
//    client.DefaultRequestHeaders.Accept.Add(
//        new MediaTypeWithQualityHeaderValue("application/json")
//    );

//    client.Timeout = TimeSpan.FromMinutes(5);
//});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
