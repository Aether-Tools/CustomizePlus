using System;
using System.Runtime.Serialization;

namespace CustomizePlusPlus.Profiles.Exceptions;

internal class ActorNotFoundException : ProfileException
{
    public ActorNotFoundException()
    {
    }

    public ActorNotFoundException(string? message) : base(message)
    {
    }

    public ActorNotFoundException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    protected ActorNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
