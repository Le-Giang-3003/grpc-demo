# Hướng dẫn CLIENT gRPC — `GrpcClientDemo`

Client là web **ASP.NET Core MVC** có giao diện **CRUD tương tác** (Xem / Thêm / Sửa / Xóa), mỗi thao tác là một lời gọi gRPC tới server `EmployeeCRUD`. Đọc cùng `HUONG_DAN_SERVER.md`.

---

## 1. Nhiệm vụ client

Thay vì truy vấn database, client **gọi sang server gRPC** để đọc/ghi nhân viên rồi hiển thị/nhập liệu bằng HTML.

```
Trình duyệt ─HTTP─► CLIENT MVC (HomeController) ─gRPC/HTTP2─► SERVER (EmployeeCRUD)
   (form, nút)            │  stub _client                          │
                          └────── nhận Employee(s) ◄───────────────┘
                          ▼
                    Views (Index / Create / Edit) → HTML
```

---

## 2. Các file của client

| File | Vai trò |
|------|---------|
| `Protos/EmployeeCRUD.proto` | Bản sao y hệt proto server (chỉ khác `csharp_namespace`) |
| `GrpcClientDemo.csproj` | 3 package gRPC + đăng ký proto sinh code **phía Client** |
| `Program.cs` | Đăng ký **stub gRPC vào DI** (tạo 1 lần, dùng lại) |
| `Controllers/HomeController.cs` | 5 action: Index, Create, Edit, Delete |
| `Views/Home/Index.cshtml` | Bảng danh sách + nút Thêm/Sửa/Xóa |
| `Views/Home/Create.cshtml`, `Edit.cshtml` | Form nhập liệu |

---

## 3. Ba NuGet package (`.csproj`)

```xml
<PackageReference Include="Grpc.Net.Client" Version="2.57.0" />  <!-- tạo channel/kết nối -->
<PackageReference Include="Google.Protobuf" Version="3.24.4" />  <!-- mã hóa/giải mã message -->
<PackageReference Include="Grpc.Tools"      Version="2.57.0" />  <!-- sinh code C# từ .proto -->

<!-- Client → sinh STUB để GỌI server (khác Server sinh class base) -->
<Protobuf Include="Protos\EmployeeCRUD.proto" GrpcServices="Client" />
```

| | Server (`GrpcServices="Server"`) | Client (`GrpcServices="Client"`) |
|---|---|---|
| Sinh ra | `EmployeeCRUDBase` (để **kế thừa & cài đặt**) | `EmployeeCRUDClient` (stub để **gọi đi**) |
| Bạn làm gì | override 5 method | gọi 5 method, nhận kết quả |

> Proto client **giống hệt** server, chỉ khác đúng dòng `option csharp_namespace = "GrpcClientDemo";`. Đây là *hợp đồng chung* — sai một chữ là hai bên không hiểu nhau.

---

## 4. Đăng ký stub qua DI — `Program.cs`

```csharp
builder.Services.AddSingleton(_ =>
{
    // ⚠️ CHỈ DÙNG KHI DEV: bỏ qua kiểm tra chứng chỉ TLS tự ký của server
    var httpHandler = new HttpClientHandler {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    var channel = GrpcChannel.ForAddress("https://localhost:5001",
        new GrpcChannelOptions { HttpHandler = httpHandler });
    return new EmployeeCRUD.EmployeeCRUDClient(channel);   // stub dùng chung cho mọi request
});
```

- **Channel** = kênh kết nối HTTP/2 tới server. Tạo **một lần** rồi tái sử dụng (không nên tạo mới mỗi request).
- **Stub (`EmployeeCRUDClient`)** = đối tượng đại diện server; gọi `client.SelectAll(...)` *trông như* gọi hàm cục bộ nhưng thực ra gửi request qua mạng.
- Đăng ký `AddSingleton` → controller nhận stub qua constructor (Dependency Injection).

---

## 5. Controller — `HomeController.cs`

Stub được **tiêm vào** constructor, dùng cho mọi action:
```csharp
public HomeController(ILogger<HomeController> logger, EmployeeCRUD.EmployeeCRUDClient client)
{ _client = client; }
```

| Action | HTTP | RPC gọi | Mô tả |
|--------|------|---------|-------|
| `Index()` | GET | `SelectAll` | Lấy toàn bộ → đổ vào bảng |
| `Create()` | GET | — | Hiện form trống |
| `Create(Employee)` | POST | `Insert` | Thêm; ID để 0 → server tự sinh |
| `Edit(int id)` | GET | `SelectByID` | Lấy 1 nhân viên → đổ vào form |
| `Edit(Employee)` | POST | `Update` | Lưu thay đổi |
| `Delete(int id)` | POST | `Delete` | Xóa theo ID |

