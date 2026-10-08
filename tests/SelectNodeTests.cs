using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="SelectNode{TSource, TResult}"/>.
    /// </summary>
    [TestFixture]
    public class SelectNodeTests
    {
        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_InitializesFromExistingSource()
        {
            var source = new ObservableList<Player>();
            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[0].Text, Is.EqualTo("Bob"));
            Assert.That(views[0].TeamId, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_NullFactory_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveSelect<Player, PlayerView>(null!, TestData.Bind));
        }

        [Test]
        public void Constructor_NullUpdater_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.ObserveSelect(p => new PlayerView(), null!));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_CreatesResult()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(player.Id));
            Assert.That(views[0].Text, Is.EqualTo(player.Name));
        }

        [Test]
        public void Add_RaisesAddChange()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            AddChange<PlayerView>? received = null;
            views.Changed += c => received = (AddChange<PlayerView>)c;

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item.Text, Is.EqualTo("Bob"));
            Assert.That(received.Index, Is.EqualTo(0));
        }

        [Test]
        public void Add_DuplicateEquals_CreatesTwoResults()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            Assert.That(views.Count, Is.EqualTo(2));
            Assert.That(views[0], Is.Not.SameAs(views[1]));
        }

        [Test]
        public void Add_IdentityFactory_PreservesReference()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var node = source.ObserveSelect(x => (object)x, (_, _) => { });

            Assert.That(node[0], Is.SameAs(p));
        }

        // -------------------------------------------------------------------
        // AddAt
        // -------------------------------------------------------------------

        [Test]
        public void AddAt_InsertsResultAtCorrectPosition()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));

            source.AddAt(1, TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            Assert.That(views.Count, Is.EqualTo(3));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[1].Id, Is.EqualTo(3));
            Assert.That(views[2].Id, Is.EqualTo(2));
        }

        [Test]
        public void AddAt_RaisesAddChangeWithIndex()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));

            AddChange<PlayerView>? received = null;
            views.Changed += c => received = (AddChange<PlayerView>)c;

            source.AddAt(1, TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item.Id, Is.EqualTo(3));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void AddAt_WithDuplicates_InsertsAtCorrectPosition()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var p1 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5); // Equals(p1)
            source.Add(p1);
            source.Add(p2);

            var view1 = views[0];
            var view2 = views[1];

            source.AddAt(1, TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            Assert.That(views.Count, Is.EqualTo(3));
            Assert.That(views[0], Is.SameAs(view1));
            Assert.That(views[1].Id, Is.EqualTo(3));
            Assert.That(views[2], Is.SameAs(view2));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_ReusesExistingResult()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            var originalView = views[0];

            player.Name = "Robert";
            player.TeamId = 20;
            source.Update(player);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0], Is.SameAs(originalView));
            Assert.That(views[0].Text, Is.EqualTo("Robert"));
            Assert.That(views[0].TeamId, Is.EqualTo(20));
        }

        [Test]
        public void Update_RaisesUpdateChangeWithSameResult()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            var originalView = views[0];

            UpdateChange<PlayerView>? received = null;
            views.Changed += c => received = (UpdateChange<PlayerView>)c;

            player.Name = "Robert";
            source.Update(player);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(originalView));
            Assert.That(received.Index, Is.EqualTo(0));
        }

        [Test]
        public void Update_NotInSource_DoesNothing()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            bool raised = false;
            views.Changed += _ => raised = true;

            source.Update(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(raised, Is.False);
            Assert.That(views.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // UpdateAt
        // -------------------------------------------------------------------

        [Test]
        public void UpdateAt_DuplicateEquals_UpdatesCorrectResult()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "B", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var first = views[0];
            var second = views[1];

            p2.Name = "Changed";
            source.UpdateAt(1);

            Assert.That(views[0], Is.SameAs(first));
            Assert.That(views[1], Is.SameAs(second));
            Assert.That(views[1].Text, Is.EqualTo("Changed"));
            Assert.That(views[0].Text, Is.EqualTo("A"));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_RemovesMappedResult()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            var view = views[0];

            RemoveChange<PlayerView>? received = null;
            views.Changed += c => received = (RemoveChange<PlayerView>)c;

            source.Remove(player);

            Assert.That(views.Count, Is.EqualTo(0));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(view));
            Assert.That(received.Index, Is.EqualTo(0));
        }

        [Test]
        public void RemoveAt_RemovesCorrectInstance()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5);
            var p3 = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.RemoveAt(1);

            Assert.That(views.Count, Is.EqualTo(2));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[1].Id, Is.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // RemoveAt
        // -------------------------------------------------------------------

        [Test]
        public void RemoveAt_RaisesRemoveChangeWithIndex()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            var view2 = views[1];

            RemoveChange<PlayerView>? received = null;
            views.Changed += c => received = (RemoveChange<PlayerView>)c;

            source.RemoveAt(1);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Item, Is.SameAs(view2));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void RemoveAt_DuplicateEquals_RemovesCorrectResult()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5); // Equals(p1)

            source.Add(p1);
            source.Add(p2);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var view1 = views[0];
            var view2 = views[1];

            source.RemoveAt(1);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0], Is.SameAs(view1));
            Assert.That(views[0], Is.Not.SameAs(view2));
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        [Test]
        public void Replace_ReplacesResultInSamePosition()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5);
            var p3 = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);
            var middleView = views[1];

            var replacement = TestData.CreatePlayer(id: 99, name: "New", teamId: 20, level: 5);
            source.Replace(p2, replacement);

            Assert.That(views.Count, Is.EqualTo(3));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[1].Id, Is.EqualTo(99));
            Assert.That(views[2].Id, Is.EqualTo(3));
            Assert.That(views[1], Is.Not.SameAs(middleView));
        }

        [Test]
        public void Replace_RaisesReplaceChange()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5);

            source.Add(p1);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);
            var originalView = views[0];

            ReplaceChange<PlayerView>? received = null;
            views.Changed += c => received = (ReplaceChange<PlayerView>)c;

            source.Replace(p1, p2);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.SameAs(originalView));
            Assert.That(received.NewItem.Id, Is.EqualTo(2));
            Assert.That(received.Index, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // ReplaceAt
        // -------------------------------------------------------------------

        [Test]
        public void ReplaceAt_RaisesReplaceChangeWithIndex()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5);
            var p3 = TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);
            var oldView2 = views[1];

            ReplaceChange<PlayerView>? received = null;
            views.Changed += c => received = (ReplaceChange<PlayerView>)c;

            var replacement = TestData.CreatePlayer(id: 99, name: "New", teamId: 20, level: 5);
            source.ReplaceAt(1, replacement);

            Assert.That(views.Count, Is.EqualTo(3));
            Assert.That(views[0].Id, Is.EqualTo(1));
            Assert.That(views[1].Id, Is.EqualTo(99));
            Assert.That(views[2].Id, Is.EqualTo(3));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.OldItem, Is.SameAs(oldView2));
            Assert.That(received.NewItem.Id, Is.EqualTo(99));
            Assert.That(received.Index, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceAt_DuplicateEquals_ReplacesCorrectInstance()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);

            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            var view1 = views[0];
            var view2 = views[1];

            var replacement = TestData.CreatePlayer(id: 99, name: "New", teamId: 20, level: 5);
            source.ReplaceAt(1, replacement);

            Assert.That(views.Count, Is.EqualTo(2));
            Assert.That(views[0], Is.SameAs(view1));
            Assert.That(views[1], Is.Not.SameAs(view2));
            Assert.That(views[1].Id, Is.EqualTo(99));
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        [Test]
        public void Move_PreservesResultReference()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            var viewA = views[0];
            var viewB = views[1];
            var viewC = views[2];

            source.Move(0, 2);

            Assert.That(views[0], Is.SameAs(viewB));
            Assert.That(views[1], Is.SameAs(viewC));
            Assert.That(views[2], Is.SameAs(viewA));
        }

        [Test]
        public void Move_RaisesMoveChangeWithIndices()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            MoveChange<PlayerView>? received = null;
            views.Changed += c => received = (MoveChange<PlayerView>)c;

            source.Move(0, 2);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(0));
            Assert.That(received.ToIndex, Is.EqualTo(2));
            Assert.That(received.Item.Id, Is.EqualTo(1));
        }

        [Test]
        public void Move_Backward_PreservesIndices()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 3, name: "C", teamId: 10, level: 5));

            MoveChange<PlayerView>? received = null;
            views.Changed += c => received = (MoveChange<PlayerView>)c;

            source.Move(2, 0);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(2));
            Assert.That(received.ToIndex, Is.EqualTo(0));
            Assert.That(received.Item.Id, Is.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsProjection()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));

            Assert.That(views.Count, Is.EqualTo(1));

            source.Reset();

            Assert.That(views.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_AllowsFurtherChanges()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));

            source.Reset();

            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 20, level: 5));

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(2));
        }

        [Test]
        public void Reset_RaisesSingleResetChange()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "A", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 2, name: "B", teamId: 10, level: 5));

            var events = new List<Change<PlayerView>>();
            views.Changed += c => events.Add(c);

            source.Reset();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ResetChange<PlayerView>>());
        }

        // -------------------------------------------------------------------
        // Empty updater
        // -------------------------------------------------------------------

        [Test]
        public void EmptyUpdater_DoesNotThrow()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.ObserveSelect(x => x * 2, (_, _) => { });

            Assert.DoesNotThrow(() => source.Update(1));
            Assert.That(node.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            bool received = false;
            views.Changed += _ => received = true;

            views.Dispose();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            views.Dispose();

            Assert.DoesNotThrow(() => views.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<Player>();
            var views = source.ObserveSelect(p => new PlayerView(), TestData.Bind);

            views.Dispose();

            Assert.DoesNotThrow(() =>
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5)));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}