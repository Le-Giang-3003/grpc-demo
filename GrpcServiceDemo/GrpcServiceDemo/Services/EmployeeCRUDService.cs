using Grpc.Core;

namespace GrpcServiceDemo
{
    // Kế thừa class base do Grpc.Tools sinh ra từ file .proto
    public class EmployeeCRUDService : EmployeeCRUD.EmployeeCRUDBase
    {
        private readonly ILogger<EmployeeCRUDService> _logger;  // Ghi log

        // Logger được TIÊM VÀO qua constructor (Dependency Injection)
        public EmployeeCRUDService(ILogger<EmployeeCRUDService> logger)
        {
            _logger = logger;
        }

        // ---- SelectAll: trả về toàn bộ nhân viên trong store ----
        public override Task<Employees> SelectAll(Empty requestData, ServerCallContext context)
        {
            _logger.LogInformation("SelectAll: lấy tất cả nhân viên");
            Employees responseData = new Employees();              // Message trả về (theo proto)
            responseData.Items.AddRange(EmployeeStore.GetAll());   // Nhồi toàn bộ vào list 'items'
            return Task.FromResult(responseData);
        }

        // ---- SelectByID: lấy 1 nhân viên theo ID ----
        public override Task<Employee> SelectByID(EmployeeFilter requestData, ServerCallContext context)
        {
            _logger.LogInformation("SelectByID: tìm nhân viên ID={Id}", requestData.EmployeeID);
            var emp = EmployeeStore.Find(requestData.EmployeeID);  // Tìm trong store theo ID
            if (emp == null)                                       // Không thấy -> báo lỗi gRPC NotFound
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Không tìm thấy nhân viên ID={requestData.EmployeeID}"));
            return Task.FromResult(emp);
        }

        // ---- Insert: thêm mới ----
        public override Task<Empty> Insert(Employee requestData, ServerCallContext context)
        {
            _logger.LogInformation("Insert: thêm nhân viên ID={Id} ({First} {Last})",
                requestData.EmployeeID, requestData.FirstName, requestData.LastName);
            EmployeeStore.Add(requestData);                        // Thêm dữ liệu client gửi lên vào store
            return Task.FromResult(new Empty());                   // Trả về Empty (không cần dữ liệu)
        }

        // ---- Update: cập nhật theo EmployeeID ----
        public override Task<Empty> Update(Employee requestData, ServerCallContext context)
        {
            _logger.LogInformation("Update: cập nhật nhân viên ID={Id}", requestData.EmployeeID);
            EmployeeStore.Update(requestData);                     // Cập nhật trong store
            return Task.FromResult(new Empty());
        }

        // ---- Delete: xóa theo ID ----
        public override Task<Empty> Delete(EmployeeFilter requestData, ServerCallContext context)
        {
            _logger.LogInformation("Delete: xóa nhân viên ID={Id}", requestData.EmployeeID);
            EmployeeStore.Remove(requestData.EmployeeID);          // Xóa khỏi store theo ID
            return Task.FromResult(new Empty());
        }
    }
}
