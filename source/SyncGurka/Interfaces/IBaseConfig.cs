using Microsoft.Extensions.Configuration;

namespace SpecGurka.Interfaces;

public interface IBaseConfig
{
    public string BaseUrl { get; }
    public string AuthToken { get; }
}