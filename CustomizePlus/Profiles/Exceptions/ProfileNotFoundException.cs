using System;
using System.Runtime.Serialization;

namespace CustomizePlus.Profiles.Exceptions;
internal class ProfileNotFoundException : ProfileException
{
    public ProfileNotFoundException()
    {
    }

    public ProfileNotFoundException(string? message) : base(message)
    {
    }

    public ProfileNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected ProfileNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
