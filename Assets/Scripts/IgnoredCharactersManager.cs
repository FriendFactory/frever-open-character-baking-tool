using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace ExportCharacterTool
{
    internal sealed class IgnoredCharactersManager
    {
        private const int DAYS_TO_IGNORE_FAILED_CHARACTER = 1;
        private readonly Dictionary<long, DateTime> _ignoredCharacters;
        
        private string IgnoredDataFilePath => Path.Combine(Application.persistentDataPath, "IgnoredCharactersData.txt");
        
        public IgnoredCharactersManager()
        {
            if (File.Exists(IgnoredDataFilePath))
            {
                var json = File.ReadAllText(IgnoredDataFilePath);
                try
                {
                    _ignoredCharacters = JsonConvert.DeserializeObject<Dictionary<long, DateTime>>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to read ignored characters data: {e.Message} {e.StackTrace}");
                    _ignoredCharacters = new Dictionary<long, DateTime>();
                }
            }
            else
            {
                _ignoredCharacters = new Dictionary<long, DateTime>();
            }
        }
        
        public void IgnoreCharacterForAWhile(long characterId)
        {
            _ignoredCharacters[characterId] = DateTime.UtcNow;
            var restoreChars = _ignoredCharacters
                .Where(x => x.Value + TimeSpan.FromDays(DAYS_TO_IGNORE_FAILED_CHARACTER) < DateTime.UtcNow).Select(x=>x.Key);
            foreach (var id in restoreChars)
            {
                _ignoredCharacters.Remove(id);
            }
            var json = JsonConvert.SerializeObject(_ignoredCharacters);
            File.WriteAllText(IgnoredDataFilePath, json);
        }

        public long[] FilterOutIgnored(IEnumerable<long> source)
        {
            return source.Where(x =>
                !_ignoredCharacters.ContainsKey(x) ||
                _ignoredCharacters[x] - DateTime.UtcNow > TimeSpan.FromDays(DAYS_TO_IGNORE_FAILED_CHARACTER)).ToArray();
        }
    }
}