using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="SumNode{TNumber}"/> и
    /// <see cref="SumWithSelectorNode{TSource, TNumber}"/>.
    /// </summary>
    [TestFixture]
    public class SumNodeTests
    {
        // -------------------------------------------------------------------
        // Без селектора
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ValueIsZero()
        {
            var source = new ObservableList<int>();

            var sum = source.ObserveSum();

            Assert.That(sum.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NonEmptySource_ValueIsSum()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            var sum = source.ObserveSum();

            Assert.That(sum.Value, Is.EqualTo(6));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveSum((IObservableList<int>)null!));
        }

        [Test]
        public void Add_IncreasesValueAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var sum = source.ObserveSum();

            ValueChange<int>? captured = null;
            sum.Changed += c => captured = c;

            source.Add(2);

            Assert.That(sum.Value, Is.EqualTo(3));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(1));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(3));
        }

        [Test]
        public void Remove_DecreasesValueAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var sum = source.ObserveSum();

            ValueChange<int>? captured = null;
            sum.Changed += c => captured = c;

            source.Remove(1);

            Assert.That(sum.Value, Is.EqualTo(2));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(3));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(2));
        }

        [Test]
        public void Update_DoesNotChangeValueOrRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var sum = source.ObserveSum();

            bool raised = false;
            sum.Changed += _ => raised = true;

            source.Update(1);

            Assert.That(sum.Value, Is.EqualTo(3));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Replace_WithSameSum_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var sum = source.ObserveSum();

            bool raised = false;
            sum.Changed += _ => raised = true;

            source.Replace(1, 1);

            Assert.That(sum.Value, Is.EqualTo(3));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Replace_WithDifferentSum_Raises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var sum = source.ObserveSum();

            ValueChange<int>? captured = null;
            sum.Changed += c => captured = c;

            source.Replace(1, 10);

            Assert.That(sum.Value, Is.EqualTo(12));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(3));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(12));
        }

        [Test]
        public void Reset_NonEmptySource_ValueIsZeroAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var sum = source.ObserveSum();

            ValueChange<int>? captured = null;
            sum.Changed += c => captured = c;

            source.Reset();

            Assert.That(sum.Value, Is.EqualTo(0));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(3));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(0));
        }

        [Test]
        public void EmptyAddRemove_RoundTripsToZero()
        {
            var source = new ObservableList<int>();
            var sum = source.ObserveSum();

            source.Add(5);
            source.Remove(5);

            Assert.That(sum.Value, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Перегрузки по типам
        // -------------------------------------------------------------------

        [Test]
        public void ObserveSum_Long_Works()
        {
            var source = new ObservableList<long>();
            source.Add(1_000_000_000L);
            source.Add(2_000_000_000L);

            var sum = source.ObserveSum();

            Assert.That(sum.Value, Is.EqualTo(3_000_000_000L));
        }

        [Test]
        public void ObserveSum_Double_Works()
        {
            var source = new ObservableList<double>();
            source.Add(0.5);
            source.Add(0.25);

            var sum = source.ObserveSum();

            Assert.That(sum.Value, Is.EqualTo(0.75).Within(1e-10));
        }

        [Test]
        public void ObserveSum_Decimal_Works()
        {
            var source = new ObservableList<decimal>();
            source.Add(1.1m);
            source.Add(2.2m);

            var sum = source.ObserveSum();

            Assert.That(sum.Value, Is.EqualTo(3.3m));
        }

        // -------------------------------------------------------------------
        // С селектором
        // -------------------------------------------------------------------

        [Test]
        public void ObserveSum_WithSelector_SumsSelectedValues()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 10));

            var totalLevel = source.ObserveSum(p => p.Level);

            Assert.That(totalLevel.Value, Is.EqualTo(15));
        }

        [Test]
        public void ObserveSum_WithSelector_EmptySource_ValueIsZero()
        {
            var source = new ObservableList<Player>();

            var totalLevel = source.ObserveSum(p => p.Level);

            Assert.That(totalLevel.Value, Is.EqualTo(0));
        }

        [Test]
        public void ObserveSum_WithSelector_NullSelector_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveSum((Func<Player, int>)null!));
        }

        [Test]
        public void ObserveSum_WithSelector_UpdateChangesSum()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var totalLevel = source.ObserveSum(x => x.Level);
            Assert.That(totalLevel.Value, Is.EqualTo(5));

            ValueChange<int>? captured = null;
            totalLevel.Changed += c => captured = c;

            p.Level = 10;
            source.Update(p);

            Assert.That(totalLevel.Value, Is.EqualTo(10));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void ObserveSum_WithSelector_UpdateWithSameValue_DoesNotRaise()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var totalLevel = source.ObserveSum(x => x.Level);

            bool raised = false;
            totalLevel.Changed += _ => raised = true;

            p.Name = "Robert";
            source.Update(p);

            Assert.That(totalLevel.Value, Is.EqualTo(5));
            Assert.That(raised, Is.False);
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var sum = source.ObserveSum();

            bool received = false;
            sum.Changed += _ => received = true;

            sum.Dispose();
            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var sum = source.ObserveSum();

            sum.Dispose();

            Assert.DoesNotThrow(() => sum.Dispose());
        }
    }
}