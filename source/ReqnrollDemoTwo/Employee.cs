namespace ReqnrollDemoTwo;

public class Employee(string name, Role role)
{
    public Guid Id { get;} = Guid.NewGuid();
    public string Name { get;} = name;
    public Role Role { get; set; } = role;
}

public enum Role
{
    Manager,
    ForkliftOperator,
    Janitor
}
