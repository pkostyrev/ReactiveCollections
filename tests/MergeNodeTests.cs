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
        public void Constructor_ConcatOrder_FirstThenSecond()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [Test]
        public void Constructor_PreservesReferences()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var a = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1);
            var b = TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 1);
            var c = TestData.CreatePlayer(id: 3, name: "C", teamId: 20, level: 1);

            first.Add(a);
            first.Add(b);
            second.Add(c);

            var merged = first.ObserveMerge(second);

            Assert.That(merged[0], Is.SameAs(a));
            Assert.That(merged[1], Is.SameAs(b));
            Assert.That(merged[2], Is.SameAs(c));
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

            var merged = first.ObserveMerge(second);

            var events = new List<Change<int>>();
            merged.Changed += e => events.Add(e);

            // события инициализации произошли до подписки
            Assert.That(events, Is.Empty);

            // новые события доходят
            first.Add(3);
            Assert.That(events.Count, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_PreservesDuplicatesAcrossSources()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.ObserveMerge(second);

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged[0], Is.EqualTo(10));
            Assert.That(merged[1], Is.EqualTo(10));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_ToFirst_AppendsBeforeSecondBlock()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            AddChange<int>? received = null;
            merged.Changed += c => received = (AddChange<int>)c;

            first.Add(10);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 10, 3, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(10));
            Assert.That(received.Index, Is.EqualTo(2));
        }

        [Test]
        public void Add_ToSecond_AppendsAtEnd()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);

            var merged = first.ObserveMerge(second);

            AddChange<int>? received = null;
            merged.Changed += c => received = (AddChange<int>)c;

            second.Add(10);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 10 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(3));
        }

        [Test]
        public void Add_FromBothSources_RaisesAddEvents()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            var merged = first.ObserveMerge(second);

            var changes = new List<Change<int>>();
            merged.Changed += c => changes.Add(c);

            first.Add(10);
            second.Add(20);

            Assert.That(changes.Count, Is.EqualTo(2));
            Assert.That(changes[0], Is.TypeOf<AddChange<int>>());
            Assert.That(((AddChange<int>)changes[0]).Item, Is.EqualTo(10));
            Assert.That(changes[1], Is.TypeOf<AddChange<int>>());
            Assert.That(((AddChange<int>)changes[1]).Item, Is.EqualTo(20));
        }

        // -------------------------------------------------------------------
        // AddAt
        // -------------------------------------------------------------------

        [Test]
        public void AddAt_First_UsesSourceIndexDirectly()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            AddChange<int>? received = null;
            merged.Changed += c => received = (AddChange<int>)c;

            first.AddAt(1, 10);

            Assert.That(merged, Is.EqualTo(new[] { 1, 10, 2, 3, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(1));
            Assert.That(received.Item, Is.EqualTo(10));
        }

        [Test]
        public void AddAt_Second_ShiftsByFirstCount()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            AddChange<int>? received = null;
            merged.Changed += c => received = (AddChange<int>)c;

            second.AddAt(1, 10);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 10, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(3));
        }

        [Test]
        public void Add_DuplicatesFromBothSources_ArePreserved()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.ObserveMerge(second);

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged.Count(x => x == 10), Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // Remove / RemoveAt
        // -------------------------------------------------------------------

        [Test]
        public void Remove_FromFirstSource_RemovesItem()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(20);

            var merged = first.ObserveMerge(second);

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

            var merged = first.ObserveMerge(second);

            bool removed = second.Remove(20);

            Assert.That(removed, Is.True);
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void RemoveAt_First_RaisesMergedIndex()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            RemoveChange<int>? received = null;
            merged.Changed += c => received = (RemoveChange<int>)c;

            first.RemoveAt(1);

            Assert.That(merged, Is.EqualTo(new[] { 1, 3, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(2));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_Second_RaisesMergedIndex()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            RemoveChange<int>? received = null;
            merged.Changed += c => received = (RemoveChange<int>)c;

            second.RemoveAt(1);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(4));
            Assert.That(received.Index, Is.EqualTo(3));
        }

        [Test]
        public void Remove_OneDuplicate_KeepsOtherOccurrence()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(10);
            second.Add(10);

            var merged = first.ObserveMerge(second);

            first.Remove(10);

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(10));
        }

        [Test]
        public void Remove_DuplicateEquals_FromSecond_DoesNotTouchFirst()
        {
            var a = new ObservableList<Player>();
            var b = new ObservableList<Player>();

            var p = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1);
            a.Add(p);

            var q = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1); // Equals(p)
            b.Add(q);

            var m = a.ObserveMerge(b);
            // Concat: [p, q]

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.SameAs(p));
            Assert.That(m[1], Is.SameAs(q));

            b.Remove(q);

            Assert.That(m.Count, Is.EqualTo(1));
            Assert.That(m[0], Is.SameAs(p));
        }

        [Test]
        public void Remove_DuplicateEquals_FromFirst_DoesNotTouchSecond()
        {
            var a = new ObservableList<Player>();
            var b = new ObservableList<Player>();

            var p = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1);
            a.Add(p);

            var q = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 1);
            b.Add(q);

            var m = a.ObserveMerge(b);

            a.Remove(p);

            Assert.That(m.Count, Is.EqualTo(1));
            Assert.That(m[0], Is.SameAs(q));
        }

        // -------------------------------------------------------------------
        // Update / UpdateAt
        // -------------------------------------------------------------------

        [Test]
        public void Update_InFirstSource_RaisesUpdateEvent()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var item = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            first.Add(item);

            var merged = first.ObserveMerge(second);

            UpdateChange<Player>? received = null;
            merged.Changed += c => received = (UpdateChange<Player>)c;

            item.Name = "New";
            bool updated = first.Update(item);

            Assert.That(updated, Is.True);
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(item));
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

            var merged = first.ObserveMerge(second);

            UpdateChange<Player>? received = null;
            merged.Changed += c => received = (UpdateChange<Player>)c;

            item.Name = "New";
            second.Update(item);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(item));
            Assert.That(merged[0].Name, Is.EqualTo("New"));
        }

        [Test]
        public void UpdateAt_Second_DoesNotTouchFirstDuplicate()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var b1 = TestData.CreatePlayer(id: 1, name: "B", teamId: 10, level: 1);
            var b2 = TestData.CreatePlayer(id: 1, name: "B", teamId: 10, level: 1);

            first.Add(b1);
            second.Add(b2);

            var merged = first.ObserveMerge(second);

            UpdateChange<Player>? received = null;
            merged.Changed += c => received = (UpdateChange<Player>)c;

            b2.Name = "B2";
            second.UpdateAt(0);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(b2));
            Assert.That(received.Index, Is.EqualTo(1));

            Assert.That(merged[0], Is.SameAs(b1));
            Assert.That(merged[0].Name, Is.EqualTo("B"));

            Assert.That(merged[1], Is.SameAs(b2));
            Assert.That(merged[1].Name, Is.EqualTo("B2"));
        }

        [Test]
        public void UpdateAt_First_DoesNotTouchSecondDuplicate()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var b1 = TestData.CreatePlayer(id: 1, name: "B", teamId: 10, level: 1);
            var b2 = TestData.CreatePlayer(id: 1, name: "B", teamId: 10, level: 1);

            first.Add(b1);
            second.Add(b2);

            var merged = first.ObserveMerge(second);

            UpdateChange<Player>? received = null;
            merged.Changed += c => received = (UpdateChange<Player>)c;

            b1.Name = "B1";
            first.UpdateAt(0);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));

            Assert.That(merged[0].Name, Is.EqualTo("B1"));
            Assert.That(merged[1].Name, Is.EqualTo("B"));
        }

        // -------------------------------------------------------------------
        // Replace / ReplaceAt
        // -------------------------------------------------------------------

        [Test]
        public void Replace_InFirstSource_ReplacesItem()
        {
            var first = new ObservableList<Player>();
            var second = new ObservableList<Player>();

            var oldItem = TestData.CreatePlayer(id: 1, name: "Old", teamId: 10, level: 1);
            var newItem = TestData.CreatePlayer(id: 2, name: "New", teamId: 10, level: 1);

            first.Add(oldItem);

            var merged = first.ObserveMerge(second);

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

            var merged = first.ObserveMerge(second);

            ReplaceChange<Player>? received = null;
            merged.Changed += c => received = (ReplaceChange<Player>)c;

            first.Replace(oldItem, newItem);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.SameAs(oldItem));
            Assert.That(received.NewItem, Is.SameAs(newItem));
            Assert.That(received.Index, Is.EqualTo(0));
        }

        [Test]
        public void ReplaceAt_First_RaisesReplaceChangeWithMergedIndex()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            ReplaceChange<int>? received = null;
            merged.Changed += c => received = (ReplaceChange<int>)c;

            first.ReplaceAt(1, 99);

            Assert.That(merged, Is.EqualTo(new[] { 1, 99, 3, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.EqualTo(2));
            Assert.That(received.NewItem, Is.EqualTo(99));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceAt_Second_ShiftsIndexByFirstCount()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            ReplaceChange<int>? received = null;
            merged.Changed += c => received = (ReplaceChange<int>)c;

            second.ReplaceAt(1, 99);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 99 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.EqualTo(4));
            Assert.That(received.NewItem, Is.EqualTo(99));
            Assert.That(received.Index, Is.EqualTo(3));
        }

        [Test]
        public void Replace_InFirstSource_DoesNotAffectSecondSourceItem()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();

            a.Add(1);
            b.Add(1);

            var m = a.ObserveMerge(b);

            a.Replace(1, 10);

            Assert.That(m.Count, Is.EqualTo(2));
            Assert.That(m[0], Is.EqualTo(10));
            Assert.That(m[1], Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        [Test]
        public void Move_WithinFirst_TranslatesIndices()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            first.Add(3);
            second.Add(4);
            second.Add(5);

            var merged = first.ObserveMerge(second);

            MoveChange<int>? received = null;
            merged.Changed += c => received = (MoveChange<int>)c;

            first.Move(2, 0);

            Assert.That(merged, Is.EqualTo(new[] { 3, 1, 2, 4, 5 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(3));
            Assert.That(received.FromIndex, Is.EqualTo(2));
            Assert.That(received.ToIndex, Is.EqualTo(0));
        }

        [Test]
        public void Move_WithinSecond_TranslatesIndices()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            first.Add(3);
            second.Add(4);
            second.Add(5);

            var merged = first.ObserveMerge(second);

            MoveChange<int>? received = null;
            merged.Changed += c => received = (MoveChange<int>)c;

            second.Move(1, 0);

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 3, 5, 4 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(5));
            Assert.That(received.FromIndex, Is.EqualTo(4));
            Assert.That(received.ToIndex, Is.EqualTo(3));
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

            var merged = first.ObserveMerge(second);

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

            var merged = first.ObserveMerge(second);

            second.Reset();

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged, Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void Reset_FirstSource_RaisesSingleReset()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.ObserveMerge(second);

            var events = new List<Change<int>>();
            merged.Changed += e => events.Add(e);

            first.Reset();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ResetChange<int>>());
        }

        [Test]
        public void Reset_BothSources_EndsEmpty()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            first.Add(1);
            second.Add(2);

            var merged = first.ObserveMerge(second);

            first.Reset();
            second.Reset();

            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_OnEmptyMerge_DoesNotRaise()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.ObserveMerge(second);

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

            var merged = first.ObserveMerge(second);

            first.Reset();
            first.Add(3);
            second.Add(4);

            Assert.That(merged.Count, Is.EqualTo(3));
            Assert.That(merged, Does.Contain(2));
            Assert.That(merged, Does.Contain(3));
            Assert.That(merged, Does.Contain(4));
        }

        // -------------------------------------------------------------------
        // Batch
        // -------------------------------------------------------------------

        [Test]
        public void Batch_FirstSource_AppliesInnerChangesInOrder()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            first.Add(2);
            second.Add(3);
            second.Add(4);

            var merged = first.ObserveMerge(second);

            using (first.Batch())
            {
                first.Add(10);
                first.Add(20);
            }

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 10, 20, 3, 4 }));
        }

        [Test]
        public void Batch_SecondSource_AppliesInnerChangesInOrder()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();
            first.Add(1);
            second.Add(2);

            var merged = first.ObserveMerge(second);

            using (second.Batch())
            {
                second.Add(10);
                second.Add(20);
            }

            Assert.That(merged, Is.EqualTo(new[] { 1, 2, 10, 20 }));
        }

        // -------------------------------------------------------------------
        // Использование как источник
        // -------------------------------------------------------------------

        [Test]
        public void Merge_UsedAsSourceForFilter_Works()
        {
            var first = new ObservableList<int>();
            var second = new ObservableList<int>();

            var merged = first.ObserveMerge(second);
            var positive = merged.ObserveWhere(x => x > 0);

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
            var merge = a.ObserveMerge(b);

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
            var merge = a.ObserveMerge(b);

            merge.Dispose();

            Assert.DoesNotThrow(() => merge.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSources()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();
            var merge = a.ObserveMerge(b);

            merge.Dispose();

            Assert.DoesNotThrow(() => a.Add(1));
            Assert.DoesNotThrow(() => b.Add(2));
            Assert.That(a.Count, Is.EqualTo(1));
            Assert.That(b.Count, Is.EqualTo(1));
        }
    }
}