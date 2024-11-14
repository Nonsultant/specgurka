namespace SpecGurka.Interfaces;

public interface IOwnerProjectConfig : IBaseConfig
{
    public string Owner { get; }
    public string Project { get; }
}
