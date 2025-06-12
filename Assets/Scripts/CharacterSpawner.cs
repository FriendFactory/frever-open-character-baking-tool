using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bridge;
using Bridge.Models.ClientServer.Assets;
using Bridge.Models.ClientServer.StartPack.Metadata;
using Extensions;
using UMA;
using UMA.AssetBundles;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ExportCharacterTool
{
    internal sealed class CharacterSpawner
    {
        private const string SHOES_SLOT_NAME = "Shoes";
        private const string OUTFIT_SLOT_NAME = "Outfit";
        private static readonly string[] TRUNK_BODY_PART_SLOTS = { "Dress", "Shirts" };
        private static readonly string[] PELVIC_BODY_PART_SLOTS = { "Pants", "Skirts", "Dress" };
        
        private readonly IBridge _bridge;
        private UmaBundleFullInfo[] _globalBundles;
        private UmaBundleHelper _umaBundleHelper;
        private CharacterManagerConfig _characterManagerConfig;
        private readonly List<UmaBundleFullInfo> _umaBundles = new();
        private long? _buildingCharacterId;
        private MetadataStartPack _metadataStartPack;
        
        private static DynamicCharacterAvatar Prefab => Resources.Load<DynamicCharacterAvatar>("DynamicCharacterAvatar");

        public event Action<long> FailedToSpawn; 
        
        public CharacterSpawner(IBridge bridge)
        {
            _bridge = bridge;
        }

        public async Task Init()
        {
            var metaPackResp = await _bridge.GetMetadataStartPackAsync();
            if (!metaPackResp.IsSuccess)
            {
                throw new CharacterExportException($"Failed to load meta pack: {metaPackResp.ErrorMessage}");
            }

            _metadataStartPack = metaPackResp.Pack;
            _umaBundleHelper = new UmaBundleHelper(metaPackResp.Pack.UnityAssetTypes.ToArray());
            _umaBundleHelper.InitializeIndexer();
            _globalBundles = metaPackResp.Pack.GlobalUmaBundles.ToArray();
            _umaBundleHelper.AddBundlesToIndex(_globalBundles);
            _umaBundles.AddRange(_globalBundles);
            await FetchAssetBundle(_globalBundles);
            _characterManagerConfig = Resources.Load<CharacterManagerConfig>("CharacterManagerConfig");
            
            DynamicAssetLoader.Instance.remoteServerURL = $"file://{Application.persistentDataPath}/Cache/";
            AssetBundleManager.overrideBaseDownloadingURL += bundleName =>
            {
                var bundleModel = _umaBundles.First(x => x.Name == bundleName);
                var path = $"file://{Application.persistentDataPath}/Cache/{_bridge.Environment.ToString()}/UmaBundle/{bundleModel.Id}/{Utility.GetPlatformName()}/AssetBundle";
                path = _bridge.EncryptionEnabled ? $"{path}{_bridge.TargetExtension}" : path;
                return path;
            }; 
        }

        public async Task<SpawnOutput> SpawnCharacter(CharacterFullInfo model)
        {
            Debug.Log($"Start character spawning: {model.Id}");
            _buildingCharacterId = model.Id;
            DynamicCharacterAvatar avatar = null;
            try
            {
                Application.logMessageReceived += OnLogMessageReceived;
                var umaRecipe = model.UmaRecipe;
                var recipe = System.Text.Encoding.UTF8.GetString(umaRecipe.J);
                var genderGlobalBundles = _globalBundles.Where(x => x.GenderIds.Contains(model.GenderId));
                var bundlesToLoad = new List<UmaBundleFullInfo>(genderGlobalBundles);
                if (model.Wardrobes != null && model.Wardrobes.Any())
                {
                    var umaBundles = model.Wardrobes.SelectMany(GetBundles).ToArray();
                    bundlesToLoad.AddRange(umaBundles);
                    _umaBundleHelper.AddBundlesToIndex(umaBundles);
              
                    await FetchAssetBundle(umaBundles);
                }
            
                DynamicAssetLoader.Instance.LoadAssetBundles(bundlesToLoad.Select(x=>x.Name).ToArray());
                await WaitForBundlesLoaded();
            
                avatar = Object.Instantiate(Prefab);
                avatar.CharacterUpdated.AddAction(umaData =>
                {
                    var sm = avatar.GetComponentInChildren<SkinnedMeshRenderer>();
                    sm.shadowCastingMode = ShadowCastingMode.On;
                    sm.rootBone = avatar.transform.Find("Root").transform;
                });
                avatar.transform.position = Vector3.zero;
                avatar.transform.rotation = Quaternion.identity;
                avatar.waitForBundles = true;
                avatar.loadFileOnStart = false;
                avatar.ClearSlots();
            
                var unpackedRecipe = UMATextRecipe.PackedLoadDCS(avatar.context, recipe);
                avatar.ImportSettings(unpackedRecipe);
                avatar.RecipeUpdated.AddAction(OnRecipeUpdated);

                void OnRecipeUpdated(UMAData data)
                {
                    GetOverrideUnderwear(model.Wardrobes, out var overrideTop, out var overrideBot);
                    UpdateUnderwearStates(avatar, overrideTop, overrideBot);
                }
                await LoadWardrobesToRecipe(avatar, unpackedRecipe, model.Wardrobes);
                
                avatar.ImportSettings(unpackedRecipe);//force again to make sure it gets applied wardrobes
                
                await WaitWhileAvatarCreated(avatar);
                avatar.RecipeUpdated.RemoveAction(OnRecipeUpdated);
                avatar.gameObject.name = $"CharacterView {model.Id}";
                avatar.umaData.animator = avatar.GetComponent<Animator>();
                ValidateBlendShapes(model, avatar);
                Debug.Log($"Finished character spawning: {model.Id}");
                Application.logMessageReceived -= OnLogMessageReceived;
                _buildingCharacterId = null;
                return new SpawnOutput
                {
                    Success = true,
                    GameObject = avatar.gameObject,
                    HeelsHeight = GetHeelsHeight(avatar)
                };
            }
            catch (Exception e)
            {
                if (avatar != null)
                {
                    Object.Destroy(avatar.gameObject);
                }
                return new SpawnOutput
                {
                    Success = false,
                    ErrorMessage = $"Failed to spawn character: {e.Message}. Stacktrace: {e.StackTrace}"
                };
            }
        }

        public void Cleanup()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        {
            if (type is LogType.Error or LogType.Exception)
            {
                FailedToSpawn?.Invoke(_buildingCharacterId.Value);
            }
        }

        private static void ValidateBlendShapes(CharacterFullInfo model, DynamicCharacterAvatar avatar)
        {
            var blendShapes = avatar.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().sharedMesh.blendShapeCount;
            if (blendShapes == 0)
            {
                throw new CharacterExportException($"Character {model.Id} has missed blend shapes");
            }
        }

        private async Task FetchAssetBundle(IEnumerable<UmaBundleFullInfo> umaBundles)
        {
            foreach (var umaBundleModel in umaBundles)
            {
                var fetchResp = await _bridge.FetchMainAssetAsync(umaBundleModel);
                if (!fetchResp.IsSuccess)
                {
                    throw new CharacterExportException($"Failed to fetch global bundle {umaBundleModel.Name}: {fetchResp.ErrorMessage}");
                }

                if (_umaBundles.All(x => x.Name != umaBundleModel.Name))
                {
                    _umaBundles.Add(umaBundleModel);
                }
                
                if (umaBundleModel.DependentUmaBundles != null)
                {
                    await FetchAssetBundle(umaBundleModel.DependentUmaBundles);
                }
            }
        }
        
        private static async Task WaitForBundlesLoaded()
        {
            while (Waiting())
            {
                await Task.Delay(25);
            }

            //for some reason it twice on the client side
            while (Waiting())
            {
                await Task.Delay(25);
            }
            
            bool Waiting()
            {
                return AssetBundleManager.AreBundlesDownloading() || !DynamicAssetLoader.Instance.downloadingAssets.areDownloadedItemsReady;
            }
        }
        
        private async Task LoadWardrobesToRecipe(DynamicCharacterAvatar avatar, UMATextRecipe.DCSUniversalPackRecipe recipe, IEnumerable<WardrobeFullInfo> wardrobes)
        {
            if (wardrobes == null)
            {
                recipe.wardrobeSet.Clear();
                return;
            }
            var accessories = wardrobes
                .Where(x => x.UmaBundle.UmaAssets.FirstOrDefault(_ => _.SlotId != null && _.SlotId != 0) != null).ToArray();
            if (!accessories.Any())
            {
                recipe.wardrobeSet.Clear();
                return;
            }

            var umaBundles = accessories.SelectMany(GetBundles).ToList();
            await FetchAssetBundle(umaBundles);
            
            DynamicAssetLoader.Instance.LoadAssetBundles(umaBundles.Select(x=>x.Name).ToList());

            await WaitForBundlesLoaded();
            
            avatar.context.dynamicCharacterSystem.Refresh(false);

            recipe.wardrobeSet = new List<WardrobeSettings>();

            foreach (var item in accessories)
            {
                var asset = item.UmaBundle.UmaAssets.First(x => x.SlotId != null && x.SlotId != 0);
                var slot = asset.SlotName;
                if (!avatar.AvailableRecipes.TryGetValue(slot, out var slotRecipe))
                {
                    Debug.LogWarning($"No recipe for {slot} available");
                    continue;
                }
                var wardrobeRecipe = slotRecipe.Find(w => w.name == asset.Name);
                if (wardrobeRecipe == null) 
                {
                    Debug.LogWarning($"Not found {asset.Name} in slot {asset.SlotName}");
                    continue;
                }
                DestroyRecipeThumbnail(wardrobeRecipe);
                
                recipe.wardrobeSet.Add(new WardrobeSettings(wardrobeRecipe.wardrobeSlot, wardrobeRecipe.name));
            }
        }
        
        private static void DestroyRecipeThumbnail(UMATextRecipe recipe)
        {
            if (recipe.wardrobeRecipeThumbs == null) return;
            foreach (var thumb in recipe.wardrobeRecipeThumbs)
            {
                if (thumb.thumb == null) continue;
                Object.DestroyImmediate(thumb.thumb.texture, true);
                Object.DestroyImmediate(thumb.thumb, true);
            }

            recipe.wardrobeRecipeThumbs = null;
        }

        private static List<UmaBundleFullInfo> GetBundles(WardrobeFullInfo wardrobe)
        {
            var mainBundle = wardrobe.UmaBundle;
            var output = new List<UmaBundleFullInfo> {mainBundle};
            if (mainBundle.DependentUmaBundles == null || mainBundle.DependentUmaBundles.Count == 0)
                return output;

            for (var i = 0; i < mainBundle.DependentUmaBundles.Count; i++)
            {
                output.Add(mainBundle.DependentUmaBundles[i]);
            }

            return output;
        }
        
        private static async Task WaitWhileAvatarCreated(DynamicCharacterAvatar avatar, CancellationToken token = default)
        {
            var created = false;
            avatar.CharacterUpdated.AddListener(OnCharacterCreated);

            void OnCharacterCreated(UMAData data)
            {
                avatar.CharacterUpdated.RemoveListener(OnCharacterCreated);
                created = true;
            }

            while (!created && !token.IsCancellationRequested)
            {
                await Task.Delay(25, token);
            }
        }
        
        private float GetHeelsHeight(DynamicCharacterAvatar avatar)
        {
            var meshVertices = avatar.WardrobeRecipes.Values.Where(x=>x.wardrobeSlot == SHOES_SLOT_NAME)
                .Select(x=>x.GetCachedRecipe(avatar.context))
                .Where(x=>x is { slotDataList: { Length: > 0 } })
                .SelectMany(x=>x.slotDataList)
                .Where(x=>x != null && x.asset != null)
                .Select(x=>x.asset)
                .Where(x=>x.meshData != null)
                .Select(x=>x.meshData)
                .SelectMany(x=>x.vertices);
                                      
            return meshVertices.Any()? -meshVertices.Min(x => x.y) : 0;
        }
        
        private void GetOverrideUnderwear(IEnumerable<WardrobeFullInfo> wardrobes, out bool overrideTop, out bool overrideBot)
        {           
            overrideTop = false;
            overrideBot = false;
            if (wardrobes == null) return;

            foreach (var wardrobe in wardrobes)
            {
                if (!overrideTop)
                {
                    overrideTop = wardrobe.OverridesUpperUnderwear;
                }

                if (!overrideBot)
                {
                    overrideBot = wardrobe.OverridesLowerUnderwear;
                }
            }
        }
        
        public void UpdateUnderwearStates(DynamicCharacterAvatar avatar, bool overrideTop, bool overrideBottom)
        { 
            if (avatar.umaData.umaRecipe.slotDataList == null) return;
            
            var slotData = avatar.umaData.umaRecipe.slotDataList;
            var bodySlot = slotData.FirstOrDefault(x => x.slotName.Contains("body", StringComparison.InvariantCultureIgnoreCase));

            if (bodySlot == null) return;

            var overlaysList = bodySlot.GetOverlayList();
            if (overlaysList == null) return;

            OverlayData topUnderwearOverlay = null;
            var umaGenderRace = avatar.activeRace.name;
            var race = _metadataStartPack.GetGenderByUmaRaceName(umaGenderRace);
            var botUnderwearOverlay = overlaysList.Find(x => x.overlayName == race.LowerUnderwearOverlay);
            if (race.UpperUnderwearOverlay != null)
            {
                topUnderwearOverlay = overlaysList.Find(x => x.overlayName == race.UpperUnderwearOverlay);
            }
            
            if (topUnderwearOverlay != null)
            {
                if (overrideTop)
                {
                    overrideTop = AreTrunkWardrobesLoadedSuccessfully(avatar);
                }
                SetUnderwearState(topUnderwearOverlay, overrideTop);
            }

            if (botUnderwearOverlay != null)
            {
                if (overrideBottom)
                {
                    overrideBottom = ArePelvicWardrobesLoadedSuccessfully(avatar);
                }
                SetUnderwearState(botUnderwearOverlay, overrideBottom);
            }
            avatar.UpdateColors(true);
        }
        
        private static bool ArePelvicWardrobesLoadedSuccessfully(DynamicCharacterAvatar avatar)
        {
            if (avatar.WardrobeRecipes == null) return false;
            return PELVIC_BODY_PART_SLOTS.Append(OUTFIT_SLOT_NAME).Any(x => avatar.WardrobeRecipes.ContainsKey(x));
        }
        
        private static void SetUnderwearState(OverlayData underwearOverlay, bool hide)
        {
            var newColor = new Color32(255, 255, 255, (byte)(!hide ? 255 : 0));
            underwearOverlay.SetColor(0, newColor);
            underwearOverlay.SetColor(1, newColor);
            underwearOverlay.SetColor(2, newColor);
        } 
        
        private static bool AreTrunkWardrobesLoadedSuccessfully(DynamicCharacterAvatar avatar)
        {
            if (avatar.WardrobeRecipes == null) return false;
            return TRUNK_BODY_PART_SLOTS.Append(OUTFIT_SLOT_NAME).Any(x => avatar.WardrobeRecipes.ContainsKey(x));
        }
    }

    internal struct SpawnOutput
    {
        public GameObject GameObject;
        public float HeelsHeight;
        public bool Success;
        public string ErrorMessage;
    }
}