using GrpcServiceDemo;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ gRPC vào DI container
builder.Services.AddGrpc();

// Cấu hình Kestrel: lắng nghe HTTPS tại cổng 5001 với giao thức HTTP/2 (bắt buộc cho gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;   // gRPC yêu cầu HTTP/2
        listenOptions.UseHttps();                        // Dùng chứng chỉ dev HTTPS mặc định
    });
});

var app = builder.Build();

// Ánh xạ service gRPC vào pipeline
app.MapGrpcService<EmployeeCRUDService>();

// Endpoint GET "/" — nhắc rằng phải gọi bằng gRPC client, không phải trình duyệt
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client.");

app.Run();
