namespace ReqnrollDemoTwo;

public class Warehouse(string name, string category)
{
    private string Name { get; set; } = name;
    public string Category { get; set; } = category;
    public List<Sections> Sections { get; } = [];

    public void AddSection(Sections section)
    {
        if (Sections.Any(s => s.Name == section.Name))
        {
            throw new InvalidOperationException("Section already exists");
        }

        if (string.IsNullOrEmpty(section.Name))
        {
            throw new InvalidOperationException("Section name cannot be empty");
        }

        Sections.Add(section);
    }

    public void RemoveSection(string sectionName)
    {
        var section = Sections.FirstOrDefault(s => s.Name == sectionName);
        if (section == null)
        {
            throw new InvalidOperationException("Section does not exist");
        }

        Sections.Remove(section);
    }

    public void RenameSection(string oldName, string newName)
    {
        var section = Sections.FirstOrDefault(s => s.Name == oldName);
        if (section == null)
        {
            throw new InvalidOperationException("Section does not exist");
        }

        if (Sections.Any(s => s.Name == newName))
        {
            throw new InvalidOperationException("A section with the new name already exists");
        }

        section.Name = newName;
    }

    public void ChangeCategory(string newCategoryName)
    {
        if (string.IsNullOrEmpty(newCategoryName))
        {
            throw new InvalidOperationException("Category name cannot be empty");
        }

        if(Category == newCategoryName)
        {
            throw new InvalidOperationException("Category name cannot be the same as the current Category name");
        }

        Category = newCategoryName;
    }
}