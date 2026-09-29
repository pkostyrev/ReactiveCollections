using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class GroupNodeTests
    {
        [Test]
        public void Constructor_ShouldInitializeFromExistingSource()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 1);

            var mike = TestData.CreatePlayer(
                id: 3,
                name: "Mike",
                teamId: 20,
                level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            // Act
            var groups = source.GroupBy(
                player => player.TeamId);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(
                group => group.Key == 10);

            var team20 = groups.Single(
                group => group.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(2));
            Assert.That(team10.Items, Does.Contain(bob));
            Assert.That(team10.Items, Does.Contain(tom));

            Assert.That(team20.Items.Count, Is.EqualTo(1));
            Assert.That(team20.Items[0], Is.SameAs(mike));
        }

        [Test]
        public void Add_FirstItemWithKey_ShouldCreateGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            // Act
            source.Add(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(bob));
        }

        [Test]
        public void Add_ItemWithExistingKey_ShouldReuseGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 1);

            source.Add(bob);

            var originalGroup = groups[0];

            // Act
            source.Add(tom);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));

            Assert.That(originalGroup.Items.Count, Is.EqualTo(2));
            Assert.That(originalGroup.Items, Does.Contain(bob));
            Assert.That(originalGroup.Items, Does.Contain(tom));
        }

        [Test]
        public void Add_FirstItemWithKey_ShouldRaiseGroupAddEvent()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            Change<Group<int, Player>>? received = null;

            groups.Changed += change =>
            {
                received = change;
            };

            // Act
            source.Add(TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1));

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item.Key, Is.EqualTo(10));
        }

        [Test]
        public void Add_ItemToExistingGroup_ShouldRaiseInnerAddEvent()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 1);

            source.Add(bob);

            var group = groups[0];

            Change<Player>? received = null;

            group.Items.Changed += change =>
            {
                received = change;
            };

            // Act
            source.Add(tom);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item, Is.SameAs(tom));
        }

        [Test]
        public void Add_NewGroup_ShouldAllowSubscribingBeforeFirstInnerAdd()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            Change<Player>? innerChange = null;

            groups.Changed += change =>
            {
                if (change.Type != ChangeType.Add)
                    return;

                change.Item.Items.Changed += itemChange =>
                {
                    innerChange = itemChange;
                };
            };

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            // Act
            source.Add(bob);

            // Assert
            Assert.That(innerChange, Is.Not.Null);
            Assert.That(innerChange!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(innerChange.Item, Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyUnchanged_ShouldKeepItemInSameGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            source.Add(bob);

            var originalGroup = groups[0];

            // Act
            bob.Name = "Robert";

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(bob));
            Assert.That(groups[0].Items[0].Name, Is.EqualTo("Robert"));
        }

        [Test]
        public void Update_KeyUnchanged_ShouldRaiseInnerUpdateEvent()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            source.Add(bob);

            var group = groups[0];

            Change<Player>? received = null;

            group.Items.Changed += change =>
            {
                received = change;
            };

            // Act
            bob.Name = "Robert";

            source.Update(bob);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyChanged_ShouldMoveItemToAnotherGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 1);

            var mike = TestData.CreatePlayer(
                id: 3,
                name: "Mike",
                teamId: 20,
                level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            var originalBobReference = bob;

            // Act
            bob.TeamId = 20;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(
                group => group.Key == 10);

            var team20 = groups.Single(
                group => group.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(1));
            Assert.That(team10.Items[0], Is.SameAs(tom));

            Assert.That(team20.Items.Count, Is.EqualTo(2));
            Assert.That(team20.Items, Does.Contain(mike));
            Assert.That(team20.Items, Does.Contain(bob));

            var movedBob = team20.Items.Single(
                player => player.Id == bob.Id);

            Assert.That(movedBob, Is.SameAs(originalBobReference));
        }

        [Test]
        public void Update_KeyChangedAndOldGroupBecomesEmpty_ShouldRemoveOldGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var mike = TestData.CreatePlayer(
                id: 2,
                name: "Mike",
                teamId: 20,
                level: 1);

            source.Add(bob);
            source.Add(mike);

            // Act
            bob.TeamId = 20;

            source.Update(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));
            Assert.That(groups[0].Items, Does.Contain(bob));
            Assert.That(groups[0].Items, Does.Contain(mike));
        }

        [Test]
        public void Remove_ItemFromNonEmptyGroup_ShouldKeepGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            var tom = TestData.CreatePlayer(
                id: 2,
                name: "Tom",
                teamId: 10,
                level: 1);

            source.Add(bob);
            source.Add(tom);

            var group = groups[0];

            // Act
            source.Remove(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(group));
            Assert.That(group.Items.Count, Is.EqualTo(1));
            Assert.That(group.Items[0], Is.SameAs(tom));
        }

        [Test]
        public void Remove_LastItem_ShouldRemoveGroup()
        {
            // Arrange
            var source = new ObservableList<Player>();

            var groups = source.GroupBy(
                player => player.TeamId);

            var bob = TestData.CreatePlayer(
                id: 1,
                name: "Bob",
                teamId: 10,
                level: 1);

            source.Add(bob);

            var group = groups[0];

            Change<Group<int, Player>>? received = null;

            groups.Changed += change =>
            {
                received = change;
            };

            // Act
            source.Remove(bob);

            // Assert
            Assert.That(groups.Count, Is.EqualTo(0));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.SameAs(group));
        }

        [Test]
        public void GroupBy_DuplicateEquals_Works()
        {
            var source = new ObservableList<Player>();
            var p1 = new Player { Name = "A", TeamId = 1 };
            var p2 = new Player { Name = "A", TeamId = 1 }; // Equals по Name
            source.Add(p1);
            source.Add(p2);

            var node = source.GroupBy(p => p.TeamId);

            Assert.That(node.Count, Is.EqualTo(1)); // одна группа
            Assert.That(node[0].Items.Count, Is.EqualTo(2)); // в ней оба
        }
    }
}
