using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class SelectNodeTests
    {
        [Test]
        public void Constructor_ShouldInitializeFromSource()
        {
            var source = new ObservableList<Player>();

            source.Add(TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5));

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[0].Text, Is.EqualTo("Bob"));
            Assert.That(views[0].TeamId, Is.EqualTo(10));
        }

        [Test]
        public void Add_ShouldCreateResult()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            var player = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            source.Add(player);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(player.Id));
            Assert.That(views[0].Text, Is.EqualTo(player.Name));
            Assert.That(views[0].TeamId, Is.EqualTo(player.TeamId));
        }

        [Test]
        public void Add_ShouldRaiseAddEvent()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            Change<PlayerView>? received = null;

            views.Changed += change => received = change;

            source.Add(TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item.Text, Is.EqualTo("Bob"));
        }

        [Test]
        public void Update_ShouldUpdateExistingResult()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            var player = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            source.Add(player);

            var originalView = views[0];

            player.Name = "Robert";
            player.TeamId = 20;

            source.Update(player);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0], Is.SameAs(originalView));
            Assert.That(views[0].Text, Is.EqualTo("Robert"));
            Assert.That(views[0].TeamId, Is.EqualTo(20));
        }

        [Test]
        public void Update_ShouldRaiseUpdateEventWithSameResult()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            var player = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            source.Add(player);

            var originalView = views[0];

            Change<PlayerView>? received = null;

            views.Changed += change => received = change;

            player.Name = "Robert";

            source.Update(player);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(originalView));
        }

        [Test]
        public void Remove_ShouldRemoveMappedResult()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                player => new PlayerView(),
                Bind);

            var player = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 5);

            source.Add(player);

            var view = views[0];

            Change<PlayerView>? received = null;

            views.Changed += change => received = change;

            source.Remove(player);

            Assert.That(views.Count, Is.EqualTo(0));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.SameAs(view));
        }

        [Test]
        public void Reset_ShouldClearProjection()
        {
            var source = new ObservableList<Player>();

            var views = source.Select(
                x => new PlayerView(),
                Bind);

            source.Add(TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 15));

            Assert.That(views.Count, Is.EqualTo(1));

            source.Reset();

            Assert.That(views.Count, Is.EqualTo(0));
        }

        private static void Bind(
            Player player,
            PlayerView view)
        {
            view.Id = player.Id;
            view.Text = player.Name;
            view.TeamId = player.TeamId;
        }

        [Test]
        public void Select_DuplicateEquals_Works()
        {
            var source = new ObservableList<Player>();
            var p1 = new Player { Name = "A" };
            var p2 = new Player { Name = "A" };  // Player.Equals по Name
            source.Add(p1);
            source.Add(p2);

            var node = source.Select(p => new object(), (_, _) => { });

            Assert.That(node.Count, Is.EqualTo(2));
        }

        [Test]
        public void Select_RemoveDuplicate_RemovesFirst()
        {
            var source = new ObservableList<Player>();
            var p1 = new Player { Name = "A" };
            var p2 = new Player { Name = "A" };
            source.Add(p1);
            source.Add(p2);

            var node = source.Select(p => new object(), (_, _) => { });
            source.Remove(p1);

            Assert.That(node.Count, Is.EqualTo(1));
        }

        [Test]
        public void Select_ValueType_Duplicates()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(1);

            var node = source.Select(x => x * 10, (_, _) => { });

            Assert.That(node.Count, Is.EqualTo(2));
        }
    }
}
