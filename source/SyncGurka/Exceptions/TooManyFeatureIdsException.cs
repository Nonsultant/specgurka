namespace SpecGurka.Exceptions;

[Serializable]
public class TooManyFeatureIdsException : Exception
{
    public TooManyFeatureIdsException() { }

    public TooManyFeatureIdsException(string message)
        : base(message) { }

    public TooManyFeatureIdsException(string message, Exception inner)
        : base(message, inner) { }
}