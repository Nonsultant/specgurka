namespace ReqnrollDemo;

public class Company(string name, string ceo)
{
    private string Name { get; set; } = name;
    public string Ceo { get; set; } = ceo;
    public List<Department> Departments { get; } = [];

    public void AddDepartment(Department department)
    {
        if (Departments.Any(d => d.Name == department.Name))
        {
            throw new InvalidOperationException("Department already exists");
        }

        if (string.IsNullOrEmpty(department.Name))
        {
            throw new InvalidOperationException("Department name cannot be empty");
        }

        Departments.Add(department);
    }

    public void RemoveDepartment(string departmentName)
    {
        var department = Departments.FirstOrDefault(d => d.Name == departmentName);
        if (department == null)
        {
            throw new InvalidOperationException("Department does not exist");
        }

        Departments.Remove(department);
    }

    public void RenameDepartment(string oldName, string newName)
    {
        var department = Departments.FirstOrDefault(d => d.Name == oldName);
        if (department == null)
        {
            throw new InvalidOperationException("Department does not exist");
        }

        if (Departments.Any(d => d.Name == newName))
        {
            throw new InvalidOperationException("A department with the new name already exists");
        }

        department.Name = newName;
    }

    public void ChangeCeo(string newCeoName)
    {
        if (string.IsNullOrEmpty(newCeoName))
        {
            throw new InvalidOperationException("CEO name cannot be empty");
        }

        if(Ceo == newCeoName)
        {
            throw new InvalidOperationException("CEO name cannot be the same as the current CEO");
        }

        Ceo = newCeoName;
    }
}