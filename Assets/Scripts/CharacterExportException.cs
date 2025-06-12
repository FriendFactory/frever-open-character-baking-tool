using System;

namespace ExportCharacterTool
{
    public sealed class CharacterExportException: Exception
    {
        public CharacterExportException(string reason): base($"Failed to export character. Reason: {reason}")
        {
        }
    }
}