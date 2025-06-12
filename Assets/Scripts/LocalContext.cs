using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ExportCharacterTool
{
    /// <summary>
    /// Local context save on machine
    /// </summary>
    [CreateAssetMenu(fileName = "LocalMachineContext", menuName = "Frever/LocalContext", order = 1)]
    public sealed class LocalContext: ScriptableObject
    {
        private string FilePath => Path.Combine(Application.persistentDataPath, "LocalContext.txt");
        
        [SerializeField] private string _bakingMachineId;
        public string BakingMachineId
        {
            get
            {
                if (_bakingMachineId == null)
                {
                    Load();
                }
                return _bakingMachineId;
            }
        }

        public void Save()
        {
            var json = JsonConvert.SerializeObject(new SaveModel
            {
                BakingMachineId = this.BakingMachineId
            });
            File.WriteAllText(FilePath, json);
        }

        public void Load()
        {
            if (!File.Exists(FilePath)) return;
           
            try
            {
                var json = File.ReadAllText(FilePath);
                var model = JsonConvert.DeserializeObject<SaveModel>(json);
                _bakingMachineId = model.BakingMachineId;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to read local context. Reason: {e.Message}");
            }
        }
        
        private sealed class SaveModel
        {
            public string BakingMachineId;
        }
    }
}