using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="AverageNode{TNumber, TAccumulate, TResult}"/> и
    /// <see cref="AverageWithSelectorNode{TSource, TNumber, TAccumulate, TResult}"/>.
    /// </summary>
    [TestFixture]
    public class AverageNodeTests
    {
        // -------------------------------------------------------------------
        // Без селектора
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<int>();

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.Null);
        }

        [Test]
        public void Constructor_NonEmptySource_ValueIsAverage()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            source.Add(6);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(4.0));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveAverage((IObservableList<int>)null!));
        }

        [Test]
        public void Constructor_SingleElement_ValueIsElement()
        {
            var source = new ObservableList<int>();
            source.Add(7);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(7.0));
        }

        [Test]
        public void Add_ChangesAverageAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            var avg = source.ObserveAverage();
            Assert.That(avg.Value, Is.EqualTo(3.0));

            ValueChange<double?>? captured = null;
            avg.Changed += c => captured = c;

            source.Add(6);

            Assert.That(avg.Value, Is.EqualTo(4.0));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(3.0));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(4.0));
        }

        [Test]
        public void Remove_ChangesAverageAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            source.Add(6);
            var avg = source.ObserveAverage();

            ValueChange<double?>? captured = null;
            avg.Changed += c => captured = c;

            source.Remove(2);

            Assert.That(avg.Value, Is.EqualTo(5.0));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Remove_LastItem_RaisesToNull()
        {
            var source = new ObservableList<int>();
            source.Add(5);
            var avg = source.ObserveAverage();

            ValueChange<double?>? captured = null;
            avg.Changed += c => captured = c;

            source.Remove(5);

            Assert.That(avg.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(5.0));
            Assert.That(captured!.Value.NewValue, Is.Null);
        }

        [Test]
        public void Reset_ClearsToNullAndRaises()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            var avg = source.ObserveAverage();

            ValueChange<double?>? captured = null;
            avg.Changed += c => captured = c;

            source.Reset();

            Assert.That(avg.Value, Is.Null);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Update_WithoutValueChange_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var avg = source.ObserveAverage();

            bool raised = false;
            avg.Changed += _ => raised = true;

            source.Update(1);

            Assert.That(avg.Value, Is.EqualTo(1.5));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Add_KeepsAverageSame_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            var avg = source.ObserveAverage();

            bool raised = false;
            avg.Changed += _ => raised = true;

            // добавляем 3 — среднее остаётся 3.0
            source.Add(3);

            Assert.That(avg.Value, Is.EqualTo(3.0));
            Assert.That(raised, Is.False);
        }

        // -------------------------------------------------------------------
        // Перегрузки по типам
        // -------------------------------------------------------------------

        [Test]
        public void ObserveAverage_Long_Works()
        {
            var source = new ObservableList<long>();
            source.Add(1_000_000_000L);
            source.Add(2_000_000_000L);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(1_500_000_000.0));
        }

        [Test]
        public void ObserveAverage_Float_ReturnsFloat()
        {
            var source = new ObservableList<float>();
            source.Add(1f);
            source.Add(2f);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(1.5f).Within(1e-6f));
        }

        [Test]
        public void ObserveAverage_Double_Works()
        {
            var source = new ObservableList<double>();
            source.Add(0.5);
            source.Add(0.25);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(0.375).Within(1e-10));
        }

        [Test]
        public void ObserveAverage_Decimal_Works()
        {
            var source = new ObservableList<decimal>();
            source.Add(1.0m);
            source.Add(2.0m);

            var avg = source.ObserveAverage();

            Assert.That(avg.Value, Is.EqualTo(1.5m));
        }

        // -------------------------------------------------------------------
        // С селектором
        // -------------------------------------------------------------------

        [Test]
        public void ObserveAverage_WithSelector_AveragesSelectedValues()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 10));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 20));

            var avgLevel = source.ObserveAverage(p => p.Level);

            Assert.That(avgLevel.Value, Is.EqualTo(15.0));
        }

        [Test]
        public void ObserveAverage_WithSelector_EmptySource_ValueIsNull()
        {
            var source = new ObservableList<Player>();

            var avgLevel = source.ObserveAverage(p => p.Level);

            Assert.That(avgLevel.Value, Is.Null);
        }

        [Test]
        public void ObserveAverage_WithSelector_NullSelector_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveAverage((Func<Player, int>)null!));
        }

        [Test]
        public void ObserveAverage_WithSelector_UpdateChangesAverage()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 10);
            source.Add(p);

            var avgLevel = source.ObserveAverage(x => x.Level);
            Assert.That(avgLevel.Value, Is.EqualTo(10.0));

            ValueChange<double?>? captured = null;
            avgLevel.Changed += c => captured = c;

            p.Level = 20;
            source.Update(p);

            Assert.That(avgLevel.Value, Is.EqualTo(20.0));
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void ObserveAverage_WithSelector_UpdateToSameValue_DoesNotRaise()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 10);
            source.Add(p);

            var avgLevel = source.ObserveAverage(x => x.Level);

            bool raised = false;
            avgLevel.Changed += _ => raised = true;

            p.Name = "Robert";
            source.Update(p);

            Assert.That(avgLevel.Value, Is.EqualTo(10.0));
            Assert.That(raised, Is.False);
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var avg = source.ObserveAverage();

            bool received = false;
            avg.Changed += _ => received = true;

            avg.Dispose();
            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var avg = source.ObserveAverage();

            avg.Dispose();

            Assert.DoesNotThrow(() => avg.Dispose());
        }
    }
}