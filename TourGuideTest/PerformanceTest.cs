using System.Diagnostics;
using GpsUtil.Location;
using TourGuide.Users;
using Xunit.Abstractions;

namespace TourGuideTest
{
    public class PerformanceTest : IClassFixture<DependencyFixture>
    {
        private readonly DependencyFixture _fixture;

        private readonly ITestOutputHelper _output;

        public PerformanceTest(DependencyFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _output = output;
        }

        [Fact]
        public void HighVolumeGetRewards()
        {
            //On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(100000);

            // Arrêter le suivi des utilisateurs pour éviter les conflits pendant le test de récompenses
            _fixture.TourGuideService.Tracker.StopTracking();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Attraction attraction = _fixture.GpsUtil.GetAttractions()[0];
            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();
            allUsers.ForEach(u => u.AddToVisitedLocations(new VisitedLocation(u.UserId, attraction, DateTime.Now)));

            // Calcul des récompenses (fait en parallèle dans la méthode)
            _fixture.RewardsService.CalculateRewards(allUsers);

            stopWatch.Stop();

            Assert.True(TimeSpan.FromMinutes(20).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }

        [Fact]
        public void HighVolumeTrackLocation()
        {
            //On peut ici augmenter le nombre d'utilisateurs pour tester les performances
            _fixture.Initialize(100000);

            // Arrêter le suivi des utilisateurs pour éviter les conflits pendant le test de localisation
            _fixture.TourGuideService.Tracker.StopTracking();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            List<User> allUsers = _fixture.TourGuideService.GetAllUsers();

            // Suivi de la localisation (fait en parallèle dans la méthode)
            _fixture.TourGuideService.TrackUserLocation(allUsers);

            stopWatch.Stop();

            Assert.True(TimeSpan.FromMinutes(15).TotalSeconds >= stopWatch.Elapsed.TotalSeconds);
        }
    }
}
