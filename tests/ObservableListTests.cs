using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="ObservableList{T}"/> — публичного API корневого источника.
    /// </summary>
    /// <remarks>
    /// Здесь же тестируется поведение базового <see cref="ObservableNode{T}.Raise"/>
    /// (обработка исключений в подписчиках, порядок вызовов) — потому что
    /// <see cref="ObservableList{T}"/> это самый простой конкретный подкласс.
    /// </remarks>
    [TestFixture]
    public class ObservableListTests
    {
        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_NewItem_IncreasesCount()
        {
            var list = new ObservableList<int>();

            list.Add(10);

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(10));
        }

        [Test]
        public void Add_RaisesAddEvent()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;
            list.Changed += c => received = c;

            list.Add(10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void Add_ReferenceItem_PreservesReference()
        {
            var list = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            list.Add(p);

            Assert.That(list[0], Is.SameAs(p));
        }

        [Test]
        public void Add_NullReference_IsAllowed()
        {
            var list = new ObservableList<string?>();

            Assert.DoesNotThrow(() => list.Add(null));
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.Null);
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_ExistingItem_ReturnsTrueAndRemovesItem()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            bool result = list.Remove(10);

            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_MissingItem_ReturnsFalseAndDoesNotRaise()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Remove(10);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_DuplicateEquals_RemovesFirstMatch()
        {
            var list = new ObservableList<Player>();
            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            list.Add(p1);
            list.Add(p2);

            list.Remove(p2);

            // List.Remove удаляет первый равный — это p1
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.SameAs(p2));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_ExistingItem_ReturnsTrueAndRaisesUpdate()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Update(10);

            Assert.That(result, Is.True);
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void Update_MissingItem_ReturnsFalseAndDoesNotRaise()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Update(10);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
        }

        [Test]
        public void Update_DuplicateEquals_RaisesOnce()
        {
            var list = new ObservableList<int>();
            list.Add(1);
            list.Add(1);

            int count = 0;
            list.Changed += _ => count++;

            list.Update(1);

            Assert.That(count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        [Test]
        public void Replace_ExistingItem_ReplacesAndReturnsTrue()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            bool result = list.Replace(10, 20);

            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(20));
        }

        [Test]
        public void Replace_MissingItem_ReturnsFalseAndDoesNotRaise()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Replace(10, 20);

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void Replace_DuplicateEquals_ReplacesFirstMatch()
        {
            var list = new ObservableList<int>();
            list.Add(1);
            list.Add(2);
            list.Add(1);

            list.Replace(1, 99);

            Assert.That(list[0], Is.EqualTo(99));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_NonEmptyList_ClearsAndReturnsTrue()
        {
            var list = new ObservableList<int>();
            list.Add(1);
            list.Add(2);

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Reset();

            Assert.That(result, Is.True);
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Reset));
        }

        [Test]
        public void Reset_EmptyList_ReturnsFalseAndDoesNotRaise()
        {
            var list = new ObservableList<int>();

            Change<int>? received = null;
            list.Changed += c => received = c;

            bool result = list.Reset();

            Assert.That(result, Is.False);
            Assert.That(received, Is.Null);
        }

        // -------------------------------------------------------------------
        // Enumeration
        // -------------------------------------------------------------------

        [Test]
        public void Enumerator_ReturnsAllItemsInOrder()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            Assert.That(list.ToArray(), Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void Enumerator_EmptyList_ReturnsNothing()
        {
            var list = new ObservableList<int>();

            Assert.That(list.ToArray(), Is.Empty);
        }

        // -------------------------------------------------------------------
        // События
        // -------------------------------------------------------------------

        [Test]
        public void Events_RaisedInOperationOrder()
        {
            var list = new ObservableList<int>();

            var events = new List<ChangeType>();
            list.Changed += c => events.Add(c.Type);

            list.Add(1);
            list.Update(1);
            list.Replace(1, 2);
            list.Remove(2);

            Assert.That(events, Is.EqualTo(new[]
            {
                ChangeType.Add,
                ChangeType.Update,
                ChangeType.Replace,
                ChangeType.Remove
            }));
        }

        // -------------------------------------------------------------------
        // Raise: обработка исключений в подписчиках
        // -------------------------------------------------------------------

        [Test]
        public void Raise_OneSubscriberThrows_OthersStillGetEvent()
        {
            var list = new ObservableList<int>();
            bool secondCalled = false;

            list.Changed += _ => throw new Exception("boom");
            list.Changed += _ => secondCalled = true;

            Assert.Throws<Exception>(() => list.Add(1));
            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void Raise_TwoSubscribersThrow_ThrowsAggregateException()
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

        [Test]
        public void Raise_SubscribersCalledInSubscriptionOrder()
        {
            var list = new ObservableList<int>();
            var order = new List<int>();

            list.Changed += _ => order.Add(1);
            list.Changed += _ => order.Add(2);
            list.Changed += _ => order.Add(3);

            list.Add(0);

            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_IsIdempotent()
        {
            var list = new ObservableList<int>();
            list.Dispose();

            Assert.DoesNotThrow(() => list.Dispose());
        }

        [Test]
        public void Dispose_MutationAfterDispose_Throws()
        {
            var list = new ObservableList<int>();
            list.Add(1);
            list.Dispose();

            Assert.Throws<ObjectDisposedException>(() => list.Add(2));
            Assert.Throws<ObjectDisposedException>(() => list.Remove(1));
            Assert.Throws<ObjectDisposedException>(() => list.Update(1));
            Assert.Throws<ObjectDisposedException>(() => list.Replace(1, 2));
            Assert.Throws<ObjectDisposedException>(() => list.Reset());
        }

        [Test]
        public void Dispose_ReadAfterDispose_DoesNotThrow()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Dispose();

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(10));
            Assert.That(list.ToArray(), Is.EqualTo(new[] { 10 }));
        }
    }
}