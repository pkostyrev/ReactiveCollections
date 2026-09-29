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

            // Player.Equals по Id — оба элемента равны
            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);

            source.Add(p1);
            source.Add(p2);

            var alive = source.Filter(p => p.Level > 0);
            Assert.That(alive.Count, Is.EqualTo(2));

            // source.Remove(p2) удалит первый равный — p1.
            // Фильтр получит Remove с Item = p2 и повторит семантику source:
            // тоже удалит первый равный из своей выдачи — p1.
            source.Remove(p2);

            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0], Is.SameAs(p2));
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

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(knight));
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

        // -------------------------------------------------------------------
        // Replace: все четыре комбинации old/new
        // -------------------------------------------------------------------

        [Test]
        public void Replace_OldInFilter_NewValid_ReplacesInPlace()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);
            Assert.That(node.Count, Is.EqualTo(1));

            source.Replace(1, 3);

            Assert.That(node.Count, Is.EqualTo(1));
            Assert.That(node[0], Is.EqualTo(3));
        }

        [Test]
        public void Replace_OldInFilter_NewInvalid_Removes()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(1, 2);

            Assert.That(node.Count, Is.EqualTo(0));
        }

        [Test]
        public void Replace_OldNotInFilter_NewValid_Adds()
        {
            var source = new ObservableList<int>();
            source.Add(2);

            var node = source.Filter(x => x % 2 == 1);
            Assert.That(node.Count, Is.EqualTo(0));

            source.Replace(2, 3);

            Assert.That(node.Count, Is.EqualTo(1));
            Assert.That(node[0], Is.EqualTo(3));
        }

        [Test]
        public void Replace_OldNotInFilter_NewInvalid_DoesNothing()
        {
            var source = new ObservableList<int>();
            source.Add(2);

            var node = source.Filter(x => x % 2 == 1);

            source.Replace(2, 4);

            Assert.That(node.Count, Is.EqualTo(0));
        }

        [Test]
        public void Replace_RaisesExpectedEventType()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Filter(x => x % 2 == 1);

            Change<int>? received = null;
            node.Changed += c => received = c;

            source.Replace(1, 3);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Replace));
            Assert.That(received.OldItem, Is.EqualTo(1));
            Assert.That(received.Item, Is.EqualTo(3));
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
