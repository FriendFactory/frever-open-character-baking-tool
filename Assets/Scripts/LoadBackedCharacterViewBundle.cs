#if UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ExportCharacterTool
{
    public class LoadBackedCharacterViewBundle : MonoBehaviour
    {
        [SerializeField] private long _characterId;
        
        // Start is called before the first frame update
        void Start()
        {
            var filePath = Application.dataPath.Replace("Assets", string.Empty) + $"AssetBundles/{EditorUserBuildSettings.activeBuildTarget}/{_characterId}";
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Asset Bundle is not found: {filePath}");
            }
            
            AssetBundle assetBundle = AssetBundle.LoadFromFile(filePath);
            GameObject loadedAsset = assetBundle.LoadAsset<GameObject>($"CharacterView {_characterId}");
            Instantiate(loadedAsset).transform.position = Vector3.zero;
        }
        
    }
}
#endif