Ví dụ Create + Edit:
```csharp
[HttpPost] public IActionResult Create(Employee employee) {
    _client.Insert(employee);                    // gọi RPC Insert
    TempData["Message"] = "Đã thêm...";          // thông báo
    return RedirectToAction(nameof(Index));      // quay lại danh sách
}

[HttpGet] public IActionResult Edit(int id) {
    try { return View(_client.SelectByID(new EmployeeFilter { EmployeeID = id })); }
    catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound) {
        TempData["Error"] = $"Không tìm thấy ID={id}.";   // bắt lỗi NotFound từ server
        return RedirectToAction(nameof(Index));
    }
}
```
- Mỗi lời gọi `_client.Xxx(...)` = **1 vòng request/response qua HTTP/2** (stub mã hóa → gửi → server xử lý → trả về → stub giải mã).
- `TempData` mang thông báo thành công/lỗi sang trang Index sau khi redirect.
- Pattern **POST → xử lý → Redirect về Index** (PRG) giúp F5 không gửi lại form.

---

## 6. Views

**`Index.cshtml`** (`@model GrpcClientDemo.Employees`): duyệt `Model.Items` in mỗi nhân viên thành 1 dòng, kèm nút **Sửa** (link sang Edit) và **Xóa** (form POST có `confirm`), trên cùng có nút **+ Thêm**; hiển thị `TempData["Message"]`/`["Error"]`.

**`Create.cshtml` / `Edit.cshtml`** (`@model GrpcClientDemo.Employee`): form `asp-for` nhập FirstName/LastName.
- Create: không có ô ID (server tự sinh).
- Edit: ô `EmployeeID` để **readonly** (vẫn gửi đi để server biết sửa ai).

> MVC tự **model binding**: dữ liệu form (FirstName, LastName, EmployeeID) tự gán vào tham số `Employee` của action — chính là kiểu sinh từ proto.

---

## 7. Luồng đầy đủ một thao tác (ví dụ Thêm)

1. Người dùng bấm **+ Thêm** → `GET /Home/Create` → hiện form.
2. Nhập tên, bấm Lưu → `POST /Home/Create` (kèm anti-forgery token).
3. Controller gọi `_client.Insert(employee)` → stub gửi qua HTTP/2 tới server.
4. Server thêm vào `EmployeeStore` (tự sinh ID), trả `Empty`.
5. Controller set `TempData` và **redirect** về `Index`.
6. `Index` gọi `SelectAll` → bảng hiển thị nhân viên mới + dải thông báo xanh.

(Sửa/Xóa tương tự, đổi RPC tương ứng.)

---

## 8. Chứng chỉ TLS (hay gặp khi demo)

Server dùng chứng chỉ HTTPS **dev tự ký** → client mặc định từ chối (lỗi *"remote certificate is invalid"*). Hai cách:
1. **Sạch (khuyến nghị):** `dotnet dev-certs https --trust` một lần, rồi có thể bỏ đoạn `httpHandler`.
2. **Nhanh cho DEV (đang dùng):** `HttpClientHandler` bỏ qua kiểm tra cert.
   ⚠️ Chữ **Dangerous** là cảnh báo thật — **không dùng ở production**.

---

## 9. Trình tự DEMO

1. **Terminal 1 — server trước:**
   ```powershell
   dotnet run --project "...\GrpcServiceDemo\GrpcServiceDemo"
   ```
   Chờ `Now listening on: https://localhost:5001`.
2. **Terminal 2 — client:**
   ```powershell
   dotnet run --project "...\GrpcClientDemo\GrpcClientDemo"
   ```
   Mở URL client (mặc định **http://localhost:5036**, hoặc xem dòng `Now listening on:`).
3. Trên trang Index: bấm **+ Thêm**, **Sửa**, **Xóa** → mỗi thao tác là một cuộc gọi gRPC thật.
4. (Gây ấn tượng) Xem log ở terminal server: mỗi thao tác in `Insert/SelectByID/Update/Delete/SelectAll` → chứng minh client thật sự gọi sang server.

---

## 10. Tóm tắt Server ↔ Client

| | Server | Client |
|---|---|---|
| Loại | gRPC service (`Grpc.AspNetCore`) | MVC + 3 package gRPC |
| Proto | namespace `GrpcServiceDemo`, `GrpcServices="Server"` | namespace `GrpcClientDemo`, `GrpcServices="Client"` |
| Code sinh | `EmployeeCRUDBase` để kế thừa | `EmployeeCRUDClient` (stub) để gọi |
| Vai trò | Lắng nghe, xử lý, giữ dữ liệu | Gọi đi, nhập liệu, hiển thị |
| Kết nối | HTTP/2 + TLS, cổng 5001 | Trỏ tới `https://localhost:5001` |

> **Câu chốt khi demo:** *"Nhờ cùng một file proto, client gọi `client.SelectAll()` y như hàm trong máy, còn gRPC lo hết phần mã hóa nhị phân và truyền qua HTTP/2 — đó chính là Remote Procedure Call."*
