namespace SpecGurka.GenGurka.Exceptions;

[Serializable]
public class WrongFeatureConfigurationException : Exception
{
    public WrongFeatureConfigurationException() { }

    public WrongFeatureConfigurationException(string message)
        : base(message) { }

    public WrongFeatureConfigurationException(string message, Exception inner)
        : base(message, inner) { }
}
