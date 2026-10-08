using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="GroupNode{TKey, TSource}"/>.
    /// </summary>
    [TestFixture]
    public class GroupNodeTests
    {
        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_InitializesFromExistingSource()
        {
            var source = new ObservableList<Player>();

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 20, level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            var groups = source.ObserveGroupBy(p => p.TeamId);

            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(g => g.Key == 10);
            var team20 = groups.Single(g => g.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(2));
            Assert.That(team10.Items, Does.Contain(bob));
            Assert.That(team10.Items, Does.Contain(tom));

            Assert.That(team20.Items.Count, Is.EqualTo(1));
            Assert.That(team20.Items[0], Is.SameAs(mike));
        }

        [Test]
        public void Constructor_NullKeySelector_Throws()
        {
            var source = new ObservableList<Player>();

            Assert.Throws<ArgumentNullException>(() =>
                new GroupNode<int, Player>(source, null!));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_FirstItemWithKey_CreatesGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(10));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(bob));
        }

        [Test]
        public void Add_ItemWithExistingKey_ReusesGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            var originalGroup = groups[0];

            source.Add(tom);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));

            Assert.That(originalGroup.Items.Count, Is.EqualTo(2));
            Assert.That(originalGroup.Items, Does.Contain(bob));
            Assert.That(originalGroup.Items, Does.Contain(tom));
        }

        [Test]
        public void Add_FirstItemWithKey_RaisesGroupAddEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            Change<Group<int, Player>>? received = null;
            groups.Changed += c => received = c;

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));

            Assert.That(received, Is.TypeOf<AddChange<Group<int, Player>>>());

            var add = (AddChange<Group<int, Player>>)received!;
            Assert.That(add.Item.Key, Is.EqualTo(10));
            Assert.That(add.Index, Is.EqualTo(0));
        }

        [Test]
        public void Add_ItemToExistingGroup_RaisesInnerAddEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            var group = groups[0];

            Change<Player>? received = null;
            group.Items.Changed += c => received = c;

            source.Add(tom);

            Assert.That(received, Is.TypeOf<AddChange<Player>>());
            Assert.That(((AddChange<Player>)received!).Item, Is.SameAs(tom));
        }

        [Test]
        public void Add_ItemToExistingGroup_DoesNotRaiseGroupEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));

            bool groupsChanged = false;
            groups.Changed += _ => groupsChanged = true;

            source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1));

            Assert.That(groupsChanged, Is.False);
        }

        [Test]
        public void Add_NewGroup_SubscriberSeesFirstInnerAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            Change<Player>? innerChange = null;
            groups.Changed += change =>
            {
                if (change is AddChange<Group<int, Player>> add)
                    add.Item.Items.Changed += inner => innerChange = inner;
            };

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(bob);

            Assert.That(innerChange, Is.TypeOf<AddChange<Player>>());
            Assert.That(((AddChange<Player>)innerChange!).Item, Is.SameAs(bob));
        }

        [Test]
        public void Add_DuplicateEquals_TwoEntriesInSameGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // AddAt
        // -------------------------------------------------------------------

        [Test]
        public void AddAt_InsertsItemAtCorrectInnerIndex()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var alice = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 10, level: 1);

            source.Add(bob);
            source.Add(tom);

            source.AddAt(1, alice);

            var group = groups.Single();

            Assert.That(group.Items[0], Is.SameAs(bob));
            Assert.That(group.Items[1], Is.SameAs(alice));
            Assert.That(group.Items[2], Is.SameAs(tom));
        }

        [Test]
        public void AddAt_BetweenItemsOfSameGroup_UsesSourceIndex()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 1);
            var tom = TestData.CreatePlayer(id: 3, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            source.Add(mike);
            source.Add(tom);

            var alice = TestData.CreatePlayer(id: 4, name: "Alice", teamId: 10, level: 1);

            // source: [Bob, Mike, Tom]
            // добавляем Alice с teamId = 10 на source-index 1
            source.AddAt(1, alice);
            // source: [Bob, Alice, Mike, Tom]
            // team10: [Bob, Alice, Tom]

            var team10 = groups.Single(g => g.Key == 10);

            Assert.That(team10.Items.Count, Is.EqualTo(3));
            Assert.That(team10.Items[0], Is.SameAs(bob));
            Assert.That(team10.Items[1], Is.SameAs(alice));
            Assert.That(team10.Items[2], Is.SameAs(tom));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_KeyUnchanged_KeepsItemInSameGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);
            var originalGroup = groups[0];

            bob.Name = "Robert";
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(bob));
            Assert.That(groups[0].Items[0].Name, Is.EqualTo("Robert"));
        }

        [Test]
        public void Update_KeyUnchanged_RaisesInnerUpdateEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);
            var group = groups[0];

            Change<Player>? received = null;
            group.Items.Changed += c => received = c;

            bob.Name = "Robert";
            source.Update(bob);

            Assert.That(received, Is.TypeOf<UpdateChange<Player>>());
            Assert.That(((UpdateChange<Player>)received!).Item, Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyChanged_MovesItemToAnotherGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 20, level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(2));

            var team10 = groups.Single(g => g.Key == 10);
            var team20 = groups.Single(g => g.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(1));
            Assert.That(team10.Items[0], Is.SameAs(tom));

            Assert.That(team20.Items.Count, Is.EqualTo(2));
            Assert.That(team20.Items, Does.Contain(mike));
            Assert.That(team20.Items, Does.Contain(bob));
        }

        [Test]
        public void Update_KeyChangedAndOldGroupEmpty_RemovesOldGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 1);

            source.Add(bob);
            source.Add(mike);

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));
            Assert.That(groups[0].Items, Does.Contain(bob));
            Assert.That(groups[0].Items, Does.Contain(mike));
        }

        [Test]
        public void Update_KeyChanged_OldGroupEmpty_RaisesGroupRemoveThenAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            var groupEvents = new List<Change<Group<int, Player>>>();
            groups.Changed += c => groupEvents.Add(c);

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groupEvents.Count, Is.EqualTo(2));
            Assert.That(groupEvents[0], Is.TypeOf<RemoveChange<Group<int, Player>>>());
            Assert.That(groupEvents[1], Is.TypeOf<AddChange<Group<int, Player>>>());
        }

        [Test]
        public void Update_KeyChanged_OldGroupGetsRemove_NewGroupGetsAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            var oldGroup = groups[0];

            var oldGroupEvents = new List<Change<Player>>();
            oldGroup.Items.Changed += c => oldGroupEvents.Add(c);

            bob.TeamId = 20;
            source.Update(bob);

            var newGroup = groups[0];
            Assert.That(newGroup, Is.Not.SameAs(oldGroup));
            Assert.That(newGroup.Key, Is.EqualTo(20));

            Assert.That(oldGroupEvents.Count, Is.EqualTo(1));
            Assert.That(oldGroupEvents[0], Is.TypeOf<RemoveChange<Player>>());

            Assert.That(newGroup.Items.Count, Is.EqualTo(1));
            Assert.That(newGroup.Items[0], Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyChanged_NewGroupRaisesInnerAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            Change<Player>? innerAdd = null;
            groups.Changed += change =>
            {
                if (change is AddChange<Group<int, Player>> add)
                    add.Item.Items.Changed += inner =>
                    {
                        if (inner is AddChange<Player> a)
                            innerAdd = a;
                    };
            };

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(innerAdd, Is.Not.Null);
            Assert.That(((AddChange<Player>)innerAdd!).Item, Is.SameAs(bob));
        }


        // -------------------------------------------------------------------
        // UpdateAt
        // -------------------------------------------------------------------

        [Test]
        public void UpdateAt_DuplicateEquals_UpdatesCorrectOccurrence()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);

            p2.Name = "Robert";
            source.UpdateAt(1);

            var group = groups.Single();

            Assert.That(group.Items[0], Is.SameAs(p1));
            Assert.That(group.Items[1], Is.SameAs(p2));
            Assert.That(group.Items[1].Name, Is.EqualTo("Robert"));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_FromNonEmptyGroup_KeepsGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            source.Add(tom);
            var group = groups[0];

            source.Remove(bob);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(group));
            Assert.That(group.Items.Count, Is.EqualTo(1));
            Assert.That(group.Items[0], Is.SameAs(tom));
        }

        [Test]
        public void Remove_LastItem_RemovesGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);
            var group = groups[0];

            Change<Group<int, Player>>? received = null;
            groups.Changed += c => received = c;

            source.Remove(bob);

            Assert.That(groups.Count, Is.EqualTo(0));
            Assert.That(received, Is.TypeOf<RemoveChange<Group<int, Player>>>());
            Assert.That(((RemoveChange<Group<int, Player>>)received!).Item, Is.SameAs(group));
        }

        [Test]
        public void Remove_NonExistent_DoesNothing()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var ghost = TestData.CreatePlayer(id: 99, name: "Ghost", teamId: 10, level: 1);

            source.Add(bob);

            Assert.DoesNotThrow(() => source.Remove(ghost));
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
        }


        // -------------------------------------------------------------------
        // RemoveAt
        // -------------------------------------------------------------------

        [Test]
        public void RemoveAt_DuplicateEquals_RemovesCorrectOccurrence()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);

            source.RemoveAt(1);

            var group = groups.Single();

            Assert.That(group.Items.Count, Is.EqualTo(1));
            Assert.That(group.Items[0], Is.SameAs(p1));
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        [Test]
        public void Replace_SameKey_ReplacesInPlace()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var alice = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 10, level: 1);

            source.Add(bob);
            var originalGroup = groups[0];

            source.Replace(bob, alice);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0], Is.SameAs(originalGroup));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(alice));
        }

        [Test]
        public void Replace_KeyChanged_MovesItemToAnotherGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 1);

            source.Add(bob);
            source.Add(mike);

            var alice = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 20, level: 1);
            source.Replace(bob, alice);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Key, Is.EqualTo(20));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));
            Assert.That(groups[0].Items, Does.Contain(alice));
            Assert.That(groups[0].Items, Does.Contain(mike));
        }

        // -------------------------------------------------------------------
        // ReplaceAt
        // -------------------------------------------------------------------

        [Test]
        public void ReplaceAt_SameKey_PreservesGroupAndPosition()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var p3 = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var group = groups.Single();

            var replacement = TestData.CreatePlayer(id: 4, name: "Alice", teamId: 10, level: 1);

            source.ReplaceAt(1, replacement);

            Assert.That(groups.Single(), Is.SameAs(group));
            Assert.That(group.Items.Count, Is.EqualTo(3));
            Assert.That(group.Items[0], Is.SameAs(p1));
            Assert.That(group.Items[1], Is.SameAs(replacement));
            Assert.That(group.Items[2], Is.SameAs(p3));
        }

        [Test]
        public void ReplaceAt_ChangedKey_MovesItemAndKeepsOldGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 20, level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            var team10 = groups.Single(g => g.Key == 10);
            var team20 = groups.Single(g => g.Key == 20);

            var replacement = TestData.CreatePlayer(id: 4, name: "Alice", teamId: 20, level: 1);

            source.ReplaceAt(0, replacement);

            Assert.That(groups.Count, Is.EqualTo(2));

            Assert.That(groups.Single(g => g.Key == 10), Is.SameAs(team10));
            Assert.That(groups.Single(g => g.Key == 20), Is.SameAs(team20));

            Assert.That(team10.Items.Count, Is.EqualTo(1));
            Assert.That(team10.Items[0], Is.SameAs(tom));

            Assert.That(team20.Items.Count, Is.EqualTo(2));
            Assert.That(team20.Items[0], Is.SameAs(replacement));
            Assert.That(team20.Items[1], Is.SameAs(mike));
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        [Test]
        public void Move_SameGroup_ReordersInnerItems()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 3, name: "Mike", teamId: 10, level: 1);

            source.Add(bob);
            source.Add(tom);
            source.Add(mike);

            source.Move(2, 0);

            var group = groups.Single();

            Assert.That(group.Items[0], Is.SameAs(mike));
            Assert.That(group.Items[1], Is.SameAs(bob));
            Assert.That(group.Items[2], Is.SameAs(tom));
        }

        [Test]
        public void Move_DuplicateEquals_MovesCorrectOccurrence()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p3 = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            source.Move(2, 0);

            var group = groups.Single();

            Assert.That(group.Items[0], Is.SameAs(p3));
            Assert.That(group.Items[1], Is.SameAs(p1));
            Assert.That(group.Items[2], Is.SameAs(p2));
        }

        [Test]
        public void Move_BetweenGroups_ReordersOnlySourcePositions()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var mike = TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 1);
            var tom = TestData.CreatePlayer(id: 3, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            source.Add(mike);
            source.Add(tom);

            // Перемещаем Tom (team10, source-index 2) на source-index 0
            source.Move(2, 0);
            // source: [Tom, Bob, Mike]
            // team10 должен стать: [Tom, Bob]

            var team10 = groups.Single(g => g.Key == 10);
            var team20 = groups.Single(g => g.Key == 20);

            Assert.That(team10.Items.Count, Is.EqualTo(2));
            Assert.That(team10.Items[0], Is.SameAs(tom));
            Assert.That(team10.Items[1], Is.SameAs(bob));

            Assert.That(team20.Items.Count, Is.EqualTo(1));
            Assert.That(team20.Items[0], Is.SameAs(mike));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsGroups()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));
            source.Add(TestData.CreatePlayer(id: 2, name: "Mike", teamId: 20, level: 1));

            Assert.That(groups.Count, Is.EqualTo(2));

            source.Reset();

            Assert.That(groups.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Ключи
        // -------------------------------------------------------------------

        [Test]
        public void Add_NullKey_ThrowsArgumentNullException()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => (string?)null);

            Assert.Throws<ArgumentNullException>(() =>
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1)));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            bool received = false;
            groups.Changed += _ => received = true;

            groups.Dispose();

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            groups.Dispose();

            Assert.DoesNotThrow(() => groups.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<Player>();
            var groups = source.ObserveGroupBy(p => p.TeamId);

            groups.Dispose();

            Assert.DoesNotThrow(() =>
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1)));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}