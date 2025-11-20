using GpsUtil.Location;
using TourGuide.LibrairiesWrappers.Interfaces;

namespace TourGuide.LibrairiesWrappers;

public class GpsUtilWrapper : IGpsUtil
{
    private readonly GpsUtil.GpsUtil _gpsUtil;

    public GpsUtilWrapper()
    {
        _gpsUtil = new();
    }

    public VisitedLocation GetUserLocation(Guid userId)
    {
        return _gpsUtil.GetUserLocation(userId);
    }

    public List<Attraction> GetAttractions()
    {
        return _gpsUtil.GetAttractions();
    }

    public List<VisitedLocation> GetUsersLocation(IEnumerable<Guid> usersId)
    {
        return _gpsUtil.GetUsersLocation(usersId);
    }
}
