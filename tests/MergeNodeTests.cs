using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="MergeNode{T}"/> — объединения двух источников
    /// одного типа по семантике Concat.
    /// </summary>
    [TestFixture]
    public class MergeNodeTests
    {
        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_InitializesFromBothSources()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.Merge(second);

            Assert.That(merged.Count, Is.EqualTo(4));
            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Constructor_WithEmptySources_ProducesEmptyResult()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_NullFirstSource_Throws()
        {
            var second = new ObservableList<int>();

            Assert.Throws<ArgumentNullException>(() =>
                new MergeNode<int>(null!, second));
        }

        [Test]
        public void Constructor_NullSecondSource_Throws()
        {
            var first = new ObservableList<int>();

            Assert.Throws<ArgumentNullException>(() =>
                new MergeNode<int>(first, null!));
        }

        [Test]
        public void Constructor_DoesNotRaiseEventsBeforeSubscription()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.Merge(second);

            var events = new List<Change<int>>();
            merged.Changed += e => events.Add(e);

            // события инициализации произошли до подписки
            Assert.That(events, Is.Empty);

            // новые события доходят
            first.Add(3);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_ToFirstSource_AddsItemToResult()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            var merged = first.Merge(second);

            first.Add(10);

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Add_ToSecondSource_AddsItemToResult()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            var merged = first.Merge(second);

            second.Add(20);

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(20));
        }

        [Test]
        public void Add_FromBothSources_RaisesAddEvents()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            var merged = first.Merge(second);

            var changes = new List<Change<int>>();
            merged.Changed += c => changes.Add(c);

            first.Add(10);
            second.Add(20);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[0].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(changes[0].Item, Is.EqualTo(10));
            Assert.That(changes[1].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(changes[1].Item, Is.EqualTo(20));
        }

        [Test]
        public void Add_DuplicatesFromBothSources_ArePreserved()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.Merge(second);

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged.Count(x => x == 10), Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_FromFirstSource_RemovesItem()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(20);

            var merged = first.Merge(second);

            bool removed = first.Remove(10);

            Assert.That(removed, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(20));
        }

        [Test]
        public void Remove_FromSecondSource_RemovesItem()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(20);

            var merged = first.Merge(second);

            bool removed = second.Remove(20);

            Assert.That(removed, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Remove_RaisesRemoveEvent()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(10);

            var merged = first.Merge(second);

            Change<int>? received = null;
            merged.Changed += c => received = c;

            first.Remove(10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void Remove_OneDuplicate_KeepsOtherOccurrence()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.Merge(second);

            first.Remove(10);

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Remove_DuplicateEquals_RemovesCorrectSource()
        {
            // Проверяет, что Remove различает источники:
            // если оба источника содержат равные по Equals элементы,
            // удаление из одного не должно задевать элемент из другого.
            var a = new ObservableList<Player>();
            var b = new ObservableList<Player>();

            var q = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1);
            b.Add(q);

            var m = a.Merge(b);

            var p = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1); // Equals(q)
            a.Add(p);

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.SameAs(q));
            Assert.That(m[1], Is.SameAs(p));

            a.Remove(p);

            Assert.That(m.Count, Is.EqualTo(1));
            Assert.That(m[0], Is.SameAs(q));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_InFirstSource_RaisesUpdateEvent()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var item = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            first.Add(item);

            var merged = first.Merge(second);

            Change<Player>? received = null;
            merged.Changed += c => received = c;

            item.Name = "New";
            bool updated = first.Update(item);

            Assert.That(updated, Is.True);
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(item));
            Assert.That(merged[0], Is.SameAs(item));
            Assert.That(merged[0].Name, Is.EqualTo("New"));
        }

        [Test]
        public void Update_InSecondSource_RaisesUpdateEvent()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var item = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            second.Add(item);

            var merged = first.Merge(second);

            Change<Player>? received = null;
            merged.Changed += c => received = c;

            item.Name = "New";
            second.Update(item);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(item));
            Assert.That(merged[0].Name, Is.EqualTo("New"));
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        [Test]
        public void Replace_InFirstSource_ReplacesItem()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var oldItem = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            var newItem = TestData.CreatePlayer(id: 2, name: "New", teamId: 10, level: 1);

            first.Add(oldItem);

            var merged = first.Merge(second);

            bool replaced = first.Replace(oldItem, newItem);

            Assert.That(replaced, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.SameAs(newItem));
        }

        [Test]
        public void Replace_RaisesReplaceEvent()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var oldItem = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            var newItem = TestData.CreatePlayer(id: 2, name: "New", teamId: 10, level: 1);

            first.Add(oldItem);

            var merged = first.Merge(second);

            Change<Player>? received = null;
            merged.Changed += c => received = c;

            first.Replace(oldItem, newItem);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Replace));
            Assert.That(received.OldItem, Is.SameAs(oldItem));
            Assert.That(received.Item, Is.SameAs(newItem));
        }

        [Test]
        public void Replace_InFirstSource_DoesNotAffectSecondSourceItem()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();

            a.Add(1);
            b.Add(1);

            var m = a.Merge(b);

            a.Replace(1, 10);

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.EqualTo(10));
            Assert.That(m[1], Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_FirstSource_KeepsSecondSourceItems()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.Merge(second);

            first.Reset();

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged, Is.EqualTo(new[] { 3, 4 }));
        }

        [Test]
        public void Reset_SecondSource_KeepsFirstSourceItems()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.Merge(second);

            second.Reset();

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Reset_FirstSource_RaisesResetThenAdds()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.Merge(second);

            var events = new List<Change<int>>();
            merged.Changed += e => events.Add(e);

            first.Reset();

            Assert.That(events.Select(e => e.Type),
                Is.EqualTo(new[] { ChangeType.Reset, ChangeType.Add }));
            Assert.That(events[1].Item, Is.EqualTo(2));
        }

        [Test]
        public void Reset_BothSources_EndsEmpty()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.Merge(second);

            first.Reset();
            second.Reset();

            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_OnEmptyMerge_DoesNotRaise()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);

            bool raised = false;
            merged.Changed += _ => raised = true;

            first.Reset();

            Assert.That(raised, Is.False);
        }

        [Test]
        public void Reset_AllowsFurtherChanges()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.Merge(second);

            first.Reset();
            first.Add(3);
            second.Add(4);

            Assert.That(merged.Count, Is.EqualTo(3));
            Assert.That(merged, Does.Contain(2));
            Assert.That(merged, Does.Contain(3));
            Assert.That(merged, Does.Contain(4));
        }

        // -------------------------------------------------------------------
        // Использование как источник
        // -------------------------------------------------------------------

        [Test]
        public void Merge_UsedAsSourceForFilter_Works()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.Merge(second);
            var positive = merged.Filter(x => x > 0);

            first.Add(-1);
            first.Add(10);
            second.Add(-2);
            second.Add(20);

            Assert.That(positive.Count, Is.EqualTo(2));
            Assert.That(positive, Is.EqualTo(new[] { 10, 20 }));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromBothSources()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();
            var merge = a.Merge(b);

            bool received = false;
            merge.Changed += _ => received = true;

            merge.Dispose();

            a.Add(1);
            b.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();
            var merge = a.Merge(b);

            merge.Dispose();

            Assert.DoesNotThrow(() => merge.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSources()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();
            var merge = a.Merge(b);

            merge.Dispose();

            Assert.DoesNotThrow(() => a.Add(1));
            Assert.DoesNotThrow(() => b.Add(2));
            Assert.That(a.Count, Is.EqualTo(1));
            Assert.That(b.Count, Is.EqualTo(1));
        }
    }
}