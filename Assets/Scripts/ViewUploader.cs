using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bridge;
using Bridge.AssetManagerServer;
using Bridge.Models.AdminService;
using Bridge.Models.AsseManager;
using Bridge.Models.Common.Files;
using Bridge.Results;
using UnityEngine;
using FileInfo = Bridge.Models.Common.Files.FileInfo;

namespace ExportCharacterTool
{
    public sealed class ViewUploader
    {
        private readonly IBridge _bridge;

        public ViewUploader(IBridge bridge)
        {
            _bridge = bridge;
        }

        public async void UploadCharacters(UploadArgs args)
        {
            var bakedViewInfos = args.BakedViewsInfo;
            for (var i = 0; i < bakedViewInfos.Count; i++)
            {
                var bv = bakedViewInfos[i];
                var model = new CharacterBakedViewDto()
                {
                    ReadinessId = 2,
                    IsValid = args.ActivateOnUpload,
                    CharacterId = bv.CharacterId,
                    CharacterVersion = bv.Version,
                    HeelsHeight = args.HeelsHeight[bv.CharacterId],
                    BakingMachineAgentName = args.BakingMachineId,
                    Files = new List<FileInfo>()
                };
                var bundlesPath = Application.dataPath.Replace("Assets", "AssetBundles");
                var iosShadowsOnPath = Path.Combine(bundlesPath, $"iOS/{Constants.AB_PATH_SHADOWS_ON_PREFIX}/{bv.CharacterId}");
                var iosShadowsOffPath = Path.Combine(bundlesPath, $"iOS/{Constants.AB_PATH_SHADOWS_OFF_PREFIX}/{bv.CharacterId}");
                var androidPath = Path.Combine(bundlesPath, $"Android/{Constants.AB_PATH_SHADOWS_ON_PREFIX}/{bv.CharacterId}");
                model.Files.Add(new FileInfo(iosShadowsOnPath, FileType.MainFile, Platform.iOS)
                {
                    Extension = FileExtension.Empty
                });
                model.Files.Add(new FileInfo(iosShadowsOffPath, FileType.MainFile, Platform.iOS)
                {
                    Extension = FileExtension.Empty,
                    Tags = new []{ FileInfoTags.NO_SHADOWS }
                });
                model.Files.Add(new FileInfo(androidPath, FileType.MainFile, Platform.Android)
                {
                    Extension = FileExtension.Empty
                });

                var existed = await GetCharacterBakedViewModelFromServer(bv.CharacterId, null);
                Result uploadResp;
                if (existed != null)
                {
                    uploadResp = await _bridge.UpdateBakedView(existed.Id, model);
                }
                else
                {
                    uploadResp = await _bridge.UploadBakedView(model);
                }

                if (uploadResp.IsError)
                {
                    Debug.LogError($"Failed to upload baked view for character {bv.CharacterId}. Reason: {uploadResp.ErrorMessage}");
                }
            }

            Debug.Log($"Completed uploading");
            args.OnComplete?.Invoke();
        }

        private async Task<CharacterBakedView> GetCharacterBakedViewModelFromServer(long characterId, long? outfitId)
        {
            var query = new Query<CharacterBakedView>();
            var filterByCharacterId = new FilterSetup
            {
                FilterType = FilterType.Equals,
                FieldName = nameof(CharacterBakedView.CharacterId),
                FilterValue = characterId
            };
            //todo: uncomment when we start handle character with outfit view
            // var filterByOutfitId = new FilterSetup()
            // {
            //     FilterType = FilterType.Equals,
            //     FieldName = nameof(CharacterBakedView.OutfitId),
            //     FilterValue = outfitId
            // };
            query.SetFilters(filterByCharacterId);
            var resp = await _bridge.GetAsync(query);
            if (resp.IsError) throw new Exception($"Failed to check if the model already existed: {resp.ErrorMessage}");

            if (resp.Models.Length > 1)
                throw new Exception(
                    $"Failed to check if the model already existed. For some reason backend returned {resp.Models.Length} instead of 1 or 0");

            return resp.Models.FirstOrDefault();
        }
    }

    public struct UploadArgs
    {
        public IReadOnlyList<BakedViewInfo> BakedViewsInfo;
        public bool ActivateOnUpload;
        public Dictionary<long, float> HeelsHeight;
        public Action OnComplete;
        public string BakingMachineId;
        
        public struct BakedViewInfo
        {
            public long CharacterId;
            public Guid Version;
        }
    }
}