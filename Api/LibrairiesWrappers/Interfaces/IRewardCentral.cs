namespace TourGuide.LibrairiesWrappers.Interfaces
{
    public interface IRewardCentral
    {
        Task<int> GetAttractionRewardPoints(Guid attractionId, Guid userId);
    }
}
