#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ExportCharacterTool
{
    public static class PrefabHelper
    {
        public static void ClearUmaPrefab(GameObject target)
        {
            var lights = GameObject.Find("LightPositionsParent(Clone)");
            Object.Destroy(lights);
            target.name = target.name.Replace("/", string.Empty);
            Selection.activeObject = target;
        
            RemoveLinksRecursively(target);
        }
        
        public static bool LinkShadersFromAssetDatabase(GameObject target, ref string shaderName, ref string materialName)
        {
            var renderer = target.GetComponentInChildren<SkinnedMeshRenderer>();
            var materials = renderer.sharedMaterials;
            foreach (var mat in materials)
            {
                var name = mat.shader.name;
                var newShader = Shader.Find(name);
                if (newShader == null)
                {
                    Debug.LogError($"Can't find shader: {name}");
                    shaderName = name;
                    materialName = mat.name;
                    return false;
                }

                var renderQueue = mat.renderQueue;
                mat.shader = newShader;
                mat.renderQueue = renderQueue;
            }

            renderer.sharedMaterials = materials;
            return true;
        }
    
        private static void RemoveLinksRecursively(GameObject target)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
            foreach (Transform child in target.transform)
            {
                RemoveLinksRecursively(child.gameObject);
            }
        }
    }
}
#endif