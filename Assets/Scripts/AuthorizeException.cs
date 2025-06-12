using System;

namespace ExportCharacterTool
{
    public sealed class AuthorizeException: Exception
    {
        public AuthorizeException(string message) : base($"Failed to authorize. Reason: {message}")
        {
        }
    }
}