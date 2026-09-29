using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="GroupNode{TKey, TSource}"/> — группировки элементов
    /// источника по ключу.
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

            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            Change<Group<int, Player>>? received = null;
            groups.Changed += c => received = c;

            source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1));

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item.Key, Is.EqualTo(10));
        }

        [Test]
        public void Add_ItemToExistingGroup_RaisesInnerAddEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var tom = TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 1);

            source.Add(bob);
            var group = groups[0];

            Change<Player>? received = null;
            group.Items.Changed += c => received = c;

            source.Add(tom);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(received.Item, Is.SameAs(tom));
        }

        [Test]
        public void Add_ItemToExistingGroup_DoesNotRaiseGroupEvent()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            Change<Player>? innerChange = null;
            groups.Changed += change =>
            {
                if (change.Type != ChangeType.Add)
                    return;

                change.Item.Items.Changed += itemChange => innerChange = itemChange;
            };

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(bob);

            Assert.That(innerChange, Is.Not.Null);
            Assert.That(innerChange!.Type, Is.EqualTo(ChangeType.Add));
            Assert.That(innerChange.Item, Is.SameAs(bob));
        }

        [Test]
        public void Add_DuplicateEquals_TwoEntriesInSameGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            // Player.Equals по Id — оба равны
            var p1 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var p2 = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);

            source.Add(p1);
            source.Add(p2);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items.Count, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_KeyUnchanged_KeepsItemInSameGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);
            var group = groups[0];

            Change<Player>? received = null;
            group.Items.Changed += c => received = c;

            bob.Name = "Robert";
            source.Update(bob);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
            Assert.That(received.Item, Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyChanged_MovesItemToAnotherGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            var groupEvents = new List<ChangeType>();
            groups.Changed += c => groupEvents.Add(c.Type);

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(groupEvents,
                Is.EqualTo(new[] { ChangeType.Remove, ChangeType.Add }));
        }

        [Test]
        public void Update_KeyChanged_OldGroupGetsRemove_NewGroupGetsAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            var oldGroup = groups[0];

            var oldGroupEvents = new List<ChangeType>();
            oldGroup.Items.Changed += c => oldGroupEvents.Add(c.Type);

            bob.TeamId = 20;
            source.Update(bob);

            var newGroup = groups[0];
            Assert.That(newGroup, Is.Not.SameAs(oldGroup));
            Assert.That(newGroup.Key, Is.EqualTo(20));

            // старая группа получила Remove
            Assert.That(oldGroupEvents,
                Is.EqualTo(new[] { ChangeType.Remove }));

            // новая группа уже содержит bob — подписка после факта
            Assert.That(newGroup.Items.Count, Is.EqualTo(1));
            Assert.That(newGroup.Items[0], Is.SameAs(bob));
        }

        [Test]
        public void Update_KeyChanged_NewGroupRaisesInnerAdd()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);

            Change<Player>? innerAdd = null;
            groups.Changed += change =>
            {
                if (change.Type == ChangeType.Add)
                {
                    change.Item.Items.Changed += inner =>
                    {
                        if (inner.Type == ChangeType.Add)
                            innerAdd = inner;
                    };
                }
            };

            bob.TeamId = 20;
            source.Update(bob);

            Assert.That(innerAdd, Is.Not.Null);
            Assert.That(innerAdd!.Item, Is.SameAs(bob));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_FromNonEmptyGroup_KeepsGroup()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            source.Add(bob);
            var group = groups[0];

            Change<Group<int, Player>>? received = null;
            groups.Changed += c => received = c;

            source.Remove(bob);

            Assert.That(groups.Count, Is.EqualTo(0));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Remove));
            Assert.That(received.Item, Is.SameAs(group));
        }

        [Test]
        public void Remove_NonExistent_DoesNothing()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var ghost = TestData.CreatePlayer(id: 99, name: "Ghost", teamId: 10, level: 1);

            source.Add(bob);

            Assert.DoesNotThrow(() => source.Remove(ghost));
            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Replace (используется базовая эмуляция Remove + Add)
        // -------------------------------------------------------------------

        [Test]
        public void Replace_ItemInGroup_ReplacesInPlace()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            var bob = TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1);
            var alice = TestData.CreatePlayer(id: 3, name: "Alice", teamId: 10, level: 1);

            source.Add(bob);

            source.Replace(bob, alice);

            Assert.That(groups.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items.Count, Is.EqualTo(1));
            Assert.That(groups[0].Items[0], Is.SameAs(alice));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsGroups()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => (string?)null);

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
            var groups = source.GroupBy(p => p.TeamId);

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
            var groups = source.GroupBy(p => p.TeamId);

            groups.Dispose();

            Assert.DoesNotThrow(() => groups.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            groups.Dispose();

            Assert.DoesNotThrow(() =>
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 1)));
            Assert.That(source.Count, Is.EqualTo(1));
        }
    }
}