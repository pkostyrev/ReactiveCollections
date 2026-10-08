using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Интеграционные тесты: цепочки из нескольких узлов,
    /// проверка сквозного прохождения изменений.
    /// </summary>
    /// <remarks>
    /// Здесь — только сценарии с цепочкой ≥ 2 узлов. Юнит-тесты отдельных
    /// узлов живут в соответствующих <c>*Tests</c> файлах.
    /// </remarks>
    [TestFixture]
    public class IntegrationTests
    {
        /// <summary>
        /// Стандартная цепочка: Filter → Select → GroupBy.
        /// </summary>
        private static IObservableList<Group<int, PlayerView>> CreateChain(
            IObservableList<Player> source)
        {
            return source
                .ObserveWhere(p => p.Level >= 10)
                .ObserveSelect(p => new PlayerView(), TestData.Bind)
                .ObserveGroupBy(v => v.TeamId);
        }

        // -------------------------------------------------------------------
        // Инициализация цепочки
        // -------------------------------------------------------------------

        [Test]
        public void Chain_InitializesFromExistingSource()
        {
            var source = new ObservableList<Player>();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));
            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5));
            source.Add(TestData.CreatePlayer(id: 3, name: "Mike", teamId: 20, level: 20));

            var groups = CreateChain(source);

            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(g => g.Key == 10);
            var team20 = groups.Single(g => g.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(1));
            Assert.That(team10.Items[0].Id, Is.EqualTo(1));
            Assert.That(team10.Items[0].Text, Is.EqualTo("Bob"));

            Assert.That(team20.Items.Count, Is.EqualTo(1));
            Assert.That(team20.Items[0].Id, Is.EqualTo(3));
            Assert.That(team20.Items[0].Text, Is.EqualTo("Mike"));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_MatchingPlayer_PassesThroughEntireChain()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));

            var view = groups[0].Items[0];
            Assert.That(view.Id, Is.EqualTo(bob.Id));
            Assert.That(view.Text, Is.EqualTo(bob.Name));
            Assert.That(view.TeamId, Is.EqualTo(bob.TeamId));
        }

        [Test]
        public void Add_NotMatchingPlayer_DoesNotReachResult()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));

            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_NewGroup_RaisesNodeEventsInChainOrder()
        {
            var source = new ObservableList<Player>();
            var filter = source.ObserveWhere(p => p.Level >= 10);
            var select = filter.ObserveSelect(p => new PlayerView(), TestData.Bind);
            var groups = select.ObserveGroupBy(v => v.TeamId);

            var order = new List<string>();
            filter.Changed += _ => order.Add("filter");
            select.Changed += _ => order.Add("select");
            groups.Changed += _ => order.Add("groups");

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));

            // Raise вызывается синхронно и вложенно: первым отрабатывает
            // самый глубокий узел цепочки, последним — самый близкий к источнику.
            Assert.That(order, Is.EqualTo(new[] { "groups", "select", "filter" }));
        }

        [Test]
        public void Add_DuplicateEqualsElements_MaintainCorrectMapping()
        {
            var source = new ObservableList<Player>();

            // Player.Equals по Id — оба равны
            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);

            source.Add(p1);
            source.Add(p2);

            var views = source
                .ObserveWhere(x => x.Level >= 10)
                .ObserveSelect(x => new PlayerView(), TestData.Bind);

            Assert.That(views.Count, Is.EqualTo(2));

            source.Remove(p1);

            Assert.That(views.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_PlayerStartsMatching_CreatesViewAndGroup()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5);
            source.Add(bob);

            bob.Level = 10;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0].Id, Is.EqualTo(bob.Id));
        }

        [Test]
        public void Update_PlayerStopsMatching_RemovesViewAndGroup()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(bob);
            Assert.That(groups.Count, Is.EqualTo(1));

            bob.Level = 5;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_PlayerData_UpdatesExistingView()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(bob);

            var originalGroup = groups[0];
            var originalView = originalGroup.Items[0];

            bob.Name = "Robert";
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));
            Assert.That(groups[0].Items[0], Is.SameAs(originalView));
            Assert.That(originalView.Text, Is.EqualTo("Robert"));
        }

        [Test]
        public void Update_PlayerTeam_MovesSameViewToAnotherGroup()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 20);
            var mike = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 20, level: 25);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            var team10Before = groups.Single(g => g.Key == 10);
            var team20Before = groups.Single(g => g.Key == 20);

            var originalBobView = team10Before.Items.Single(v => v.Id == bob.Id);

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(2));

            var team10After = groups.Single(g => g.Key == 10);
            var team20After = groups.Single(g => g.Key == 20);

            Assert.That(team10After, Is.SameAs(team10Before));
            Assert.That(team20After, Is.SameAs(team20Before));

            Assert.That(team10After.Items.Any(v => v.Id == bob.Id), Is.False);

            var movedBobView = team20After.Items.Single(v => v.Id == bob.Id);
            Assert.That(movedBobView, Is.SameAs(originalBobView));
            Assert.That(movedBobView.TeamId, Is.EqualTo(20));
        }

        [Test]
        public void Update_PlayerTeam_WhenOldGroupEmpty_RemovesOldGroup()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var mike = TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 20);

            source.Add(bob);
            source.Add(mike);

            var originalView = groups.Single(g => g.Key == 10).Items[0];

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));

            var movedView = groups[0].Items.Single(v => v.Id == bob.Id);
            Assert.That(movedView, Is.SameAs(originalView));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_MatchingPlayer_RemovesViewFromChain()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 20);

            source.Add(bob);
            source.Add(tom);

            var group = groups[0];

            source.Remove(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(group));
            Assert.That(group.Items.Count, Is.EqualTo(1));
            Assert.That(group.Items[0].Id, Is.EqualTo(tom.Id));
        }

        [Test]
        public void Remove_LastMatchingPlayer_RemovesEmptyGroup()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15);
            source.Add(bob);

            source.Remove(bob);

            Assert.That(groups.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_AllowsFurtherChanges()
        {
            var source = new ObservableList<Player>();
            var groups = CreateChain(source);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));

            source.Reset();

            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 20, level: 30));

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Merge в цепочке
        // -------------------------------------------------------------------

        [Test]
        public void Merge_CanBeGroupedAfterSelect()
        {
            var a = new ObservableList<Player>();
            var b = new ObservableList<Player>();

            a.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));
            b.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 20, level: 15));

            var groups = a.ObserveMerge(b)
                .ObserveSelect(p => new PlayerView(), TestData.Bind)
                .ObserveGroupBy(v => v.TeamId);

            Assert.That(groups.Count, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // IDisposable: удаление всей цепочки
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_WholeChain_StopsPropagation()
        {
            var source = new ObservableList<Player>();
            var filter = source.ObserveWhere(p => p.Level >= 10);
            var select = filter.ObserveSelect(p => new PlayerView(), TestData.Bind);
            var groups = select.ObserveGroupBy(v => v.TeamId);

            bool groupsChanged = false;
            groups.Changed += _ => groupsChanged = true;

            groups.Dispose();
            select.Dispose();
            filter.Dispose();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 15));

            Assert.That(groupsChanged, Is.False);
            Assert.That(groups.Count, Is.EqualTo(0));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}