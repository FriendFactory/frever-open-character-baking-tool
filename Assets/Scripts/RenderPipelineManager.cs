#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExportCharacterTool
{
    public static class RenderPipelineManager
    {
        public static void SetShadowsCasting(bool isOn)
        {
            var urpAssetPostFix = isOn ? Constants.AB_PATH_SHADOWS_ON_PREFIX : Constants.AB_PATH_SHADOWS_OFF_PREFIX;
            var urpAsset = Resources.Load<RenderPipelineAsset>($"URP/PipelineAsset_Low {urpAssetPostFix}");
            ChangeActiveRenderingPipeline(urpAsset);
        }
        
        private static void ChangeActiveRenderingPipeline(RenderPipelineAsset newPipelineAsset)
        {
            if (newPipelineAsset != null)
            {
                // Change the active render pipeline
                GraphicsSettings.renderPipelineAsset = newPipelineAsset;

                // Save the project settings to persist the change
                EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError("No Render Pipeline Asset selected!");
            }
        }
    }
}
#endif