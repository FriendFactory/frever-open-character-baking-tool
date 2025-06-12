using System.Collections.Generic;
using System.Linq;
using Bridge.Models.ClientServer.StartPack.Metadata;
using Models;

namespace Extensions
{
    public static class MetadataStartPackExtensions
    {
        public static IEnumerable<Race> GetRaces(this MetadataStartPack metadataStartPack)
        {
            return metadataStartPack.IntellectualProperty.SelectMany(x => x.Races);
        }
        
        public static Race GetRace(this MetadataStartPack metadataStartPack, long id)
        {
            return metadataStartPack.GetRaces().FirstOrDefault(x => x.Id == id);
        }

        public static Gender GetGenderByUmaRaceName(this MetadataStartPack metadataStartPack, string umaGenderRace)
        {
            return metadataStartPack.GetAllGenders().FirstOrDefault(x => x.UmaRaceKey == umaGenderRace);
        }

        public static IEnumerable<Gender> GetAllGenders(this MetadataStartPack metadataStartPack)
        {
            return metadataStartPack.GetRaces().SelectMany(x => x.Genders);
        }

        public static Gender GetGenderById(this MetadataStartPack metadataStartPack, long id)
        {
            return metadataStartPack.GetAllGenders().FirstOrDefault(x => x.Id == id);
        }
        
        public static string GetUmaRaceNameByGenderId(this MetadataStartPack metadataStartPack, long genderId)
        {
            return metadataStartPack.GetAllGenders().FirstOrDefault(x => x.Id == genderId).UmaRaceKey;
        }

        public static Universe GetUniverseByGenderId(this MetadataStartPack metadataStartPack, long genderId)
        {
            var race = metadataStartPack.GetRaceByGenderId(genderId);
            return race is {} ? metadataStartPack.GetUniverseByRaceId(race.Id) : null;
        }

        public static Universe GetUniverseByRaceId(this MetadataStartPack metadataStartPack, long raceId)
        {
            return metadataStartPack.Universes.FirstOrDefault(u => u.Races.Any(r => r.RaceId == raceId));
        }
        
        public static Race GetRaceByUniverseId(this MetadataStartPack metadataStartPack, long universeId)
        {
            var raceId = metadataStartPack.Universes.FirstOrDefault(u => u.Id == universeId)?.Races.First().RaceId;
            return metadataStartPack.GetRaces().FirstOrDefault(x => x.Id == raceId);
        }

        public static bool IsSelfieToAvatarSupportedByRace(this MetadataStartPack metadataStartPack, long raceId)
        {
            var universe = metadataStartPack.GetUniverseByRaceId(raceId);
            var settings = universe.Races.First(x=>x.RaceId == raceId).Settings;
            return settings.SupportsSelfieToAvatar;
        }

        public static Race GetRaceByGenderId(this MetadataStartPack metadataStartPack, long genderId)
        {
            return metadataStartPack.IntellectualProperty.SelectMany(x => x.Races)
                                    .FirstOrDefault(x => x.Genders.Any(_ => _.Id == genderId));
        }

        public static IEnumerable<Universe> GetActiveUniverses(this MetadataStartPack metadataStartPack)
        {
            return metadataStartPack.Universes.Where(x => x.Races.Any(race => race.Settings.CanUseCharacters));
        }

        public static int GetActiveUniversesCount(this MetadataStartPack metadataStartPack)
        {
            return metadataStartPack.GetActiveUniverses().Count();
        }
    }
}