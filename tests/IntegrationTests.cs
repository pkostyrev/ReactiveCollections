using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        [Test]
        public void Chain_ShouldInitializeFromExistingSource()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 5);

            var mike = TestData.CreatePlayer(
                id: 3,
                name: "Mike",
                teamId: 20,
                level: 20);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            // Act
            var groups = CreateChain(source);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(
                group => group.Key == 10);

            var team20 = groups.Single(
                group => group.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(1));
            Assert.That(team10.Items[0].Id, Is.EqualTo(bob.Id));
            Assert.That(team10.Items[0].Text, Is.EqualTo("Bob"));

            Assert.That(team20.Items.Count, Is.EqualTo(1));
            Assert.That(team20.Items[0].Id, Is.EqualTo(mike.Id));
            Assert.That(team20.Items[0].Text, Is.EqualTo("Mike"));
        }

        [Test]
        public void Add_MatchingPlayer_ShouldPassThroughEntireChain()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            // Act
            source.Add(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));

            var view = groups[0].Items[0];

            Assert.That(view.Id, Is.EqualTo(bob.Id));
            Assert.That(view.Text, Is.EqualTo(bob.Name));
            Assert.That(view.TeamId, Is.EqualTo(bob.TeamId));
        }

        [Test]
        public void Add_NotMatchingPlayer_ShouldNotReachResult()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            // Act
            source.Add(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_PlayerStartsMatching_ShouldCreateViewAndGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            source.Add(bob);

            // Act
            bob.Level = 10;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0].Id, Is.EqualTo(bob.Id));
        }

        [Test]
        public void Update_PlayerStopsMatching_ShouldRemoveViewAndEmptyGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            source.Add(bob);

            Assert.That(groups.Count, Is.EqualTo(1));

            // Act
            bob.Level = 5;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_PlayerData_ShouldUpdateExistingView()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            source.Add(bob);

            var originalGroup = groups[0];
            var originalView = originalGroup.Items[0];

            // Act
            bob.Name = "Robert";

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));
            Assert.That(groups[0].Items[0], Is.SameAs(originalView));
            Assert.That(originalView.Text, Is.EqualTo("Robert"));
        }

        [Test]
        public void Update_PlayerTeam_ShouldMoveSameViewToAnotherGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 20);

            var mike = TestData.CreatePlayer(
                id: 3,
                name: "Mike",
                teamId: 20,
                level: 25);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            var team10Before = groups.Single(
                group => group.Key == 10);

            var team20Before = groups.Single(
                group => group.Key == 20);

            var originalBobView = team10Before.Items.Single(
                view => view.Id == bob.Id);

            // Act
            bob.TeamId = 20;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(2));

            var team10After = groups.Single(
                group => group.Key == 10);

            var team20After = groups.Single(
                group => group.Key == 20);

            Assert.That(team10After, Is.SameAs(team10Before));
            Assert.That(team20After, Is.SameAs(team20Before));

            Assert.That(
                team10After.Items.Any(view => view.Id == bob.Id),
                Is.False);

            var movedBobView = team20After.Items.Single(
                view => view.Id == bob.Id);

            Assert.That(movedBobView, Is.SameAs(originalBobView));
            Assert.That(movedBobView.TeamId, Is.EqualTo(20));
        }

        [Test]
        public void Update_PlayerTeam_WhenOldGroupBecomesEmpty_ShouldRemoveOldGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            var mike = TestData.CreatePlayer(
                id: 2,
                name: "Mike",
                teamId: 20,
                level: 20);

            source.Add(bob);
            source.Add(mike);

            var originalView = groups
                .Single(group => group.Key == 10)
                .Items[0];

            // Act
            bob.TeamId = 20;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));

            var movedView = groups[0].Items.Single(
                view => view.Id == bob.Id);

            Assert.That(movedView, Is.SameAs(originalView));
        }

        [Test]
        public void Remove_MatchingPlayer_ShouldRemoveViewFromChain()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 20);

            source.Add(bob);
            source.Add(tom);

            var group = groups[0];

            // Act
            source.Remove(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(group));
            Assert.That(group.Items.Count, Is.EqualTo(1));
            Assert.That(group.Items[0].Id, Is.EqualTo(tom.Id));
        }

        [Test]
        public void Remove_LastMatchingPlayer_ShouldRemoveEmptyGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15);

            source.Add(bob);

            // Act
            source.Remove(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(0));
        }

        private static IObservableList<Group<int, PlayerView>> CreateChain(
            IObservableList<Player> source)
        {
            return source
                .Filter(player => player.Level >= 10)
                .Select(
                    player => new PlayerView(),
                    Bind)
                .GroupBy(view => view.TeamId);
        }

        [Test]
        public void Reset_ShouldAllowFurtherUpdates()
        {
            var source = new ObservableList<Player>();

            var groups = CreateChain(source);

            source.Add(TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15));

            source.Reset();

            source.Add(TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 20,
                level: 30));

            Assert.That(groups.Count, Is.EqualTo(1));

            Assert.That(groups[0].Key, Is.EqualTo(20));

            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
        }

        private static void Bind(
            Player source,
            PlayerView result)
        {
            result.Id = source.Id;
            result.Text = source.Name;
            result.TeamId = source.TeamId;
        }

        [Test]
        public void Raise_OneSubscriberThrows_OthersStillGetEvent()
        {
            var list = new ObservableList<int>();
            var secondCalled = false;

            list.Changed += _ => throw new Exception("boom");
            list.Changed += _ => secondCalled = true;

            Assert.Throws<Exception>(() => list.Add(1));
            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void Raise_TwoSubscribersThrow_AggregateException()
        {
            var list = new ObservableList<int>();

            list.Changed += _ => throw new Exception("first");
            list.Changed += _ => throw new Exception("second");

            var ex = Assert.Throws<AggregateException>(() => list.Add(1));
            Assert.That(ex!.InnerExceptions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Raise_NoExceptions_DoesNotThrow()
        {
            var list = new ObservableList<int>();
            list.Changed += _ => { };

            Assert.DoesNotThrow(() => list.Add(1));
        }
    }
}
