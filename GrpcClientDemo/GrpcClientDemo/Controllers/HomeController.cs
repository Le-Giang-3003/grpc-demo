using GrpcClientDemo.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace GrpcClientDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly EmployeeCRUD.EmployeeCRUDClient _client;

        public HomeController(ILogger<HomeController> logger, EmployeeCRUD.EmployeeCRUDClient client)
        {
            _logger = logger;
            _client = client;
        }

        public IActionResult Index()
        {
            var employees = _client.SelectAll(new Empty());
            return View(employees);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new Employee());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee employee)
        {
            _client.Insert(employee);
            TempData["Message"] = $"Đã thêm nhân viên \"{employee.FirstName} {employee.LastName}\".";
            return RedirectToAction(nameof(Index));
        }

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
                TempData["Error"] = $"Không tìm thấy nhân viên ID={id}.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Employee employee)
        {
            _client.Update(employee);
            TempData["Message"] = $"Đã cập nhật nhân viên ID={employee.EmployeeID}.";
            return RedirectToAction(nameof(Index));
        }

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
