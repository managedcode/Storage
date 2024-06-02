using System;

namespace ManagedCode.Storage.Core.Exceptions;

public class BadConfigurationException : Exception
{
    public BadConfigurationException()
    {
    }

    public BadConfigurationException(string message) : base(message)
    {
    }

    public BadConfigurationException(string message, Exception inner) : base(message, inner)
    {
    }
}