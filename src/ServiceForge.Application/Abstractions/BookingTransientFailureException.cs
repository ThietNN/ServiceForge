namespace ServiceForge.Application.Abstractions;

public sealed class BookingTransientFailureException(string message, Exception? innerException = null)
    : Exception(message, innerException);
