using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
