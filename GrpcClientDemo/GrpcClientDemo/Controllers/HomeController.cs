using GrpcClientDemo.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GrpcClientDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        // Stub gRPC được TIÊM VÀO qua constructor (đăng ký ở Program.cs)
        private readonly EmployeeCRUD.EmployeeCRUDClient _client;

        public HomeController(ILogger<HomeController> logger, EmployeeCRUD.EmployeeCRUDClient client)
        {
            _logger = logger;
            _client = client;
        }

        // ===== DANH SÁCH: gọi RPC SelectAll lấy toàn bộ nhân viên =====
        public IActionResult Index()
        {
            var employees = _client.SelectAll(new Empty());
            return View(employees);
        }

        // ===== THÊM (hiển thị form trống) =====
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Employee());
        }

        // ===== THÊM (nhận dữ liệu form, gọi RPC Insert) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee employee)
        {
            // EmployeeID để 0 -> server tự sinh ID mới
            _client.Insert(employee);
            TempData["Message"] = $"Đã thêm nhân viên \"{employee.FirstName} {employee.LastName}\".";
            return RedirectToAction(nameof(Index));
        }

        // ===== SỬA (gọi RPC SelectByID lấy dữ liệu cũ, đổ vào form) =====
        [HttpGet]
        public IActionResult Edit(int id)
        {
            try
            {
                var emp = _client.SelectByID(new EmployeeFilter { EmployeeID = id });
                return View(emp);
            }
            catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                // Server trả về NotFound -> báo lỗi và quay về danh sách
                TempData["Error"] = $"Không tìm thấy nhân viên ID={id}.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ===== SỬA (nhận dữ liệu form, gọi RPC Update) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Employee employee)
        {
            _client.Update(employee);
            TempData["Message"] = $"Đã cập nhật nhân viên ID={employee.EmployeeID}.";
            return RedirectToAction(nameof(Index));
        }

        // ===== XÓA (gọi RPC Delete theo ID) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _client.Delete(new EmployeeFilter { EmployeeID = id });
            TempData["Message"] = $"Đã xóa nhân viên ID={id}.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
