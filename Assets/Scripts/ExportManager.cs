#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bridge;
using Bridge.Models.ClientServer.Assets;
using Bridge.Results;
using Newtonsoft.Json;
using SharedAssetBundleScripts.Runtime.Character;
using UMA.PowerTools;
using UnityEditor;
using UnityEngine;

namespace ExportCharacterTool
{
    public sealed class ExportManager : MonoBehaviour
    {
        private const int CHARACTERS_BAKE_PAGE_SIZE = 8;
        
        [SerializeField] private ExportingContext _context;
        [SerializeField] private LocalContext _localContext;
        [SerializeField] private ShaderList _missedShadersStorage;
     
        private CharacterSpawner _characterSpawner;
        private UmaBundleHelper _umaBundleHelper;
        private IBridge _bridge;
        private ViewUploader _viewUploader;
        private IgnoredCharactersManager _ignoredCharactersManager;
        
        private long[] TargetCharacterIds { get; set; }
        private CharacterFullInfo[] TargetCharacters { get; set; }
        private CharacterFullInfo[] ExportedSuccessfully { get; set; }
        private readonly Dictionary<long, float> _heelsHeight = new();

        
        private void Awake()
        {
            Application.runInBackground = true;
            _bridge = new ServerBridge();
            _characterSpawner = new CharacterSpawner(_bridge);
            _ignoredCharactersManager = new IgnoredCharactersManager();
            _characterSpawner.FailedToSpawn += OnFailedToSpawn;
            _viewUploader = new ViewUploader(_bridge);
            _localContext.Load();
        }
        
        private async void Start()
        {
            await _bridge.LoginToLastSavedUserAsync();
            await _characterSpawner.Init();
            var characters = await GetCharacters();
            if (characters.Length == 0)
            {
                EditorApplication.ExitPlaymode();
                if (_context.CharacterSource == CharacterSource.LoadedCharactersFromBackend)
                {
                    Debug.Log("No more characters to build. Closing Unity Editor");
                    EditorUtility.DisplayDialog("Nothing to bake", "No more characters to build. Closing Unity Editor",
                        "ok");
                    await Task.Delay(2000);
                    EditorApplication.Exit(0);
                }
            
                return;
            }
          
            ExecuteCharactersExporting(characters);
        }

        private void OnDestroy()
        {
            _characterSpawner.Cleanup();
        }

        private async void ExecuteCharactersExporting(CharacterFullInfo[] characters)
        {
            TargetCharacterIds = characters.Select(x=>x.Id).ToArray();
            ExportedSuccessfully = await ExportCharacters(characters);
            if (_context.ExportingProcessStep == ExportingStep.ExportPrefab)
            {
                SetAssetBundleNameForPrefab(ExportedSuccessfully.Select(x=>x.Id));
                AddBakedRootComponent(ExportedSuccessfully.Select(x=>x.Id));
            }
            
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.ExitPlaymode();
        }

