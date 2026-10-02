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
            Assert.That(received, Is.TypeOf<AddChange<int>>());

            var add = (AddChange<int>)received!;
            Assert.That(add.Item, Is.EqualTo(10));
            Assert.That(add.Index, Is.EqualTo(0));
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
            Assert.That(received, Is.TypeOf<UpdateChange<int>>());

            var update = (UpdateChange<int>)received!;
            Assert.That(update.Item, Is.EqualTo(10));
            Assert.That(update.Index, Is.EqualTo(0));
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
            Assert.That(received, Is.TypeOf<ResetChange<int>>());
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

        [Test]
        public void Reset_EmptyList_DoesNotRaiseChange()
        {
            var list = new ObservableList<int>();
            bool raised = false;
            list.Changed += _ => raised = true;

            bool result = list.Reset();

            Assert.That(result, Is.False);
            Assert.That(raised, Is.False);
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

            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            list.Add(1);
            list.Update(1);
            list.Replace(1, 2);
            list.Remove(2);

            Assert.That(events.Count, Is.EqualTo(4));
            Assert.That(events[0], Is.TypeOf<AddChange<int>>());
            Assert.That(events[1], Is.TypeOf<UpdateChange<int>>());
            Assert.That(events[2], Is.TypeOf<ReplaceChange<int>>());
            Assert.That(events[3], Is.TypeOf<RemoveChange<int>>());
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

        // -------------------------------------------------------------------
        // Индексы в событиях
        // -------------------------------------------------------------------

        [Test]
        public void Add_RaisesAddChangeWithIndex()
        {
            var list = new ObservableList<int>();

            AddChange<int>? received = null;
            list.Changed += c => received = (AddChange<int>)c;

            list.Add(10);
            list.Add(20);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(20));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void Remove_RaisesRemoveChangeWithIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            RemoveChange<int>? received = null;
            list.Changed += c => received = (RemoveChange<int>)c;

            list.Remove(20);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(20));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void Update_RaisesUpdateChangeWithIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);

            UpdateChange<int>? received = null;
            list.Changed += c => received = (UpdateChange<int>)c;

            list.Update(20);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(20));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void Replace_RaisesReplaceChangeWithIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);

            ReplaceChange<int>? received = null;
            list.Changed += c => received = (ReplaceChange<int>)c;

            list.Replace(20, 99);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.EqualTo(20));
            Assert.That(received.NewItem, Is.EqualTo(99));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // AddAt
        // -------------------------------------------------------------------

        [Test]
        public void AddAt_InsertsAtPosition()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(30);

            bool result = list.AddAt(1, 20);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void AddAt_RaisesAddChangeWithIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(30);

            AddChange<int>? received = null;
            list.Changed += c => received = (AddChange<int>)c;

            list.AddAt(1, 20);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(20));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void AddAt_AtEnd_IsValid()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            bool result = list.AddAt(1, 20);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void AddAt_NegativeIndex_ReturnsFalse()
        {
            var list = new ObservableList<int>();

            Assert.That(list.AddAt(-1, 10), Is.False);
            Assert.That(list.Count, Is.EqualTo(0));
        }

        [Test]
        public void AddAt_IndexBeyondEnd_ReturnsFalse()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            Assert.That(list.AddAt(5, 20), Is.False);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddAt_WithDuplicates_PreservesExactPosition()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            list.AddAt(2, 10);

            Assert.That(list, Is.EqualTo(new[] { 10, 20, 10, 10 }));
        }

        [Test]
        public void AddAt_WithDuplicates_RaisesCorrectIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            AddChange<int>? received = null;
            list.Changed += c => received = (AddChange<int>)c;

            list.AddAt(2, 10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // RemoveAt
        // -------------------------------------------------------------------

        [Test]
        public void RemoveAt_RemovesByIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            bool result = list.RemoveAt(1);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 10, 30 }));
        }

        [Test]
        public void RemoveAt_RaisesRemoveChangeWithIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);

            RemoveChange<int>? received = null;
            list.Changed += c => received = (RemoveChange<int>)c;

            list.RemoveAt(1);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(20));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_InvalidIndex_ReturnsFalse()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            Assert.That(list.RemoveAt(-1), Is.False);
            Assert.That(list.RemoveAt(5), Is.False);
            Assert.That(list.Count, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_WithDuplicates_RemovesCorrectInstance()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            list.RemoveAt(2);   // удаляем второй 10-й

            Assert.That(list, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void Remove_WithDuplicates_RemovesFirstMatch()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            bool result = list.Remove(10);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 20, 10 }));
        }

        [Test]
        public void Remove_WithDuplicates_RaisesFirstMatchIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            RemoveChange<int>? received = null;
            list.Changed += c => received = (RemoveChange<int>)c;

            list.Remove(10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        [Test]
        public void Move_Forward_MovesElement()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            bool result = list.Move(0, 2);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 20, 30, 10 }));
        }

        [Test]
        public void Move_Backward_MovesElement()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            bool result = list.Move(2, 0);

            Assert.That(result, Is.True);
            Assert.That(list, Is.EqualTo(new[] { 30, 10, 20 }));
        }

        [Test]
        public void Move_RaisesMoveChangeWithIndices()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            MoveChange<int>? received = null;
            list.Changed += c => received = (MoveChange<int>)c;

            list.Move(0, 2);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(10));
            Assert.That(received.FromIndex, Is.EqualTo(0));
            Assert.That(received.ToIndex, Is.EqualTo(2));
        }

        [Test]
        public void Move_SameIndex_ReturnsFalseAndDoesNotRaise()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            bool raised = false;
            list.Changed += _ => raised = true;

            Assert.That(list.Move(0, 0), Is.False);
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Move_InvalidIndices_ReturnFalse()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);

            Assert.That(list.Move(-1, 0), Is.False);
            Assert.That(list.Move(0, -1), Is.False);
            Assert.That(list.Move(5, 0), Is.False);
            Assert.That(list.Move(0, 5), Is.False);
            Assert.That(list, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void Move_WithDuplicates_MovesCorrectInstance()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            list.Move(2, 0);    // перемещаем второй 10-й

            Assert.That(list, Is.EqualTo(new[] { 10, 10, 20 }));
        }

        [Test]
        public void Move_Backward_RaisesCorrectIndices()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);
            list.Add(40);

            MoveChange<int>? received = null;
            list.Changed += c => received = (MoveChange<int>)c;

            list.Move(3, 1);

            Assert.That(list, Is.EqualTo(new[] { 10, 40, 20, 30 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(3));
            Assert.That(received.ToIndex, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update с дубликатами
        // -------------------------------------------------------------------

        [Test]
        public void Update_WithDuplicates_RaisesChangeWithCorrectIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            UpdateChange<int>? received = null;
            list.Changed += c => received = (UpdateChange<int>)c;

            list.Update(10);   // находит первый 10-й (индекс 0)

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));
        }

        [Test]
        public void UpdateAt_UpdatesSpecificInstance()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            UpdateChange<int>? received = null;
            list.Changed += c => received = (UpdateChange<int>)c;

            list.UpdateAt(2);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(2));
        }

        [Test]
        public void UpdateAt_InvalidIndex_ReturnsFalse()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            Assert.That(list.UpdateAt(-1), Is.False);
            Assert.That(list.UpdateAt(5), Is.False);
        }

        // -------------------------------------------------------------------
        // Replace с дубликатами
        // -------------------------------------------------------------------

        [Test]
        public void Replace_WithDuplicates_ReplacesFirstMatchWithCorrectIndex()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            ReplaceChange<int>? received = null;
            list.Changed += c => received = (ReplaceChange<int>)c;

            list.Replace(10, 99);   // заменяет первый 10-й (индекс 0)

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));
            Assert.That(list, Is.EqualTo(new[] { 99, 20, 10 }));
        }

        [Test]
        public void ReplaceAt_ReplacesSpecificInstance()
        {
            var list = new ObservableList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(10);

            ReplaceChange<int>? received = null;
            list.Changed += c => received = (ReplaceChange<int>)c;

            list.ReplaceAt(2, 99);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(2));
            Assert.That(received.OldItem, Is.EqualTo(10));
            Assert.That(received.NewItem, Is.EqualTo(99));
            Assert.That(list, Is.EqualTo(new[] { 10, 20, 99 }));
        }

        [Test]
        public void ReplaceAt_InvalidIndex_ReturnsFalse()
        {
            var list = new ObservableList<int>();
            list.Add(10);

            Assert.That(list.ReplaceAt(-1, 20), Is.False);
            Assert.That(list.ReplaceAt(5, 20), Is.False);
            Assert.That(list, Is.EqualTo(new[] { 10 }));
        }

        // -------------------------------------------------------------------
        // Batch и индексы
        // -------------------------------------------------------------------

        [Test]
        public void Batch_CollectsChangesWithIndices()
        {
            var list = new ObservableList<int>();

            BatchChange<int>? received = null;
            list.Changed += c => received = (BatchChange<int>)c;

            using (list.Batch())
            {
                list.Add(10);
                list.Add(20);
                list.AddAt(0, 5);
            }

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Changes.Count, Is.EqualTo(3));

            var add1 = (AddChange<int>)received.Changes[0];
            var add2 = (AddChange<int>)received.Changes[1];
            var addAt = (AddChange<int>)received.Changes[2];

            Assert.That(add1.Index, Is.EqualTo(0));
            Assert.That(add2.Index, Is.EqualTo(1));
            Assert.That(addAt.Index, Is.EqualTo(0));
            Assert.That(addAt.Item, Is.EqualTo(5));
        }
    }
}

