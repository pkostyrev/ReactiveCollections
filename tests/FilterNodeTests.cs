using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="FilterNode{T}"/> — проекции, пропускающей
    /// только элементы, удовлетворяющие предикату.
    /// </summary>
    [TestFixture]
    public class FilterNodeTests
    {
        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_InitializesFromExistingSource()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 15));
            source.Add(TestData.CreatePlayer(id: 2, name: "Ghost", teamId: 20, level: 0));

            var alive = source.Filter(p => p.Level > 0);

            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0].Name, Is.EqualTo("Knight"));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_MatchingItem_Appears()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            source.Add(TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 15));

            Assert.That(alive.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NotMatchingItem_DoesNotAppear()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            source.Add(TestData.CreatePlayer(id: 1, name: "Ghost", teamId: 20, level: 0));

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_MatchingItem_PreservesReference()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var knight = TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 15);
            source.Add(knight);

            Assert.That(alive[0], Is.SameAs(knight));
        }

        // -------------------------------------------------------------------
        // AddAt
        // -------------------------------------------------------------------

        [Test]
        public void AddAt_InsertsAtCorrectFilterIndex()
        {
            var source = new ObservableList<int>();
            source.Add(1);   // валиден
            source.Add(2);   // невалиден
            source.Add(3);   // валиден
            // source: [1, 2, 3], filter: [1, 3]

            var odd = source.Filter(x => x % 2 == 1);

            source.AddAt(1, 5);
            // source: [1, 5, 2, 3], filter: [1, 5, 3]

            Assert.That(odd, Is.EqualTo(new[] { 1, 5, 3 }));
        }

        [Test]
        public void AddAt_InvalidItem_DoesNotAppear()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(3);

            var odd = source.Filter(x => x % 2 == 1);

            source.AddAt(1, 2);

            Assert.That(odd, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void AddAt_WithDuplicates_InsertsAtExactPosition()
        {
            var source = new ObservableList<int>();
            source.Add(10);
            source.Add(20);
            source.Add(10);

            var filter = source.Filter(x => x > 0);

            source.AddAt(2, 10);
            // source: [10, 20, 10, 10], filter: то же

            Assert.That(filter, Is.EqualTo(new[] { 10, 20, 10, 10 }));
        }

        [Test]
        public void AddAt_WithDuplicates_RaisesCorrectIndex()
        {
            var source = new ObservableList<int>();
            source.Add(10);
            source.Add(20);
            source.Add(10);

            var filter = source.Filter(x => x > 0);

            AddChange<int>? received = null;
            filter.Changed += c => received = (AddChange<int>)c;

            source.AddAt(2, 10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(10));
            Assert.That(received.Index, Is.EqualTo(2));
        }

        [Test]
        public void AddAt_WithInvalidBefore_TranslatesFilterIndex()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);    // невалиден (чётное)
            source.Add(3);
            // source: [1, 2, 3], filter: [1, 3]

            var odd = source.Filter(x => x % 2 == 1);

            AddChange<int>? received = null;
            odd.Changed += c => received = (AddChange<int>)c;

            source.AddAt(2, 5);
            // source: [1, 2, 5, 3], filter: [1, 5, 3]

            Assert.That(odd, Is.EqualTo(new[] { 1, 5, 3 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.EqualTo(5));
            Assert.That(received.Index, Is.EqualTo(1));   // filter-index, не source-index (2)
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_MatchingItem_IsRemoved()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var knight = TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 15);
            source.Add(knight);

            source.Remove(knight);

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_NotMatchingItem_DoesNothing()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var ghost = TestData.CreatePlayer(id: 1, name: "Ghost", teamId: 20, level: 0);
            source.Add(ghost);

            Assert.DoesNotThrow(() => source.Remove(ghost));
            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_DuplicateEquals_RemovesFirstMatch()
        {
            var source = new ObservableList<Player>();
            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);

            source.Add(p1);
            source.Add(p2);

            var alive = source.Filter(p => p.Level > 0);
            Assert.That(alive.Count, Is.EqualTo(2));

            // source.Remove(p2) удаляет первый равный (p1) — фильтр
            // получает RemoveChange с Index = 0 и удаляет свой первый элемент.
            source.Remove(p2);

            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0], Is.SameAs(p2));
        }

        // -------------------------------------------------------------------
        // RemoveAt
        // -------------------------------------------------------------------

        [Test]
        public void RemoveAt_RemovesCorrectInstance()
        {
            var source = new ObservableList<int>();
            source.Add(10);
            source.Add(20);
            source.Add(10);

            var filter = source.Filter(x => x > 0);

            source.RemoveAt(2);   // удаляем второй 10-й

            Assert.That(filter, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void RemoveAt_InvalidElement_DoesNotAffectFilter()
        {
            var source = new ObservableList<int>();
            source.Add(1);   // валиден
            source.Add(2);   // невалиден
            source.Add(3);   // валиден

            var odd = source.Filter(x => x % 2 == 1);

            source.RemoveAt(1);   // удаляем невалидный

            Assert.That(odd, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void RemoveAt_WithInvalidBefore_TranslatesFilterIndex()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);    // невалиден
            source.Add(3);
            // source: [1, 2, 3], filter: [1, 3]

            var odd = source.Filter(x => x % 2 == 1);

            source.RemoveAt(2);   // удаляем 3 (source index 2)
            // source: [1, 2], filter: [1]

            Assert.That(odd, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void RemoveAt_WithDuplicateAfterInvalid_RemovesCorrectInstance()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);    // невалиден
            source.Add(1);
            // source: [1, 2, 1], filter: [1, 1]

            var odd = source.Filter(x => x % 2 == 1);

            source.RemoveAt(2);   // удаляем второй 1 (source index 2)
            // source: [1, 2], filter: [1]

            Assert.That(odd, Is.EqualTo(new[] { 1 }));
            Assert.That(odd.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_ItemStartsMatching_IsAdded()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var ghost = TestData.CreatePlayer(id: 1, name: "Ghost", teamId: 20, level: 0);
            source.Add(ghost);

            ghost.Level = 50;
            source.Update(ghost);

            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0], Is.SameAs(ghost));
        }

        [Test]
        public void Update_ItemStopsMatching_IsRemoved()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var knight = TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 100);
            source.Add(knight);

            knight.Level = 0;
            source.Update(knight);

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_ItemStillMatching_RaisesUpdate()
        {
            var source = new ObservableList<Player>();
            var alive = source.Filter(p => p.Level > 0);

            var knight = TestData.CreatePlayer(id: 1, name: "Knight", teamId: 10, level: 100);
            source.Add(knight);

            Change<Player>? received = null;
            alive.Changed += c => received = c;

            knight.Level = 80;
            source.Update(knight);

            Assert.That(received, Is.TypeOf<UpdateChange<Player>>());
            Assert.That(((UpdateChange<Player>)received!).Item, Is.SameAs(knight));
        }

        [Test]
        public void Update_NotInFilter_StillNotMatching_DoesNothing()
        {
            var source = new ObservableList<int>();
            var node = source.Filter(x => x % 2 == 1);

            source.Add(2);

            bool raised = false;
            node.Changed += _ => raised = true;

            source.Update(2);

            Assert.That(raised, Is.False);
            Assert.That(node.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_WithDuplicates_UpdatesCorrectInstance()
        {
            var source = new ObservableList<int>();
            source.Add(10);
            source.Add(20);
            source.Add(10);

            var filter = source.Filter(x => x > 0);

            UpdateChange<int>? received = null;
            filter.Changed += c => received = (UpdateChange<int>)c;

            source.UpdateAt(2);   // обновляем третий элемент (второй 10)

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(2));   // filter-index
        }

        [Test]
        public void UpdateAt_WithInvalidBefore_TranslatesFilterIndex()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);    // невалиден
            source.Add(3);
            // source: [1, 2, 3], filter: [1, 3]

            var odd = source.Filter(x => x % 2 == 1);

            UpdateChange<int>? received = null;
            odd.Changed += c => received = (UpdateChange<int>)c;

            source.UpdateAt(2);   // обновляем 3-й элемент (source index 2)
            // filter-index этого элемента — 1

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        [Test]
        public void Replace_ValidToValid_ReplacesInPlace()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(1, 3);

            Assert.That(node.Count, Is.EqualTo(1));
            Assert.That(node[0], Is.EqualTo(3));
        }

        [Test]
        public void Replace_ValidToInvalid_Removes()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(1, 2);

            Assert.That(node.Count, Is.EqualTo(0));
        }

        [Test]
        public void Replace_InvalidToValid_Adds()
        {
            var source = new ObservableList<int>();
            source.Add(2);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(2, 3);

            Assert.That(node.Count, Is.EqualTo(1));
            Assert.That(node[0], Is.EqualTo(3));
        }

        [Test]
        public void Replace_InvalidToInvalid_DoesNothing()
        {
            var source = new ObservableList<int>();
            source.Add(2);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(2, 4);

            Assert.That(node.Count, Is.EqualTo(0));
        }

        [Test]
        public void Replace_ValidToValid_RaisesReplaceChange()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);

            Change<int>? received = null;
            node.Changed += c => received = c;

            source.Replace(1, 3);

            Assert.That(received, Is.TypeOf<ReplaceChange<int>>());

            var replace = (ReplaceChange<int>)received!;
            Assert.That(replace.OldItem, Is.EqualTo(1));
            Assert.That(replace.NewItem, Is.EqualTo(3));
        }

        [Test]
        public void Replace_WithInvalidBefore_TranslatesFilterIndex()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);    // невалиден
            source.Add(3);
            // source: [1, 2, 3], filter: [1, 3]

            var odd = source.Filter(x => x % 2 == 1);

            ReplaceChange<int>? received = null;
            odd.Changed += c => received = (ReplaceChange<int>)c;

            source.ReplaceAt(2, 5);
            // source: [1, 2, 5], filter: [1, 5]

            Assert.That(odd, Is.EqualTo(new[] { 1, 5 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(1));   // filter-index
            Assert.That(received.OldItem, Is.EqualTo(3));
            Assert.That(received.NewItem, Is.EqualTo(5));
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        [Test]
        public void Move_AllValid_TranslatesIndices()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(3);
            source.Add(5);

            var odd = source.Filter(x => x % 2 == 1);

            MoveChange<int>? received = null;
            odd.Changed += c => received = (MoveChange<int>)c;

            source.Move(0, 2);

            Assert.That(odd, Is.EqualTo(new[] { 3, 5, 1 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(0));
            Assert.That(received.ToIndex, Is.EqualTo(2));
            Assert.That(received.Item, Is.EqualTo(1));
        }

        [Test]
        public void Move_InvalidItemMoved_DoesNotChangeFilter()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);   // невалиден
            source.Add(3);

            var odd = source.Filter(x => x % 2 == 1);

            bool raised = false;
            odd.Changed += _ => raised = true;

            source.Move(1, 2);

            Assert.That(odd, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Move_ThroughInvalidElement_TranslatesFilterIndices()
        {
            var source = new ObservableList<int>();
            source.Add(1);   // валиден (source 0, filter 0)
            source.Add(2);   // невалиден
            source.Add(3);   // валиден (source 2, filter 1)
            source.Add(5);   // валиден (source 3, filter 2)

            var odd = source.Filter(x => x % 2 == 1);

            MoveChange<int>? received = null;
            odd.Changed += c => received = (MoveChange<int>)c;

            // Двигаем 5 (source 3) на source 0
            source.Move(3, 0);
            // source: [5, 1, 2, 3], filter: [5, 1, 3]

            Assert.That(odd, Is.EqualTo(new[] { 5, 1, 3 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(2));
            Assert.That(received.ToIndex, Is.EqualTo(0));
        }

        [Test]
        public void Move_BetweenInvalidElements_TranslatesCorrectly()
        {
            var source = new ObservableList<int>();
            source.Add(2);   // невалиден
            source.Add(1);   // валиден (filter 0)
            source.Add(4);   // невалиден
            source.Add(3);   // валиден (filter 1)

            var odd = source.Filter(x => x % 2 == 1);

            MoveChange<int>? received = null;
            odd.Changed += c => received = (MoveChange<int>)c;

            source.Move(3, 1);
            // source: [2, 3, 1, 4], filter: [3, 1]

            Assert.That(odd, Is.EqualTo(new[] { 3, 1 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(1));
            Assert.That(received.ToIndex, Is.EqualTo(0));
        }

        [Test]
        public void Move_WithDuplicates_TranslatesCorrectInstance()
        {
            var source = new ObservableList<int>();
            source.Add(10);
            source.Add(20);
            source.Add(10);

            var filter = source.Filter(x => x > 0);

            MoveChange<int>? received = null;
            filter.Changed += c => received = (MoveChange<int>)c;

            source.Move(2, 0);

            Assert.That(filter, Is.EqualTo(new[] { 10, 10, 20 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(2));
            Assert.That(received.ToIndex, Is.EqualTo(0));
        }

        [Test]
        public void Move_PreservesReference()
        {
            var source = new ObservableList<Player>();
            var p1 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 100);
            var p2 = TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 100);
            source.Add(p1);
            source.Add(p2);

            var alive = source.Filter(p => p.Level > 0);
            var originalP1 = alive[0];

            source.Move(0, 1);

            Assert.That(alive[0], Is.SameAs(p2));
            Assert.That(alive[1], Is.SameAs(originalP1));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsFilteredItems()
        {
            var source = new ObservableList<Player>();
            var filter = source.Filter(p => p.Level >= 10);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 20));

            Assert.That(filter.Count, Is.EqualTo(2));

            source.Reset();

            Assert.That(filter.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_AllowsFurtherAdds()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(3);

            var odd = source.Filter(x => x % 2 == 1);

            source.Reset();
            source.Add(5);

            Assert.That(odd, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void Reset_RaisesOnlyResetChange()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            var odd = source.Filter(x => x % 2 == 1);
            Assert.That(odd, Is.EqualTo(new[] { 1, 3 }));

            var events = new List<Change<int>>();
            odd.Changed += c => events.Add(c);

            source.Reset();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ResetChange<int>>());
        }

        // -------------------------------------------------------------------
        // Цепочки
        // -------------------------------------------------------------------

        [Test]
        public void Chain_FilterSelect_MovePreservesViewReference()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 100));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 100));
            source.Add(TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 100));

            var views = source
                .Filter(p => p.Level > 0)
                .Select(p => new PlayerView(), TestData.Bind);

            var viewA = views[0];
            var viewB = views[1];
            var viewC = views[2];

            source.Move(2, 0);

            Assert.That(views[0], Is.SameAs(viewC));
            Assert.That(views[1], Is.SameAs(viewA));
            Assert.That(views[2], Is.SameAs(viewB));
        }

        [Test]
        public void Chain_FilterWithInvalidBetween_MovePreservesViewReference()
        {
            var source = new ObservableList<Player>();
            var p1 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 100);
            var pDead = TestData.CreatePlayer(id: 2, name: "Dead", teamId: 10, level: 0);
            var p2 = TestData.CreatePlayer(id: 3, name: "B", teamId: 10, level: 100);

            source.Add(p1);
            source.Add(pDead);
            source.Add(p2);

            var views = source
                .Filter(p => p.Level > 0)
                .Select(p => new PlayerView(), TestData.Bind);

            var viewA = views[0];
            var viewB = views[1];

            source.Move(2, 0);
            // source: [B, A, Dead], views: [B, A]

            Assert.That(views[0], Is.SameAs(viewB));
            Assert.That(views[1], Is.SameAs(viewA));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<int>();
            var filter = source.Filter(x => x > 0);

            bool received = false;
            filter.Changed += _ => received = true;

            filter.Dispose();

            source.Add(1);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<int>();
            var filter = source.Filter(x => true);

            filter.Dispose();

            Assert.DoesNotThrow(() => filter.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<int>();
            var filter = source.Filter(x => true);

            filter.Dispose();

            Assert.DoesNotThrow(() => source.Add(1));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}
