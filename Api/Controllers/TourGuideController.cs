using GpsUtil.Location;
using Microsoft.AspNetCore.Mvc;
using TourGuide.Dtos;
using TourGuide.Services.Interfaces;
using TourGuide.Users;
using TripPricer;

namespace TourGuide.Controllers;

[ApiController]
[Route("[controller]")]
public class TourGuideController : ControllerBase
{
    private readonly ITourGuideService _tourGuideService;
    private readonly IRewardsService _rewardsService;

    public TourGuideController(ITourGuideService tourGuideService, IRewardsService rewardsService)
    {
        _tourGuideService = tourGuideService;
        _rewardsService = rewardsService;
    }

    [HttpGet("getLocation")]
    public ActionResult<VisitedLocation> GetLocation([FromQuery] string userName)
    {
        var location = _tourGuideService.GetUserLocation(GetUser(userName));
        return Ok(location);
    }

    // TODO: Change this method to no longer return a List of Attractions.
    // Instead: Get the closest five tourist attractions to the user - no matter how far away they are.
    // Return a new JSON object that contains:
    // Name of Tourist attraction, 
    // Tourist attractions lat/long, 
    // The user's location lat/long, 
    // The distance in miles between the user's location and each of the attractions.
    // The reward points for visiting each Attraction.
    //    Note: Attraction reward points can be gathered from RewardsCentral
    [HttpGet("getNearbyAttractions")]
    public ActionResult<List<NearbyAttractionDto>> GetNearbyAttractions([FromQuery] string userName)
    {
        var user = GetUser(userName);
        if (user == null)
        {
            return NotFound("User '{userName}' not found");
        }

        var visitedLocation = _tourGuideService.GetUserLocation(user);
        var userLoc = visitedLocation.Location;
        var attractions = _tourGuideService.GetAttractions();

        var result = attractions
            .Select(a =>
        {
            var attractionLoc = new Locations(a.Latitude, a.Longitude);
            var distance = _rewardsService.GetDistance(attractionLoc, userLoc);
            var points = _rewardsService.GetRewardPointsAsync(a, user);

            return new
            {
                Attraction = a,
                Distance = distance,
                Points = points,
            };

        }).OrderBy(x => x.Distance)
          .Take(5)
          .Select(x => new NearbyAttractionDto(
              x.Attraction.AttractionName,
              x.Attraction.Latitude,
              x.Attraction.Longitude,
              userLoc.Latitude,
              userLoc.Longitude,
              // Round distance to 2 decimal places
              Math.Round(x.Distance, 2),
              x.Points.Result
              ))
            .ToList();

        return Ok(result);
    }

    [HttpGet("getRewards")]
    public ActionResult<List<UserReward>> GetRewards([FromQuery] string userName)
    {
        var rewards = _tourGuideService.GetUserRewards(GetUser(userName));
        return Ok(rewards);
    }

    [HttpGet("getTripDeals")]
    public ActionResult<List<Provider>> GetTripDeals([FromQuery] string userName)
    {
        var deals = _tourGuideService.GetTripDeals(GetUser(userName));
        return Ok(deals);
    }

    private User GetUser(string userName)
    {
        return _tourGuideService.GetUser(userName);
    }
}
