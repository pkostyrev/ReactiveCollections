using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class MergeNodeTests
    {
        [Test]
        public void Constructor_ShouldInitializeFromBothSources()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);

            second.Add(3);
            second.Add(4);

            // Act
            var merged = first.Merge(second);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(4));
            Assert.That(merged, Is.EqualTo(new[]
            {
            1,
            2,
            3,
            4
        }));
        }

        [Test]
        public void Constructor_WithEmptySources_ShouldCreateEmptyResult()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            // Act
            var merged = first.Merge(second);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_ToFirstSource_ShouldAddItemToResult()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            // Act
            first.Add(10);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Add_ToSecondSource_ShouldAddItemToResult()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            // Act
            second.Add(20);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(20));
        }

        [Test]
        public void Add_FromBothSources_ShouldRaiseAddEvents()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            var changes = new List<Change<int>>();

            merged.Changed += change =>
            {
                changes.Add(change);
            };

            // Act
            first.Add(10);
            second.Add(20);

            // Assert
            Assert.That(changes.Count, Is.EqualTo(2));

            Assert.That(changes[0].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(changes[0].Item, Is.EqualTo(10));

            Assert.That(changes[1].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(changes[1].Item, Is.EqualTo(20));
        }

        [Test]
        public void Remove_FromFirstSource_ShouldRemoveItemFromResult()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(20);

            var merged = first.Merge(second);

            // Act
            bool removed = first.Remove(10);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(20));
        }

        [Test]
        public void Remove_FromSecondSource_ShouldRemoveItemFromResult()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(20);

            var merged = first.Merge(second);

            // Act
            bool removed = second.Remove(20);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Remove_ShouldRaiseRemoveEvent()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);

            var merged = first.Merge(second);

            Change<int>? received = null;

            merged.Changed += change =>
            {
                received = change;
            };

            // Act
            first.Remove(10);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void DuplicateItems_ShouldBePreserved()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            // Act
            var merged = first.Merge(second);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged.Count(item => item == 10), Is.EqualTo(2));
        }

        [Test]
        public void Remove_OneDuplicate_ShouldKeepOtherOccurrence()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.Merge(second);

            // Act
            first.Remove(10);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Update_InFirstSource_ShouldRaiseUpdateEvent()
        {
            // Arrange
            var first = new ObservableList<TestItem>();
            var second = new ObservableList<TestItem>();

            var item = new TestItem
            {
                Id = 1,
                Name = "Old"
            };

            first.Add(item);

            var merged = first.Merge(second);

            Change<TestItem>? received = null;

            merged.Changed += change =>
            {
                received = change;
            };

            // Act
            item.Name = "New";

            bool updated = first.Update(item);

            // Assert
            Assert.That(updated, Is.True);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(item));

            Assert.That(merged[0], Is.SameAs(item));
            Assert.That(merged[0].Name, Is.EqualTo("New"));
        }

        [Test]
        public void Update_InSecondSource_ShouldRaiseUpdateEvent()
        {
            // Arrange
            var first = new ObservableList<TestItem>();
            var second = new ObservableList<TestItem>();

            var item = new TestItem
            {
                Id = 1,
                Name = "Old"
            };

            second.Add(item);

            var merged = first.Merge(second);

            Change<TestItem>? received = null;

            merged.Changed += change =>
            {
                received = change;
            };

            // Act
            item.Name = "New";

            second.Update(item);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(item));
            Assert.That(merged[0].Name, Is.EqualTo("New"));
        }

        [Test]
        public void Replace_InFirstSource_ShouldReplaceItemInResult()
        {
            // Arrange
            var first = new ObservableList<TestItem>();
            var second = new ObservableList<TestItem>();

            var oldItem = new TestItem
            {
                Id = 1,
                Name = "Old"
            };

            var newItem = new TestItem
            {
                Id = 2,
                Name = "New"
            };

            first.Add(oldItem);

            var merged = first.Merge(second);

            // Act
            bool replaced = first.Replace(
                oldItem,
                newItem);

            // Assert
            Assert.That(replaced, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.SameAs(newItem));
        }

        [Test]
        public void Replace_ShouldRaiseReplaceEvent()
        {
            // Arrange
            var first = new ObservableList<TestItem>();
            var second = new ObservableList<TestItem>();

            var oldItem = new TestItem
            {
                Id = 1,
                Name = "Old"
            };

            var newItem = new TestItem
            {
                Id = 2,
                Name = "New"
            };

            first.Add(oldItem);

            var merged = first.Merge(second);

            Change<TestItem>? received = null;

            merged.Changed += change =>
            {
                received = change;
            };

            // Act
            first.Replace(
                oldItem,
                newItem);

            // Assert
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Replace));
            Assert.That(received.OldItem, Is.SameAs(oldItem));
            Assert.That(received.Item, Is.SameAs(newItem));
        }

        [Test]
        public void Reset_FirstSource_ShouldKeepSecondSourceItems()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);

            second.Add(3);
            second.Add(4);

            var merged = first.Merge(second);

            // Act
            first.Reset();

            // Assert
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged, Is.EqualTo(new[]
            {
            3,
            4
        }));
        }

        [Test]
        public void Reset_SecondSource_ShouldKeepFirstSourceItems()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);

            second.Add(3);
            second.Add(4);

            var merged = first.Merge(second);

            // Act
            second.Reset();

            // Assert
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged, Is.EqualTo(new[]
            {
            1,
            2
        }));
        }

        [Test]
        public void Reset_ShouldAllowFurtherChanges()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.Merge(second);

            first.Reset();

            // Act
            first.Add(3);
            second.Add(4);

            // Assert
            Assert.That(merged.Count, Is.EqualTo(3));

            Assert.That(merged, Does.Contain(2));
            Assert.That(merged, Does.Contain(3));
            Assert.That(merged, Does.Contain(4));
        }

        [Test]
        public void Merge_ShouldBeUsableAsSourceForFilter()
        {
            // Arrange
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            var positive = merged.Filter(
                item => item > 0);

            // Act
            first.Add(-1);
            first.Add(10);

            second.Add(-2);
            second.Add(20);

            // Assert
            Assert.That(positive.Count, Is.EqualTo(2));
            Assert.That(positive, Is.EqualTo(new[]
            {
            10,
            20
        }));
        }

        [Test]
        public void Constructor_NullFirstSource_ShouldThrow()
        {
            var second = new ObservableList<int>();

            Assert.That(
                () => new MergeNode<int>(null!, second),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_NullSecondSource_ShouldThrow()
        {
            var first = new ObservableList<int>();

            Assert.That(
                () => new MergeNode<int>(first, null!),
                Throws.ArgumentNullException);
        }

        private sealed class TestItem
        {
            public int Id;
            public string Name = string.Empty;
        }

        [Test]
        public void Merge_RemoveDuplicate_RemovesCorrectSource()
        {
            var a = new ObservableList<Player>();
            var b = new ObservableList<Player>();

            var q = new Player { Name = "A" };
            b.Add(q);

            var m = a.Merge(b);                     // [q]

            var p = new Player { Name = "A" };      // p.Equals(q) == true
            a.Add(p);                               // m = [q, p]

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.SameAs(q));
            Assert.That(m[1], Is.SameAs(p));

            a.Remove(p);                            // раньше удаляло q, теперь — p

            Assert.That(m.Count, Is.EqualTo(1));
            Assert.That(m[0], Is.SameAs(q));
        }

        [Test]
        public void Merge_ReplaceFromOneSource_DoesNotAffectOther()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();

            a.Add(1);
            b.Add(1);

            var m = a.Merge(b);                     // [1, 1]

            a.Replace(1, 10);                       // должно заменить именно a-элемент

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.EqualTo(10));
            Assert.That(m[1], Is.EqualTo(1));
        }
    }
}
