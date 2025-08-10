using System;
using System.Runtime.Serialization;

namespace CustomizePlusPlus.Profiles.Exceptions;

internal class ProfileException : Exception
{
    public ProfileException()
    {
    }

    public ProfileException(string? message) : base(message)
    {
    }

    public ProfileException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected ProfileException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
