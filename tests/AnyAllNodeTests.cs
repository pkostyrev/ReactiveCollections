using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="AnyNode{T}"/> — агрегата «есть ли хотя бы один элемент».
    /// </summary>
    [TestFixture]
    public class AnyNodeTests
    {
        [Test]
        public void Constructor_EmptySource_ValueIsFalse()
        {
            var source = new ObservableList<int>();

            var any = source.ObserveAny();

            Assert.That(any.Value, Is.False);
        }

        [Test]
        public void Constructor_NonEmptySource_ValueIsTrue()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var any = source.ObserveAny();

            Assert.That(any.Value, Is.True);
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AggregateExtensions.ObserveAny<int>(null!));
        }

        [Test]
        public void Add_ToEmptyList_RaisesFalseToTrue()
        {
            var source = new ObservableList<int>();
            var any = source.ObserveAny();

            ValueChange<bool>? captured = null;
            any.Changed += c => captured = c;

            source.Add(1);

            Assert.That(any.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.False);
            Assert.That(captured!.Value.NewValue, Is.True);
        }

        [Test]
        public void Add_ToNonEmptyList_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var any = source.ObserveAny();

            bool raised = false;
            any.Changed += _ => raised = true;

            source.Add(2);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Remove_LastItem_RaisesTrueToFalse()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var any = source.ObserveAny();

            ValueChange<bool>? captured = null;
            any.Changed += c => captured = c;

            source.Remove(1);

            Assert.That(any.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.True);
            Assert.That(captured!.Value.NewValue, Is.False);
        }

        [Test]
        public void Remove_NonLastItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var any = source.ObserveAny();

            bool raised = false;
            any.Changed += _ => raised = true;

            source.Remove(1);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Update_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var any = source.ObserveAny();

            bool raised = false;
            any.Changed += _ => raised = true;

            source.Update(1);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Replace_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var any = source.ObserveAny();

            bool raised = false;
            any.Changed += _ => raised = true;

            source.Replace(1, 2);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Reset_NonEmptyList_RaisesTrueToFalse()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var any = source.ObserveAny();

            ValueChange<bool>? captured = null;
            any.Changed += c => captured = c;

            source.Reset();

            Assert.That(any.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.True);
            Assert.That(captured!.Value.NewValue, Is.False);
        }

        [Test]
        public void Reset_EmptyList_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            var any = source.ObserveAny();

            bool raised = false;
            any.Changed += _ => raised = true;

            source.Reset();

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var any = source.ObserveAny();

            bool received = false;
            any.Changed += _ => received = true;

            any.Dispose();
            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var any = source.ObserveAny();

            any.Dispose();

            Assert.DoesNotThrow(() => any.Dispose());
        }
    }

    /// <summary>
    /// Тесты <see cref="AnyWithPredicateNode{T}"/> — агрегата
    /// «есть ли элемент, удовлетворяющий предикату».
    /// </summary>
    [TestFixture]
    public class AnyWithPredicateNodeTests
    {
        [Test]
        public void Constructor_NoMatch_ValueIsFalse()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);

            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            Assert.That(anyOdd.Value, Is.False);
        }

        [Test]
        public void Constructor_HasMatch_ValueIsTrue()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(3);

            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            Assert.That(anyOdd.Value, Is.True);
        }

        [Test]
        public void Constructor_EmptySource_ValueIsFalse()
        {
            var source = new ObservableList<int>();

            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            Assert.That(anyOdd.Value, Is.False);
        }

        [Test]
        public void Constructor_NullPredicate_Throws()
        {
            var source = new ObservableList<int>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveAny((Func<int, bool>)null!));
        }

        [Test]
        public void Add_MatchingItem_RaisesFalseToTrue()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            ValueChange<bool>? captured = null;
            anyOdd.Changed += c => captured = c;

            source.Add(3);

            Assert.That(anyOdd.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.False);
            Assert.That(captured!.Value.NewValue, Is.True);
        }

        [Test]
        public void Add_NonMatchingItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            bool raised = false;
            anyOdd.Changed += _ => raised = true;

            source.Add(4);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Remove_LastMatchingItem_RaisesTrueToFalse()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(3);
            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            ValueChange<bool>? captured = null;
            anyOdd.Changed += c => captured = c;

            source.Remove(3);

            Assert.That(anyOdd.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.True);
            Assert.That(captured!.Value.NewValue, Is.False);
        }

        [Test]
        public void Remove_NonMatchingItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(2);
            source.Add(4);
            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            bool raised = false;
            anyOdd.Changed += _ => raised = true;

            source.Remove(2);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Update_ItemStartsMatching_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var hasHighLevel = source.ObserveAny(x => x.Level >= 10);
            Assert.That(hasHighLevel.Value, Is.False);

            ValueChange<bool>? captured = null;
            hasHighLevel.Changed += c => captured = c;

            p.Level = 15;
            source.Update(p);

            Assert.That(hasHighLevel.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Update_ItemStopsMatching_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(p);

            var hasHighLevel = source.ObserveAny(x => x.Level >= 10);
            Assert.That(hasHighLevel.Value, Is.True);

            ValueChange<bool>? captured = null;
            hasHighLevel.Changed += c => captured = c;

            p.Level = 5;
            source.Update(p);

            Assert.That(hasHighLevel.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Update_StillMatching_DoesNotRaise()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(p);

            var hasHighLevel = source.ObserveAny(x => x.Level >= 10);

            bool raised = false;
            hasHighLevel.Changed += _ => raised = true;

            p.Level = 20;
            source.Update(p);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var anyOdd = source.ObserveAny(x => x % 2 == 1);

            bool received = false;
            anyOdd.Changed += _ => received = true;

            anyOdd.Dispose();
            source.Add(3);

            Assert.That(received, Is.False);
        }
    }

    /// <summary>
    /// Тесты <see cref="AllNode{T}"/> — агрегата
    /// «все ли элементы удовлетворяют предикату».
    /// </summary>
    [TestFixture]
    public class AllNodeTests
    {
        [Test]
        public void Constructor_AllMatch_ValueIsTrue()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(3);

            var allOdd = source.ObserveAll(x => x % 2 == 1);

            Assert.That(allOdd.Value, Is.True);
        }

        [Test]
        public void Constructor_NotAllMatch_ValueIsFalse()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);

            var allOdd = source.ObserveAll(x => x % 2 == 1);

            Assert.That(allOdd.Value, Is.False);
        }

        [Test]
        public void Constructor_EmptySource_ValueIsTrue_VacuousTruth()
        {
            var source = new ObservableList<int>();

            var allOdd = source.ObserveAll(x => x % 2 == 1);

            Assert.That(allOdd.Value, Is.True);
        }

        [Test]
        public void Constructor_NullPredicate_Throws()
        {
            var source = new ObservableList<int>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveAll((Func<int, bool>)null!));
        }

        [Test]
        public void Add_NonMatchingItem_RaisesTrueToFalse()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var allOdd = source.ObserveAll(x => x % 2 == 1);

            ValueChange<bool>? captured = null;
            allOdd.Changed += c => captured = c;

            source.Add(2);

            Assert.That(allOdd.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.True);
            Assert.That(captured!.Value.NewValue, Is.False);
        }

        [Test]
        public void Add_MatchingItem_DoesNotRaise()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            var allOdd = source.ObserveAll(x => x % 2 == 1);

            bool raised = false;
            allOdd.Changed += _ => raised = true;

            source.Add(3);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Remove_NonMatchingItem_RaisesFalseToTrue()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var allOdd = source.ObserveAll(x => x % 2 == 1);

            ValueChange<bool>? captured = null;
            allOdd.Changed += c => captured = c;

            source.Remove(2);

            Assert.That(allOdd.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.False);
            Assert.That(captured!.Value.NewValue, Is.True);
        }

        [Test]
        public void Update_ItemStopsMatching_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(p);

            var allHighLevel = source.ObserveAll(x => x.Level >= 10);
            Assert.That(allHighLevel.Value, Is.True);

            ValueChange<bool>? captured = null;
            allHighLevel.Changed += c => captured = c;

            p.Level = 5;
            source.Update(p);

            Assert.That(allHighLevel.Value, Is.False);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Update_ItemStartsMatching_Raises()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var allHighLevel = source.ObserveAll(x => x.Level >= 10);
            Assert.That(allHighLevel.Value, Is.False);

            ValueChange<bool>? captured = null;
            allHighLevel.Changed += c => captured = c;

            p.Level = 15;
            source.Update(p);

            Assert.That(allHighLevel.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Update_StillMatching_DoesNotRaise()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(p);

            var allHighLevel = source.ObserveAll(x => x.Level >= 10);

            bool raised = false;
            allHighLevel.Changed += _ => raised = true;

            p.Level = 20;
            source.Update(p);

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Reset_ToEmpty_RaisesFalseToTrue()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            var allOdd = source.ObserveAll(x => x % 2 == 1);
            Assert.That(allOdd.Value, Is.False);

            ValueChange<bool>? captured = null;
            allOdd.Changed += c => captured = c;

            source.Reset();

            Assert.That(allOdd.Value, Is.True);
            Assert.That(captured, Is.Not.Null);
        }

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var allOdd = source.ObserveAll(x => x % 2 == 1);

            bool received = false;
            allOdd.Changed += _ => received = true;

            allOdd.Dispose();
            source.Add(2);

            Assert.That(received, Is.False);
        }
    }
}