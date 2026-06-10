using System.Collections.Generic;
using System.Linq;

namespace GrpcServiceDemo
{
    public static class EmployeeStore
    {
        private static readonly List<Employee> _employees = new List<Employee>
        {
            new Employee { EmployeeID = 1, FirstName = "Nancy",  LastName = "Davolio"   },
            new Employee { EmployeeID = 2, FirstName = "Andrew", LastName = "Fuller"    },
            new Employee { EmployeeID = 3, FirstName = "Janet",  LastName = "Leverling" },
        };

        //lock để an toàn khi nhiều request truy cập cùng lúc (thread-safe)
        private static readonly object _lock = new object();

        public static List<Employee> GetAll()
        {
            lock (_lock)
            {
                return _employees.Select(e => e.Clone()).ToList();
            }
        }

        public static Employee? Find(int id)
        {
            lock (_lock)
            {
                var found = _employees.FirstOrDefault(e => e.EmployeeID == id);
                return found?.Clone();
            }
        }

        public static void Add(Employee employee)
        {
            lock (_lock)
            {
                var clone = employee.Clone();
                if (clone.EmployeeID == 0)
                    clone.EmployeeID = _employees.Count == 0 ? 1 : _employees.Max(e => e.EmployeeID) + 1;
                _employees.Add(clone);
            }
        }

        public static bool Update(Employee employee)
        {
            lock (_lock)
            {
                var existing = _employees.FirstOrDefault(e => e.EmployeeID == employee.EmployeeID);
                if (existing == null) return false;
                existing.FirstName = employee.FirstName;
                existing.LastName = employee.LastName;
                return true;
            }
        }

        public static bool Remove(int id)
        {
            lock (_lock)
            {
                var existing = _employees.FirstOrDefault(e => e.EmployeeID == id);
                if (existing == null) return false;
                _employees.Remove(existing);
                return true;
            }
        }
    }
}
