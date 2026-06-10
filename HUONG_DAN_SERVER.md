# Hướng dẫn SERVER gRPC — `GrpcServiceDemo`

Tài liệu mô tả ngắn gọn cách server gRPC `EmployeeCRUD` hoạt động, đủ để demo cho giảng viên.

---

## 1. gRPC là gì?

- **gRPC** = Google Remote Procedure Call: cho phép chương trình này **gọi hàm nằm trong chương trình khác** qua mạng, như gọi hàm cục bộ.
- Dùng **HTTP/2** + **Protocol Buffers (protobuf)**: dữ liệu mã hóa **nhị phân** → nhỏ và nhanh hơn JSON/REST.
- **Hợp đồng** giữa 2 bên là file `.proto`. Server và client **dùng chung 1 proto** để biết có hàm nào, tham số gì, trả về gì.

---

## 2. Bức tranh tổng thể

```
   CLIENT (MVC)                              SERVER (GrpcServiceDemo)
 ┌──────────────────────┐      HTTP/2     ┌──────────────────────────┐
 │ stub EmployeeCRUD-    │ ─────────────► │ EmployeeCRUDService       │
 │ Client.SelectAll()... │   (protobuf    │   : EmployeeCRUDBase      │
 │                       │ ◄───────────── │        │                  │
 └──────────────────────┘    nhị phân)    │        ▼                  │
                                          │   EmployeeStore (List RAM)│
                                          └──────────────────────────┘
         cùng dùng  ──►   Protos/EmployeeCRUD.proto   ◄──  cùng dùng
```

---

## 3. Các file của server

| File | Vai trò |
|------|---------|
| `Protos/EmployeeCRUD.proto` | **Hợp đồng**: định nghĩa service + 5 RPC + các message |
| `GrpcServiceDemo.csproj` | Khai báo `Grpc.AspNetCore`; đăng ký proto sinh code **phía Server** |
| `Services/EmployeeCRUDService.cs` | **Cài đặt thật** của 5 RPC (logic) |
| `Services/EmployeeStore.cs` | "Database" giả lập bằng `List<Employee>` trong RAM |
| `Program.cs` | Mở cổng 5001 HTTP/2, gắn service vào pipeline |

---

## 4. File proto — trái tim hệ thống

```proto
syntax = "proto3";
package Northwind;
option csharp_namespace = "GrpcServiceDemo";   // server dùng namespace này

service EmployeeCRUD {
  rpc SelectAll  (Empty)          returns (Employees);  // lấy tất cả
  rpc SelectByID (EmployeeFilter) returns (Employee);   // lấy 1 theo ID
  rpc Insert     (Employee)       returns (Empty);      // thêm
  rpc Update     (Employee)       returns (Empty);      // sửa
  rpc Delete     (EmployeeFilter) returns (Empty);      // xóa
}

message Employee       { int32 employeeID = 1; string firstName = 2; string lastName = 3; }
message Employees      { repeated Employee items = 1; }   // repeated = danh sách
message EmployeeFilter { int32 employeeID = 1; }
message Empty          { }                                // message rỗng
```

- `service` = nhóm hàm gọi từ xa; mỗi `rpc Tên (ThamSố) returns (KếtQuả)`.
- `message` = cấu trúc dữ liệu (như class). Số `= 1, = 2` là **field number** (định danh trường khi mã hóa nhị phân), **không phải giá trị**.
- `csharp_namespace` quyết định namespace code C# sinh ra. Server để `GrpcServiceDemo`, client để `GrpcClientDemo`; phần còn lại **giống hệt**.

---

## 5. Grpc.Tools tự sinh code

`GrpcServiceDemo.csproj`:
```xml
<PackageReference Include="Grpc.AspNetCore" Version="2.57.0" />
<!-- Server → sinh class base trừu tượng để mình kế thừa -->
<Protobuf Include="Protos\EmployeeCRUD.proto" GrpcServices="Server" />
```

Khi `dotnet build`, Grpc.Tools đọc proto và sinh (trong `obj/`):
- Các class message: `Employee`, `Employees`, `EmployeeFilter`, `Empty`.
- Class base trừu tượng `EmployeeCRUD.EmployeeCRUDBase` có 5 method `virtual`.

→ Việc của bạn: **kế thừa class base và override 5 method**. Không phải tự viết code mạng/HTTP/2.

---

## 6. Cài đặt 5 RPC — `EmployeeCRUDService.cs`

