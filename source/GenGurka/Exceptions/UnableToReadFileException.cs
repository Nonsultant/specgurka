namespace SpecGurka.GenGurka.Exceptions;

[Serializable]
public class UnableToReadFileException : Exception
{
    public UnableToReadFileException() { }

    public UnableToReadFileException(string message)
        : base(message) { }

    public UnableToReadFileException(string message, Exception inner)
        : base(message, inner) { }
}
