using Grpc.Core;

namespace GrpcServiceDemo
{
    // Kế thừa class base do Grpc.Tools sinh ra từ file .proto
    public class EmployeeCRUDService : EmployeeCRUD.EmployeeCRUDBase
    {
        private readonly ILogger<EmployeeCRUDService> _logger;  

        public EmployeeCRUDService(ILogger<EmployeeCRUDService> logger)
        {
            _logger = logger;
        }

        public override Task<Employees> SelectAll(Empty requestData, ServerCallContext context)
        {
            _logger.LogInformation("SelectAll: get all employees");
            Employees responseData = new Employees();              
            responseData.Items.AddRange(EmployeeStore.GetAll());  
            return Task.FromResult(responseData);
        }

        public override Task<Employee> SelectByID(EmployeeFilter requestData, ServerCallContext context)
        {
            _logger.LogInformation("SelectByID: tìm nhân viên ID={Id}", requestData.EmployeeID);
            var emp = EmployeeStore.Find(requestData.EmployeeID);  
            if (emp == null)                                       
                throw new RpcException(new Status(StatusCode.NotFound,
                    $"Không tìm thấy nhân viên ID={requestData.EmployeeID}"));
            return Task.FromResult(emp);
        }

        public override Task<Empty> Insert(Employee requestData, ServerCallContext context)
        {
            _logger.LogInformation("Insert: thêm nhân viên ID={Id} ({First} {Last})",
                requestData.EmployeeID, requestData.FirstName, requestData.LastName);
            EmployeeStore.Add(requestData);                        
            return Task.FromResult(new Empty());                   
        }

        public override Task<Empty> Update(Employee requestData, ServerCallContext context)
        {
            _logger.LogInformation("Update: cập nhật nhân viên ID={Id}", requestData.EmployeeID);
            EmployeeStore.Update(requestData);                     
            return Task.FromResult(new Empty());
        }

        public override Task<Empty> Delete(EmployeeFilter requestData, ServerCallContext context)
        {
            _logger.LogInformation("Delete: xóa nhân viên ID={Id}", requestData.EmployeeID);
            EmployeeStore.Remove(requestData.EmployeeID);          
            return Task.FromResult(new Empty());
        }
    }
}
