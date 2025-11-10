namespace TourGuide.Dtos
{
    public record NearbyAttractionDto(
        string AttractionName,
        double AttractionLatitude,
        double AttractionLongitude,
        double UserLatitude,
        double UserLongitude,
        double DistanceInMiles,
        int RewardPoints
        );
}
