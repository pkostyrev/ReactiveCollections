namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class ObservableListTests
    {
        [Test]
        public void Add_ShouldIncreaseCount()
        {
            // Arrange
            var list = new ObservableList<int>();

            // Act
            list.Add(10);

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(10));
        }

        [Test]
        public void Remove_ShouldRemoveItem()
        {
            // Arrange
            var list = new ObservableList<int>();

            list.Add(10);

            // Act
            list.Remove(10);

            // Assert
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_ShouldRaiseUpdateEvent()
        {
            // Arrange
            var list = new ObservableList<int>();

            list.Add(10);

            Change<int>? received = null;

            list.Changed += change =>
            {
                received = change;
            };

            // Act
            list.Update(10);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void Replace_ShouldReplaceItem()
        {
            // Arrange
            var list = new ObservableList<int>();

            list.Add(10);

            // Act
            list.Replace(10, 20);

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(20));
        }

        [Test]
        public void Enumerator_ShouldReturnAllItems()
        {
            // Arrange
            var list = new ObservableList<int>();

            list.Add(10);
            list.Add(20);
            list.Add(30);

            // Act
            var items = list.ToArray();

            // Assert
            Assert.That(items, Is.EqualTo(new[]
            {
                10,
                20,
                30
            }));
        }

        [Test]
        public void Add_ShouldRaiseAddEvent()
        {
            // Arrange
            var list = new ObservableList<int>();

            Change<int>? received = null;

            list.Changed += change =>
            {
                received = change;
            };

            // Act
            list.Add(10);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void Events_ShouldBeRaisedInCorrectOrder()
        {
            // Arrange
            var list = new ObservableList<int>();

            var events = new List<ChangeType>();

            list.Changed += change => events.Add(change.Type);

            // Act
            list.Add(1);
            list.Update(1);
            list.Replace(1, 2);
            list.Remove(2);

            // Assert
            Assert.That(events, Is.EqualTo(new[]
            {
                ChangeType.Add,
                ChangeType.Update,
                ChangeType.Replace,
                ChangeType.Remove
            }));
        }

        [Test]
        public void Reset_ShouldClearCollection()
        {
            var list = new ObservableList<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);

            Change<int>? received = null;

            list.Changed += c => received = c;

            list.Reset();

            Assert.That(list.Count, Is.EqualTo(0));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Reset));
        }

        [Test]
        public void Remove_ExistingItem_ShouldReturnTrue()
        {
            var list = new ObservableList<int>();

            list.Add(10);

            bool result = list.Remove(10);

            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_MissingItem_ShouldReturnFalseAndNotRaiseEvent()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;

            list.Changed += change => received = change;

            bool result = list.Remove(10);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_MissingItem_ShouldReturnFalseAndNotRaiseEvent()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;

            list.Changed += change => received = change;

            bool result = list.Update(10);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
        }

        [Test]
        public void Replace_MissingItem_ShouldReturnFalseAndNotRaiseEvent()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;

            list.Changed += change => received = change;

            bool result = list.Replace(10, 20);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_ExistingItem_ShouldReturnTrue()
        {
            var list = new ObservableList<int>();

            list.Add(10);

            bool result = list.Update(10);

            Assert.That(result, Is.True);
        }

        [Test]
        public void Replace_ExistingItem_ShouldReturnTrue()
        {
            var list = new ObservableList<int>();

            list.Add(10);

            bool result = list.Replace(10, 20);

            Assert.That(result, Is.True);
            Assert.That(list[0], Is.EqualTo(20));
        }

        [Test]
        public void Reset_NonEmptyList_ShouldReturnTrue()
        {
            var list = new ObservableList<int>();

            list.Add(10);

            bool result = list.Reset();

            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_EmptyList_ShouldReturnFalseAndNotRaiseEvent()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;

            list.Changed += change => received = change;

            bool result = list.Reset();

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
        }
    }
}