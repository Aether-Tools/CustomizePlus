using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizePlus.Api.Enums;

/// <summary>
/// Error codes returned by some API methods
/// </summary>
public enum ErrorCode
{
    Success = 0,

    /// <summary>
    /// Returned when invalid character address was provided
    /// </summary>
    InvalidCharacter = 1,

    /// <summary>
    /// Returned if IPCCharacterProfile could not be deserialized or deserialized into an empty object
    /// </summary>
    CorruptedProfile = 2,

    /// <summary>
    /// Provided character does not have active profiles, provided profile id is invalid or provided profile id is not valid for use in current function
    /// </summary>
    ProfileNotFound = 3,

    /// <summary>
    /// General error telling that one of the provided arguments were invalid.
    /// </summary>
    InvalidArgument = 4,

    UnknownError = 255
}