        private async Task<CharacterFullInfo[]> GetCharacters()
        {
            if (_context.ExportingProcessStep == ExportingStep.BuildAssetBundleWithAnotherURP)
            {
                var characterResp = await _bridge.GetCharactersAdminAccessLevel(_context.ExportedCharacterIds);
                if (!characterResp.IsSuccess)
                {
                    throw new CharacterExportException($"Failed to load characters {JsonConvert.SerializeObject(_context.ExportedCharacterIds)} model");
                }

                return FilterFromURPBakedWithShadowsAndModifiedSinceBakedWithShadows(characterResp.Models);
            }
            
            if (_context.CharacterSource == CharacterSource.CustomList)
            {
                var characterResp = await _bridge.GetCharactersAdminAccessLevel(_context.CharacterIds);
                if (!characterResp.IsSuccess)
                {
                    throw new CharacterExportException($"Failed to load characters {JsonConvert.SerializeObject(_context.CharacterIds)} model");
                }
                return characterResp.Models;
            }

            long[] validCharacters;
            ArrayResult<CharacterFullInfo> resp;
            do
            {         
                resp = await _bridge.GetNonBakedCharacters(CHARACTERS_BAKE_PAGE_SIZE);
                if (!resp.IsSuccess)
                {
                    throw new CharacterExportException("Failed to load non-baked characters");
                }

                validCharacters = _ignoredCharactersManager.FilterOutIgnored(resp.Models.Select(x=>x.Id));
                if (validCharacters.Length == 0)
                {
                    await Task.Delay(5000);
                }
            } while (validCharacters.Length == 0);
           
            return resp.Models.Where(x=> validCharacters.Contains(x.Id)).ToArray();

            CharacterFullInfo[] FilterFromURPBakedWithShadowsAndModifiedSinceBakedWithShadows(CharacterFullInfo[] source)
            {
                return source.Where(character => _context.BundleDatas.Any(bundleData =>
                    bundleData.CharacterId == character.Id &&
                    Guid.Parse(bundleData.CharacterVersion) == character.Version)).ToArray();
            }
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;
            
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            if (_context.ExportingProcessStep == ExportingStep.ExportPrefab)
            {
                _context.ExportedCharacterIds = ExportedSuccessfully.Select(x => x.Id).ToArray();
                var bundlesData = new List<BundleData>();
                foreach (var c in ExportedSuccessfully)
                {
                    bundlesData.Add(new BundleData
                    {
                        CharacterId = c.Id,
                        CharacterVersion = c.Version.ToString(),
                        Path = GetPrefabPath(c.Id)
                    });
                }

                _context.BundleDatas = bundlesData;
            }
            else
            {
                _context.BundleDatas = _context.BundleDatas
                    .Where(x => ExportedSuccessfully.Any(c => c.Id == x.CharacterId)).ToList();
            }
       

            var targetPlatforms = new[]{ BuildTarget.iOS, BuildTarget.Android };
            targetPlatforms = targetPlatforms.OrderBy(x => x == EditorUserBuildSettings.activeBuildTarget).ToArray();
            
            foreach (var character in ExportedSuccessfully)
            {
                if (GetPrefab(character.Id).GetComponent<BakedViewRoot>() == null)
                {
                    throw new CharacterExportException($"Prefab {character.Id} has missed root component");
                }
            }

            if (_context.ExportingProcessStep == ExportingStep.ExportPrefab)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
                BuildAssetBundleManager.ClearAssetBundleFolder();
                BuildAssetBundleManager.BuildAssetBundles(_context.BundleDatas, Constants.AB_PATH_SHADOWS_ON_PREFIX, targetPlatforms);
                RenderPipelineManager.SetShadowsCasting(false);
                _context.ExportingProcessStep = ExportingStep.BuildAssetBundleWithAnotherURP;
                EditorApplication.EnterPlaymode();
            }
            else
            {
                BuildAssetBundleManager.BuildAssetBundles(_context.BundleDatas, Constants.AB_PATH_SHADOWS_OFF_PREFIX, BuildTarget.iOS);
                _context.ExportingProcessStep = ExportingStep.ExportPrefab;
                RenderPipelineManager.SetShadowsCasting(true);
                if (_context.UploadToServer)
                {
                    var args = new UploadArgs
                    {
                        BakedViewsInfo = _context.BundleDatas.Select(x => new UploadArgs.BakedViewInfo()
                        {
                            CharacterId = x.CharacterId,
                            Version = Guid.Parse(x.CharacterVersion)
                        }).ToArray(),
                        ActivateOnUpload = _context.ActivateOnUploading,
                        OnComplete = OnCharactersUploaded,
                        HeelsHeight = _heelsHeight,
                        BakingMachineId = _localContext.BakingMachineId
                    };
                    _viewUploader.UploadCharacters(args);
                }
            
                if (_context.SaveCharactersOutsideTheProject)
                {
                    CharacterAssetRelocator.MoveCharactersOutsideTheProject(_bridge.Environment.ToString());
                }

                if (_context.CleanupExportFolderOnComplete)
                {
                    Directory.Delete("Assets/ExportedCharacters", true);
                    AssetDatabase.Refresh();
                }
                
                PlayerSettings.colorSpace = ColorSpace.Gamma;
            }
        }

        private void OnCharactersUploaded()
        {
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            if (_context.CharacterSource == CharacterSource.CustomList || _context.StopEndlessBakingProcess)
            {
                return;
            }
            EditorApplication.EnterPlaymode();
        }

