using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="MinNode{TNumber}"/> и
    /// <see cref="MinWithSelectorNode{TSource, TNumber}"/>.
    /// </summary>
    [TestFixture]
    public class MinNodeTests
    {
        // -------------------------------------------------------------------
        // Без селектора
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<int>();

            var min = source.ObserveMin();

            Assert.That(min.Value, Is.Null);
        }

        [Test]
        public void Constructor_NonEmptySource_ValueIsMinimum()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            source.Add(2);
            source.Add(8);

            var min = source.ObserveMin();

            Assert.That(min.Value, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveMin((IObservableList<int>)null!));
        }

        [Test]
        public void Add_SmallerItem_UpdatesAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var min = source.ObserveMin();

            ValueChange<int?>? captured = null;
            min.Changed += c => captured = c;

            source.Add(2);

            Assert.That(min.Value, Is.EqualTo(2));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(5));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(2));
        }

        [Test]
        public void Add_LargerItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var min = source.ObserveMin();

            bool raised = false;
            min.Changed += _ => raised = true;

            source.Add(10);

            Assert.That(min.Value, Is.EqualTo(5));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Remove_MinimumItem_Raises()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(5);
            var min = source.ObserveMin();

            ValueChange<int?>? captured = null;
            min.Changed += c => captured = c;

            source.Remove(2);

            Assert.That(min.Value, Is.EqualTo(5));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Remove_LastItem_RaisesToNull()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var min = source.ObserveMin();

            ValueChange<int?>? captured = null;
            min.Changed += c => captured = c;

            source.Remove(5);

            Assert.That(min.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(5));
            Assert.That(captured!.Value.NewValue, Is.Null);
        }

        [Test]
        public void Update_ChangingToSmallerValue_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var minLevel = source.ObserveMin(x => x.Level);
            Assert.That(minLevel.Value, Is.EqualTo(5));

            ValueChange<int?>? captured = null;
            minLevel.Changed += c => captured = c;

            p.Level = 3;
            source.Update(p);

            Assert.That(minLevel.Value, Is.EqualTo(3));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Reset_ClearsToNullAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            source.Add(2);
            var min = source.ObserveMin();

            ValueChange<int?>? captured = null;
            min.Changed += c => captured = c;

            source.Reset();

            Assert.That(min.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void ObserveMin_Long_Works()
        {
            var source = new ObservableList<long>();
            source.Add(1_000_000_000L);
            source.Add(2_000_000_000L);

            var min = source.ObserveMin();

            Assert.That(min.Value, Is.EqualTo(1_000_000_000L));
        }

        [Test]
        public void ObserveMin_Double_Works()
        {
            var source = new ObservableList<double>();
            source.Add(0.5);
            source.Add(0.25);

            var min = source.ObserveMin();

            Assert.That(min.Value, Is.EqualTo(0.25).Within(1e-10));
        }

        [Test]
        public void ObserveMin_Decimal_Works()
        {
            var source = new ObservableList<decimal>();
            source.Add(1.1m);
            source.Add(2.2m);

            var min = source.ObserveMin();

            Assert.That(min.Value, Is.EqualTo(1.1m));
        }

        // -------------------------------------------------------------------
        // С селектором
        // -------------------------------------------------------------------

        [Test]
        public void ObserveMin_WithSelector_FindsMin()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 10));

            var minLevel = source.ObserveMin(p => p.Level);

            Assert.That(minLevel.Value, Is.EqualTo(5));
        }

        [Test]
        public void ObserveMin_WithSelector_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<Player>();

            var minLevel = source.ObserveMin(p => p.Level);

            Assert.That(minLevel.Value, Is.Null);
        }

        [Test]
        public void ObserveMin_WithSelector_NullSelector_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveMin((Func<Player, int>)null!));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var min = source.ObserveMin();

            bool received = false;
            min.Changed += _ => received = true;

            min.Dispose();
            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var min = source.ObserveMin();

            min.Dispose();

            Assert.DoesNotThrow(() => min.Dispose());
        }
    }

    /// <summary>
    /// Тесты <see cref="MaxNode{TNumber}"/> и
    /// <see cref="MaxWithSelectorNode{TSource, TNumber}"/>.
    /// </summary>
    [TestFixture]
    public class MaxNodeTests
    {
        [Test]
        public void Constructor_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<int>();

            var max = source.ObserveMax();

            Assert.That(max.Value, Is.Null);
        }

        [Test]
        public void Constructor_NonEmptySource_ValueIsMaximum()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            source.Add(2);
            source.Add(8);

            var max = source.ObserveMax();

            Assert.That(max.Value, Is.EqualTo(8));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveMax((IObservableList<int>)null!));
        }

        [Test]
        public void Add_LargerItem_UpdatesAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var max = source.ObserveMax();

            ValueChange<int?>? captured = null;
            max.Changed += c => captured = c;

            source.Add(10);

            Assert.That(max.Value, Is.EqualTo(10));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(5));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(10));
        }

        [Test]
        public void Add_SmallerItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var max = source.ObserveMax();

            bool raised = false;
            max.Changed += _ => raised = true;

            source.Add(2);

            Assert.That(max.Value, Is.EqualTo(5));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Remove_MaximumItem_Raises()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(8);
            var max = source.ObserveMax();

            ValueChange<int?>? captured = null;
            max.Changed += c => captured = c;

            source.Remove(8);

            Assert.That(max.Value, Is.EqualTo(2));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Remove_LastItem_RaisesToNull()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var max = source.ObserveMax();

            ValueChange<int?>? captured = null;
            max.Changed += c => captured = c;

            source.Remove(5);

            Assert.That(max.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Reset_ClearsToNullAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            source.Add(2);
            var max = source.ObserveMax();

            ValueChange<int?>? captured = null;
            max.Changed += c => captured = c;

            source.Reset();

            Assert.That(max.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void ObserveMax_Long_Works()
        {
            var source = new ObservableList<long>();
            source.Add(1_000_000_000L);
            source.Add(2_000_000_000L);

            var max = source.ObserveMax();

            Assert.That(max.Value, Is.EqualTo(2_000_000_000L));
        }

        [Test]
        public void ObserveMax_Decimal_Works()
        {
            var source = new ObservableList<decimal>();
            source.Add(1.1m);
            source.Add(2.2m);

            var max = source.ObserveMax();

            Assert.That(max.Value, Is.EqualTo(2.2m));
        }

        // -------------------------------------------------------------------
        // С селектором
        // -------------------------------------------------------------------

        [Test]
        public void ObserveMax_WithSelector_FindsMax()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 10));

            var maxLevel = source.ObserveMax(p => p.Level);

            Assert.That(maxLevel.Value, Is.EqualTo(10));
        }

        [Test]
        public void ObserveMax_WithSelector_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<Player>();

            var maxLevel = source.ObserveMax(p => p.Level);

            Assert.That(maxLevel.Value, Is.Null);
        }

        [Test]
        public void ObserveMax_WithSelector_NullSelector_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveMax((Func<Player, int>)null!));
        }

        [Test]
        public void ObserveMax_WithSelector_UpdateChangingMax_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var maxLevel = source.ObserveMax(x => x.Level);
            Assert.That(maxLevel.Value, Is.EqualTo(5));

            ValueChange<int?>? captured = null;
            maxLevel.Changed += c => captured = c;

            p.Level = 20;
            source.Update(p);

            Assert.That(maxLevel.Value, Is.EqualTo(20));
            Assert.That(captured, Is.Not.Null);
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var max = source.ObserveMax();

            bool received = false;
            max.Changed += _ => received = true;

            max.Dispose();
            source.Add(1);

            Assert.That(received, Is.False);
        }
    }
}