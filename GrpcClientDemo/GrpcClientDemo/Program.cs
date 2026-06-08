using GrpcClientDemo;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ===== Đăng ký gRPC client qua DI (Dependency Injection) =====
// Tạo MỘT lần và tái sử dụng cho mọi request (channel nên được dùng lại, không tạo mới liên tục).
builder.Services.AddSingleton(_ =>
{
    // CẢNH BÁO: handler dưới đây BỎ QUA kiểm tra chứng chỉ TLS — CHỈ DÙNG KHI DEV
    // (server dùng chứng chỉ dev tự ký). KHÔNG dùng ở production!
    var httpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    // Channel = kênh kết nối HTTP/2 tới server gRPC
    var channel = GrpcChannel.ForAddress(
        "https://localhost:5001",
        new GrpcChannelOptions { HttpHandler = httpHandler });

    // Stub (client proxy) có sẵn 5 method tương ứng 5 RPC trong proto
    return new EmployeeCRUD.EmployeeCRUDClient(channel);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
