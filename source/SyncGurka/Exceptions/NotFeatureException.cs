namespace SpecGurka.Exceptions;

[Serializable]
public class NotFeatureException : Exception
{
    public NotFeatureException() { }

    public NotFeatureException(string message)
        : base(message) { }

    public NotFeatureException(string message, Exception inner)
        : base(message, inner) { }
}