```csharp
public class EmployeeCRUDService : EmployeeCRUD.EmployeeCRUDBase   // kế thừa base do proto sinh
{
    public override Task<Employees> SelectAll(Empty req, ServerCallContext ctx)
    {
        var res = new Employees();
        res.Items.AddRange(EmployeeStore.GetAll());
        return Task.FromResult(res);
    }
    // SelectByID, Insert, Update, Delete: tương tự, gọi EmployeeStore
}
```

**Quy luật mọi method:** nhận `requestData` (đúng kiểu message trong proto) + `ServerCallContext` → xử lý → trả `Task<KếtQuả>`.

**Báo lỗi kiểu gRPC** (trong `SelectByID`, khi không tìm thấy):
```csharp
throw new RpcException(new Status(StatusCode.NotFound, "Không tìm thấy..."));
```
gRPC có status code riêng (`NotFound`, `InvalidArgument`...) thay cho HTTP 404/400. Client bắt được để xử lý.

---

## 7. "Database" — `EmployeeStore.cs`

- `static List<Employee>` trong RAM, khởi tạo 3 nhân viên: Nancy, Andrew, Janet.
- Mọi thao tác bọc trong `lock (_lock)` để **thread-safe** (server xử lý nhiều request đồng thời).
- `GetAll()` / `Find()` trả **bản sao** (`.Clone()`) để bên ngoài không sửa trực tiếp dữ liệu gốc.
- **`Add()` tự sinh ID** khi client gửi ID = 0:
  ```csharp
  if (clone.EmployeeID == 0)
      clone.EmployeeID = _employees.Count == 0 ? 1 : _employees.Max(e => e.EmployeeID) + 1;
  ```
  → nhân viên mới nhận ID = max + 1 (không bị ID = 0).
- ⚠️ Dữ liệu trong RAM → **tắt server là mất**, khởi động lại quay về 3 nhân viên gốc.

---

## 8. Khởi động — `Program.cs`

```csharp
builder.Services.AddGrpc();                          // bật gRPC
builder.WebHost.ConfigureKestrel(o =>                // cấu hình web server
{
    o.ListenLocalhost(5001, lo =>
    {
        lo.Protocols = HttpProtocols.Http2;          // gRPC BẮT BUỘC HTTP/2
        lo.UseHttps();                               // TLS bằng chứng chỉ dev
    });
});
var app = builder.Build();
app.MapGrpcService<EmployeeCRUDService>();           // gắn service vào pipeline
app.MapGet("/", () => "...must be made through a gRPC client.");  // chặn truy cập bằng trình duyệt
app.Run();
```

Server lắng nghe **https://localhost:5001**, ép **HTTP/2** + **HTTPS**.

---

## 9. Luồng một cuộc gọi (ví dụ `SelectAll`)

1. Client gọi `client.SelectAll(new Empty())`.
2. Stub mã hóa tham số thành **nhị phân protobuf**, gửi qua **HTTP/2**.
3. Kestrel định tuyến tới `EmployeeCRUDService.SelectAll(...)`.
4. Method lấy dữ liệu từ `EmployeeStore`, đóng gói vào message `Employees`.
5. Server mã hóa kết quả, gửi trả.
6. Stub client **giải mã** lại thành object C# → controller dùng ngay.

---

## 10. Chạy & kiểm tra

```powershell
dotnet run --project "...\GrpcServiceDemo\GrpcServiceDemo"
```
- Lắng nghe tại **https://localhost:5001**.
- Mở URL này bằng trình duyệt → chỉ thấy dòng *"...must be made through a gRPC client."* → **đúng** (server sống nhưng chỉ phục vụ gRPC). Muốn thấy dữ liệu phải dùng **client** (xem `HUONG_DAN_CLIENT.md`).

---

## 11. Câu hỏi giảng viên hay hỏi

| Câu hỏi | Trả lời ngắn |
|---------|--------------|
| Tại sao phải HTTP/2? | gRPC dùng multiplexing/streaming của HTTP/2; HTTP/1.1 không hỗ trợ. |
| `= 1, = 2` trong proto? | Field number — định danh trường trong gói nhị phân, không phải giá trị. |
| Server vs client khác proto chỗ nào? | Chỉ khác `csharp_namespace` và `GrpcServices` (Server/Client). Định nghĩa giống hệt. |
| Dữ liệu lưu ở đâu? | `List<Employee>` trong RAM, mất khi tắt server. |
| Vì sao có `lock`? | Server đa luồng, tránh hai request sửa list cùng lúc. |
| ID nhân viên mới ở đâu ra? | Server tự sinh max+1 trong `EmployeeStore.Add` khi client gửi ID=0. |
