using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="CountNode{T}"/> — агрегата «количество элементов».
    /// </summary>
    [TestFixture]
    public class CountNodeTests
    {
        // -------------------------------------------------------------------
        // Конструктор и начальное значение
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ValueIsZero()
        {
            var source = new ObservableList<int>();

            var count = source.ObserveCount();

            Assert.That(count.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_SourceWithElements_ValueEqualsCount()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            var count = source.ObserveCount();

            Assert.That(count.Value, Is.EqualTo(3));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveCount<int>(null!));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_IncrementsValueAndRaises()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            ValueChange<int>? captured = null;
            count.Changed += c => captured = c;

            source.Add(10);

            Assert.That(count.Value, Is.EqualTo(1));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(0));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_DecrementsValueAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var count = source.ObserveCount();

            ValueChange<int>? captured = null;
            count.Changed += c => captured = c;

            source.Remove(1);

            Assert.That(count.Value, Is.EqualTo(1));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(2));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update и Replace: значение не меняется, событие не райзится
        // -------------------------------------------------------------------

        [Test]
        public void Update_DoesNotChangeValueOrRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var count = source.ObserveCount();

            bool raised = false;
            count.Changed += _ => raised = true;

            source.Update(1);

            Assert.That(count.Value, Is.EqualTo(1));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Replace_DoesNotChangeValueOrRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var count = source.ObserveCount();

            bool raised = false;
            count.Changed += _ => raised = true;

            source.Replace(1, 2);

            Assert.That(count.Value, Is.EqualTo(1));
            Assert.That(raised, Is.False);
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsValueAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var count = source.ObserveCount();

            ValueChange<int>? captured = null;
            count.Changed += c => captured = c;

            source.Reset();

            Assert.That(count.Value, Is.EqualTo(0));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(2));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Raise
        // -------------------------------------------------------------------

        [Test]
        public void Raise_OneSubscriberThrows_OthersStillGetEvent()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            bool secondCalled = false;
            count.Changed += _ => throw new Exception("boom");
            count.Changed += _ => secondCalled = true;

            Assert.Throws<Exception>(() => source.Add(1));
            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void Raise_TwoSubscribersThrow_ThrowsAggregateException()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            count.Changed += _ => throw new Exception("first");
            count.Changed += _ => throw new Exception("second");

            var ex = Assert.Throws<AggregateException>(() => source.Add(1));
            Assert.That(ex!.InnerExceptions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Raise_SubscribersCalledInSubscriptionOrder()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            var order = new List<int>();
            count.Changed += _ => order.Add(1);
            count.Changed += _ => order.Add(2);
            count.Changed += _ => order.Add(3);

            source.Add(1);

            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            bool received = false;
            count.Changed += _ => received = true;

            count.Dispose();

            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            count.Dispose();

            Assert.DoesNotThrow(() => count.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            count.Dispose();

            Assert.DoesNotThrow(() => source.Add(1));
            Assert.That(source.Count, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_ReadAfterDispose_DoesNotThrow()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var count = source.ObserveCount();

            count.Dispose();

            Assert.That(count.Value, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Работа с Player
        // -------------------------------------------------------------------

        [Test]
        public void Count_WithReferenceElements_Works()
        {
            var source = new ObservableList<Player>();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1));

            var count = source.ObserveCount();

            Assert.That(count.Value, Is.EqualTo(2));

            source.Remove(source[0]);

            Assert.That(count.Value, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Использование в цепочке
        // -------------------------------------------------------------------

        [Test]
        public void Count_AfterFilter_ReflectsFilteredCount()
        {
            var source = new ObservableList<Player>();
            var alive = source.ObserveWhere(p => p.Level > 0);

            var count = alive.ObserveCount();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));
            source.Add(TestData.CreatePlayer(id: 2, name: "Ghost", teamId: 10, level: 0));

            Assert.That(count.Value, Is.EqualTo(1));
        }
    }
}