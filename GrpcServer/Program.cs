
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Konfiguracja CORS
builder.Services.AddCors(setupAction =>
{
    setupAction.AddPolicy("AllowLocalhost", policy =>
    {
        policy.AllowAnyHeader()
              .WithOrigins("http://localhost:5025")
              .AllowAnyMethod()  // Zezwala na dowoln� metod� HTTP
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding"); // Nag��wki specyficzne dla gRPC
    });
});

var app = builder.Build();

// U�ycie CORS w aplikacji
app.UseCors("AllowLocalhost");

// Konfiguracja tras
app.UseRouting();

// Konfiguracja gRPC Web (je�li chcesz umo�liwi� komunikacj� z gRPC za pomoc� HTTP/1)
app.UseGrpcWeb(new GrpcWebOptions
{
    DefaultEnabled = true
});

// Endpointy dla gRPC
app.UseEndpoints(endpoints =>
{
    endpoints.MapGrpcService<QuizServiceImpl>();
});

app.Run();
