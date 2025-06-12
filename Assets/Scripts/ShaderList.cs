using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace ExportCharacterTool
{
    [CreateAssetMenu(fileName = "Missed Shaders", menuName = "Frever/Create Shader List")]
    internal sealed class ShaderList : ScriptableObject
    {
        [SerializeField] private List<ShaderInfo> _missedShadersInfo = new();

        private static string FilePath => Application.persistentDataPath + "/MissedShaders.txt";
        
        private void OnEnable()
        {
            if (_missedShadersInfo.Count != 0) return;

            if (!File.Exists(FilePath)) return;
            
            var json = File.ReadAllText(FilePath);
            try
            {
                var missedShaders = JsonConvert.DeserializeObject<List<ShaderInfo>>(json);
                _missedShadersInfo.AddRange(missedShaders);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to load missed shaders data");
            }
        }

        public void Add(string shaderFullName, string materialName, long character, long[] characterWardrobes)
        {
            var existedInfo = _missedShadersInfo.FirstOrDefault(x => x.Name.Equals(shaderFullName));
            if (existedInfo == null)
            {
                _missedShadersInfo.Add(new ShaderInfo
                {
                    Name= shaderFullName,
                    MaterialName = materialName,
                    CharacterToWardrobes = new Dictionary<long, long[]>
                    {
                        {character, characterWardrobes}
                    },
                    DateTime = DateTime.UtcNow
                });
            }
            else
            {
                existedInfo.CharacterToWardrobes ??= new Dictionary<long, long[]>();
                existedInfo.CharacterToWardrobes[character] = characterWardrobes;
            }
            
            Save();
        }

        private void Save()
        {
            var json = JsonConvert.SerializeObject(_missedShadersInfo);
            File.WriteAllText(FilePath, json);
        }
        
        [Serializable]
        public sealed class ShaderInfo
        {
            public string Name;
            public string MaterialName;
            public Dictionary<long, long[]> CharacterToWardrobes;
            public DateTime DateTime;
        }
    }
}