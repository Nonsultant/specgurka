namespace ReqnrollDemoTwo;

public class Sections(string name)
{
    public string Name { get; set; } = name;
    public List<Employee> Employees { get; } = [];

    public void AddEmployee(Employee employee)
    {
        if (Employees.Any(e => e.Id == employee.Id))
        {
            throw new InvalidOperationException("Employee already exists in the section");
        }

        Employees.Add(employee);
    }

    public void RemoveEmployee(Guid employeeId)
    {
        var employee = Employees.FirstOrDefault(e => e.Id == employeeId);
        if (employee == null)
        {
            throw new InvalidOperationException("Employee does not exist in the section");
        }

        Employees.Remove(employee);
    }
}
