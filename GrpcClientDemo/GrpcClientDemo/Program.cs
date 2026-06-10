using GrpcClientDemo;
using Grpc.Net.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Đăng ký gRPC client qua DI
// Tạo MỘT lần và tái sử dụng cho mọi request (channel nên được dùng lại, không tạo mới liên tục).
builder.Services.AddSingleton(_ =>
{
    var httpHandler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    // Channel = kênh kết nối HTTP/2 tới server gRPC
    var channel = GrpcChannel.ForAddress(
        "https://localhost:5001",
        new GrpcChannelOptions { HttpHandler = httpHandler });

    return new EmployeeCRUD.EmployeeCRUDClient(channel);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
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
