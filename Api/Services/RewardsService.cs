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

    public void CalculateRewards(User user)
    {
        // Compteur thread-safe
        Interlocked.Increment(ref count);

        // Snapshots pour éviter les problèmes de collection modifiée pendant l'itération
        var userLocations = (user.VisitedLocations ?? new List<VisitedLocation>()).ToArray();

        if (userLocations.Length == 0)
        {
            var current = _gpsUtil.GetUserLocation(user.UserId);
            userLocations = new[] { current };
        }

        var attractions = (_gpsUtil.GetAttractions() ?? new List<Attraction>()).ToArray();

        // Récompenses déjà attribuées pour éviter les doublons (HashSet pour performance)
        var rewardedNames = new HashSet<string>((user.UserRewards ?? new List<UserReward>())
                .Select(r => r.Attraction.AttractionName));

        foreach (var visitedLocation in userLocations)
        {
            foreach (var attraction in attractions)
            {
                if (rewardedNames.Contains(attraction.AttractionName))
                    continue;

                // Vérification de la proximité
                if (NearAttraction(visitedLocation, attraction))
                {
                    var points = GetRewardPoints(attraction, user);
                    var reward = new UserReward(visitedLocation, attraction, points);

                    lock (user)
                    {
                        if (!user.UserRewards.Any(r => r.Attraction.AttractionName == attraction.AttractionName))
                        {
                            user.AddUserReward(reward);
                            rewardedNames.Add(attraction.AttractionName);
                        }
                    }
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

    public int GetRewardPoints(Attraction attraction, User user)
    {
        return _rewardsCentral.GetAttractionRewardPoints(attraction.AttractionId, user.UserId);
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
