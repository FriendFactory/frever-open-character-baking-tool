using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace ExportCharacterTool
{
    [CreateAssetMenu(fileName = "ExportingContext", menuName = "Frever/ExportingContext", order = 1)]
    public sealed class ExportingContext: ScriptableObject
    {
        public CharacterSource CharacterSource;
        public bool UploadToServer = true;
        public bool CleanupExportFolderOnComplete = true;
        public bool SaveCharactersOutsideTheProject;
        public bool ActivateOnUploading;
        public long[] CharacterIds;
        public bool StopEndlessBakingProcess;
        [FormerlySerializedAs("StepNumber")] public ExportingStep ExportingProcessStep;
        public List<BundleData> BundleDatas;
        public long[] ExportedCharacterIds;
    }
    
    public enum CharacterSource
    {
        LoadedCharactersFromBackend,
        CustomList
    }

    public enum ExportingStep
    {
        ExportPrefab,
        BuildAssetBundleWithAnotherURP
    }
}