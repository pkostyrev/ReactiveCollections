using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="SelectNode{TSource, TResult}"/> — проекции,
    /// преобразующей элементы источника в результат с сохранением
    /// ссылочной идентичности.
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

            var views = source.Select(p => new PlayerView(), TestData.Bind);

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
                source.Select<Player, PlayerView>(null!, TestData.Bind));
        }

        [Test]
        public void Constructor_NullUpdater_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                source.Select(p => new PlayerView(), null!));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_CreatesResult()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0].Id, Is.EqualTo(player.Id));
            Assert.That(views[0].Text, Is.EqualTo(player.Name));
            Assert.That(views[0].TeamId, Is.EqualTo(player.TeamId));
        }

        [Test]
        public void Add_RaisesAddEvent()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            Change<PlayerView>? received = null;
            views.Changed += c => received = c;

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item.Text, Is.EqualTo("Bob"));
        }

        [Test]
        public void Add_DuplicateEquals_CreatesTwoResults()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5); // Equals == true

            source.Add(p1);
            source.Add(p2);

            var views = source.Select(p => new PlayerView(), TestData.Bind);

            Assert.That(views.Count, Is.EqualTo(2));
            Assert.That(views[0], Is.Not.SameAs(views[1]));
        }

        [Test]
        public void Add_IdentityFactory_PreservesReference()
        {
            var source = new ObservableList<Player>();
            var p = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(p);

            var node = source.Select(x => (object)x, (_, _) => { });

            Assert.That(node[0], Is.SameAs(p));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_ReusesExistingResult()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

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
        public void Update_RaisesUpdateEventWithSameResult()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            var originalView = views[0];

            Change<PlayerView>? received = null;
            views.Changed += c => received = c;

            player.Name = "Robert";
            source.Update(player);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(originalView));
        }

        [Test]
        public void Update_NotInSource_DoesNothing()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            bool raised = false;
            views.Changed += _ => raised = true;

            // источник не содержит элемент — Update вернёт false, событие не пойдёт
            source.Update(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(raised, Is.False);
            Assert.That(views.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_DuplicateEquals_UpdatesFirstMatch()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(1);

            var node = source.Select(x => x, (_, _) => { });

            int updates = 0;
            node.Changed += c =>
            {
                if (c.Type == ChangeType.Update) updates++;
            };

            source.Update(1);

            Assert.That(updates, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_RemovesMappedResult()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            var player = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(player);

            var view = views[0];

            Change<PlayerView>? received = null;
            views.Changed += c => received = c;

            source.Remove(player);

            Assert.That(views.Count, Is.EqualTo(0));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.SameAs(view));
        }

        [Test]
        public void Remove_DuplicateEquals_RemovesFirstMatch()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);

            source.Add(p1);
            source.Add(p2);

            var views = source.Select(p => new PlayerView(), TestData.Bind);
            Assert.That(views.Count, Is.EqualTo(2));

            // source.Remove(p2) удалит первый равный — p1.
            // SelectNode повторит семантику: удалит Entry для первого равного.
            source.Remove(p2);

            Assert.That(views.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Replace: используется базовая эмуляция Remove + Add
        // -------------------------------------------------------------------

        [Test]
        public void Replace_CreatesNewResult()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5);

            source.Add(p1);

            var views = source.Select(p => new PlayerView(), TestData.Bind);
            var originalView = views[0];

            source.Replace(p1, p2);

            Assert.That(views.Count, Is.EqualTo(1));
            Assert.That(views[0], Is.Not.SameAs(originalView));
            Assert.That(views[0].Id, Is.EqualTo(2));
        }

        [Test]
        public void Replace_RaisesRemoveThenAdd()
        {
            var source = new ObservableList<Player>();

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5);

            source.Add(p1);

            var views = source.Select(p => new PlayerView(), TestData.Bind);

            var events = new List<Change<PlayerView>>();
            views.Changed += e => events.Add(e);

            source.Replace(p1, p2);

            Assert.That(events.Select(e => e.Type),
                Is.EqualTo(new[] { ChangeType.Remove, ChangeType.Add }));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsProjection()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));

            Assert.That(views.Count, Is.EqualTo(1));

            source.Reset();

            Assert.That(views.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Empty updater
        // -------------------------------------------------------------------

        [Test]
        public void EmptyUpdater_DoesNotThrow()
        {
            var source = new ObservableList<int>();
            source.Add(1);

            var node = source.Select(x => x * 2, (_, _) => { });

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
            var views = source.Select(p => new PlayerView(), TestData.Bind);

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
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            views.Dispose();

            Assert.DoesNotThrow(() => views.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            views.Dispose();

            Assert.DoesNotThrow(() =>
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5)));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}