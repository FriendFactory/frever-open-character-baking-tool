using System;
using UnityEngine;

namespace ExportCharacterTool
{
    [CreateAssetMenu(fileName = "CharacterManagerConfig.asset", menuName = "Friend Factory/Configs/Character Manager Config", order = 4)]
    public sealed class CharacterManagerConfig : ScriptableObject
    {
        public int MaxCharactersCount = 6;
        public long MaleId = 1;
        public long FemaleId = 2;
        public long NonBinaryId = 3;
        public string MaleKey = "Male Base";
        public string FemaleKey = "Female Base";
        public string NonBinaryKey = "NonBinary Base";
        public int StarsGroupId = 1;
        public string DefaultMaleRecipeName = "DefaultMaleCharacter";
        public string DefaultFemaleRecipeName = "DefaultFemaleCharacter";
        public string DefaultNonBinaryRecipeName = "DefaultNonBinaryCharacter";
        public string MaleLowerOverlayName = "underwear_M_Shorts_v1_Overlay";
        public string FemaleLowerOverlayName = "underwear_F_Shorts_v1_Overlay";
        public string FemaleUpperOverlayName = "underwear_F_Bra_v1_Overlay";
        public SlotClipping[] SlotsClippingMatrix;

        [Serializable]
        public sealed class SlotClipping
        {
            public string Slot;
            public string[] ClippingSlots;
        }
    }
}