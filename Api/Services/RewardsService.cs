using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;
using TourGuide.Services.Interfaces;
using TourGuide.Users;

namespace TourGuide.Services;

public class RewardsService : IRewardsService
{
    private const double StatuteMilesPerNauticalMile = 1.15077945;
    private readonly int _defaultProximityBuffer = 10;
    private int _proximityBuffer;
    private readonly int _attractionProximityRange = 200;
    private readonly IGpsUtil _gpsUtil;
    private readonly IRewardCentral _rewardsCentral;
    private static int count = 0;

    public RewardsService(IGpsUtil gpsUtil, IRewardCentral rewardCentral)
    {
        _gpsUtil = gpsUtil;
        _rewardsCentral = rewardCentral;
        _proximityBuffer = _defaultProximityBuffer;
    }

    public void SetProximityBuffer(int proximityBuffer)
    {
        _proximityBuffer = proximityBuffer;
    }

    public void SetDefaultProximityBuffer()
    {
        _proximityBuffer = _defaultProximityBuffer;
    }

    // Permet de calculer les Rewards pour plusieurs users en même temps avec du parallélisme
    public async Task CalculateRewardsAsync(List<User> users)
    {
        // Compteur thread-safe pour suivre le nombre d'utilisateurs traités
        Interlocked.Increment(ref count);

        // Configure les options de parallélisme pour utiliser un nombre élevé de threads selon le nombre de processeurs
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount * 10
        };

        // Permet de traiter chaque user en parallèle pour calculer ses rewards
        await Parallel.ForEachAsync(users, options, async (user, cancellationToken) =>
        {
            await CalculateRewardsAsync(user);
        });
    }

    public async Task CalculateRewardsAsync(User user)
    {
        // Assure que la liste des VisitedLocations n'est pas nulle, sinon on utilise la location actuelle
        var userLocations = (user.VisitedLocations ?? new List<VisitedLocation>()).ToArray();
        if (userLocations.Length == 0)
        {
            var current = _gpsUtil.GetUserLocation(user.UserId);
            userLocations = new[] { current };
        }

        // Assure que la liste des Attractions n'est pas nulle (sinon on utilise une liste vide) et on récupère les noms des attractions déjà récompensées
        var attractions = (_gpsUtil.GetAttractions() ?? new List<Attraction>()).ToArray();
        var rewardedAttractionNames = new HashSet<string>((user.UserRewards ?? new List<UserReward>()).Select(r => r.Attraction.AttractionName));

        // Trouve toutes les paires (VisitedLocation, Attraction) qui sont proches et pas encore récompensées
        var nearbyPairs = new List<(VisitedLocation location, Attraction attraction)>();

        foreach (var visitedLocation in userLocations)
        {
            foreach (var attraction in attractions)
            {
                if (rewardedAttractionNames.Contains(attraction.AttractionName))
                {
                    continue;
                }
                // Vérifie si la VisitedLocation est proche de l'Attraction
                if (NearAttraction(visitedLocation, attraction))
                {
                    // Ajoute la paire à la liste des paires proches
                    nearbyPairs.Add((visitedLocation, attraction));
                    // Marque cette attraction comme déjà récompensée pour éviter les doublons
                    rewardedAttractionNames.Add(attraction.AttractionName);
                }
            }
        }
        // Si aucune paire proche n'a été trouvée, on retourne immédiatement
        if (nearbyPairs.Count == 0)
        {
            return;
        }
        // Calcule les points de récompense pour chaque paire 
        var rewardTasks = nearbyPairs.Select(async pair =>
        {
            var points = await GetRewardPointsAsync(pair.attraction, user);
            return (pair.location, pair.attraction, points);
        }).ToList();

        // WhenAll pour attendre que toutes les tâches soient terminées
        var results = await Task.WhenAll(rewardTasks);

        // Ajoute les récompenses à l'utilisateur de manière thread-safe
        lock (user)
        {
            foreach (var (location, attraction, points) in results)
            {
                if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                {
                    var reward = new UserReward(location, attraction, points);
                    user.AddUserReward(reward);
                }
            }
        }
    }

    public bool IsWithinAttractionProximity(Attraction attraction, Locations location)
    {
        Console.WriteLine(GetDistance(attraction, location));
        return GetDistance(attraction, location) <= _attractionProximityRange;
    }

    private bool NearAttraction(VisitedLocation visitedLocation, Attraction attraction)
    {
        return GetDistance(attraction, visitedLocation.Location) <= _proximityBuffer;
    }

    public Task<int> GetRewardPointsAsync(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPointsAsync(attraction.AttractionId, user.UserId);
    }



    public double GetDistance(Locations loc1, Locations loc2)
    {
        double lat1 = Math.PI * loc1.Latitude / 180.0;
        double lon1 = Math.PI * loc1.Longitude / 180.0;
        double lat2 = Math.PI * loc2.Latitude / 180.0;
        double lon2 = Math.PI * loc2.Longitude / 180.0;

        double angle = Math.Acos(Math.Sin(lat1) * Math.Sin(lat2)
                                + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2));

        double nauticalMiles = 60.0 * angle * 180.0 / Math.PI;
        return StatuteMilesPerNauticalMile * nauticalMiles;
    }
}