        private async Task<CharacterFullInfo[]> ExportCharacters(CharacterFullInfo[] characters)
        {
            var exported = new List<CharacterFullInfo>();
            TargetCharacterIds = characters.Select(x=>x.Id).ToArray();
            
            TargetCharacters = characters;
                
            var views = new List<GameObject>(characters.Length);
            _heelsHeight.Clear();
            foreach (var model in characters)
            {
                var spawnData = await _characterSpawner.SpawnCharacter(model);
                if (!spawnData.Success)
                {
                    Debug.LogError(spawnData.ErrorMessage);
                    continue;
                }
                PrefabHelper.ClearUmaPrefab(spawnData.GameObject);
                string missedShaderName = null;
                string matName = null;
                if (!PrefabHelper.LinkShadersFromAssetDatabase(spawnData.GameObject, ref missedShaderName, ref matName))
                {
                    Debug.LogError($"Can't export character {model.Id}. Reason: missed shader");
                    var wardrobeIds = model.Wardrobes == null ? Array.Empty<long>() : model.Wardrobes.Select(x => x.Id).ToArray();
                    _missedShadersStorage.Add(missedShaderName, matName, model.Id, wardrobeIds);
                    var wardrobes = SelectWardrobesThatUsesTheShader(model, missedShaderName);
                    foreach (var wardrobe in wardrobes)
                    {
                        InvalidateWardrobe(wardrobe.Id, $"Missed shader for baking: {missedShaderName}");
                    }
                    continue;
                }
                views.Add(spawnData.GameObject);
                _heelsHeight[model.Id] = spawnData.HeelsHeight;
                exported.Add(model);
            }
            
            if (_context.ExportingProcessStep == ExportingStep.ExportPrefab)
            {
                UMASaveCharacters.SaveCharacterPrefabsMenuItem(views.ToArray());
            }
            return exported.ToArray();
        }

        private static IEnumerable<WardrobeFullInfo> SelectWardrobesThatUsesTheShader(CharacterFullInfo model, string missedShaderName)
        {
            return model.Wardrobes.Where(wardrobe => 
                ContainsShader(wardrobe.UmaBundle)
                || (wardrobe.UmaBundle.DependentUmaBundles.Any() && wardrobe.UmaBundle.DependentUmaBundles.Any(ContainsShader)));

            bool ContainsShader(UmaBundleFullInfo bundle)
            {
                return bundle.UmaAssets.Where(umaAssetInfo => umaAssetInfo.UmaAssetFiles != null)
                    .SelectMany(umaAssetInfo => umaAssetInfo.UmaAssetFiles)
                    .Any(umaAssetFileInfo => missedShaderName.EndsWith(umaAssetFileInfo.Name, true, CultureInfo.InvariantCulture));
            }
        }

        private async void InvalidateWardrobe(long wardrobeId, string reason)
        {
            await _bridge.InvalidateWardrobe(wardrobeId, reason);
        }
        
        private static void SetAssetBundleNameForPrefab(IEnumerable<long> charactersId)
        {
            foreach (var id in charactersId)
            {
                var prefab = GetPrefab(id);
                var prefabPath = GetPrefabPath(id);

                if (prefab == null)
                {
                    Debug.LogError("Prefab not found at path: " + prefabPath);
                    return;
                }
            
                var assetImporter = AssetImporter.GetAtPath(prefabPath);

                if (assetImporter == null)
                {
                    Debug.LogError("Could not get AssetImporter for prefab at path: " + prefabPath);
                    return;
                }

                assetImporter.assetBundleName = id.ToString();
            }
            AssetDatabase.SaveAssets();
        }

        private static void AddBakedRootComponent(IEnumerable<long> ids)
        {
            var method = typeof(BakedViewRoot).GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var id in ids)
            {
                var prefab = GetPrefab(id);
                var bakedViewRoot = prefab.AddComponent<BakedViewRoot>();
                method.Invoke(bakedViewRoot, Array.Empty<object>());   
            }
            AssetDatabase.SaveAssets();
        }
        
        private static GameObject GetPrefab(long characterId)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabPath(characterId));
            if (prefab == null)
            {
                throw new CharacterExportException($"Failed to export {characterId}. The prefab is not found");
            }

            return prefab;
        }
        
        private async void OnFailedToSpawn(long id)
        {
            _ignoredCharactersManager.IgnoreCharacterForAWhile(id);
            Debug.LogError($"Failed to spawn character: {id}. Unity will be closed in 5 seconds");
            await Task.Delay(5000);
            EditorApplication.Exit(0);
        }

        private static string GetPrefabPath(long characterId)
        {
            return $"Assets/ExportedCharacters/{characterId}/CharacterView {characterId}.prefab";
        }
    }
}

#endif
