using BlazorApp;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using QuizApp;
using Grpc.Net.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddSingleton(sp => GrpcChannel.ForAddress("http://localhost:5195/"));
builder.Services.AddSingleton<QuizService.QuizServiceClient>(serviceProvider =>
{
    var channel = serviceProvider.GetRequiredService<GrpcChannel>();  // Pobieramy kana³ gRPC
    return new QuizService.QuizServiceClient(channel);  // Tworzymy klienta gRPC
});
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
