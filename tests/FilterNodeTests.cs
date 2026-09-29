using ReactiveCollections.Tests.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class FilterNodeTests
    {
        [Test]
        public void Constructor_ShouldInitializeFromSource()
        {
            // Arrange
            var source = new ObservableList<Unit>();

            source.Add(new Unit { Name = "Knight", HP = 100 });
            source.Add(new Unit { Name = "Ghost", HP = 0 });

            // Act
            var alive = source.Filter(x => x.HP > 0);

            // Assert
            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0].Name, Is.EqualTo("Knight"));
        }

        [Test]
        public void Add_MatchingItem_ShouldAppear()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            source.Add(new Unit
            {
                Name = "Knight",
                HP = 100
            });

            Assert.That(alive.Count, Is.EqualTo(1));
        }

        [Test]
        public void Add_NotMatchingItem_ShouldNotAppear()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            source.Add(new Unit
            {
                Name = "Ghost",
                HP = 0
            });

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_ItemStartsMatching_ShouldBeAdded()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            var ghost = new Unit
            {
                Name = "Ghost",
                HP = 0
            };

            source.Add(ghost);

            ghost.HP = 50;

            source.Update(ghost);

            Assert.That(alive.Count, Is.EqualTo(1));
            Assert.That(alive[0], Is.SameAs(ghost));
        }

        [Test]
        public void Update_ItemStopsMatching_ShouldBeRemoved()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            var knight = new Unit
            {
                Name = "Knight",
                HP = 100
            };

            source.Add(knight);

            knight.HP = 0;

            source.Update(knight);

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_ItemStillMatches_ShouldRaiseUpdate()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            var knight = new Unit
            {
                Name = "Knight",
                HP = 100
            };

            source.Add(knight);

            Change<Unit>? received = null;

            alive.Changed += c => received = c;

            knight.HP = 80;

            source.Update(knight);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
        }

        [Test]
        public void Remove_MatchingItem_ShouldBeRemoved()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            var knight = new Unit
            {
                Name = "Knight",
                HP = 100
            };

            source.Add(knight);

            source.Remove(knight);

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Remove_NotMatchingItem_ShouldDoNothing()
        {
            var source = new ObservableList<Unit>();

            var alive = source.Filter(x => x.HP > 0);

            var ghost = new Unit
            {
                Name = "Ghost",
                HP = 0
            };

            source.Add(ghost);

            source.Remove(ghost);

            Assert.That(alive.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_ShouldClearFilteredItems()
        {
            var source = new ObservableList<Player>();

            var filter = source.Filter(x => x.Level >= 10);

            source.Add(new Player { Level = 15 });
            source.Add(new Player { Level = 20 });

            Assert.That(filter.Count, Is.EqualTo(2));

            source.Reset();

            Assert.That(filter.Count, Is.EqualTo(0));
        }

        [Test]
        public void Filter_Replace_OldNotInFilter_NewValid_AddsNew()
        {
            var source = new ObservableList<int>();
            source.Add(1);                          // нечётное, фильтр пропустит
            var node = source.Filter(x => x % 2 == 1);
            Assert.That(node.Count, Is.EqualTo(1));

            source.Replace(1, 2);                   // было 1 (проходит), стало 2 (не проходит)
            Assert.That(node.Count, Is.EqualTo(0));

            source.Replace(2, 3);                   // было 2 (не проходит), стало 3 (проходит)
            Assert.That(node.Count, Is.EqualTo(1));
            Assert.That(node[0], Is.EqualTo(3));
        }

        [Test]
        public void Filter_Update_ElementStaysValid_EmitsUpdate()
        {
            var source = new ObservableList<Player>();
            var p = new Player { Name = "A" };
            source.Add(p);

            var node = source.Filter(x => x.Name.Length > 0);

            Change<Player>? lastChange = null;
            node.Changed += c => lastChange = c;

            p.Name = "B";
            source.Update(p);

            Assert.That(lastChange, Is.Not.Null);
            Assert.That(lastChange!.Type, Is.EqualTo(ChangeType.Update));
        }

        [Test]
        public void Filter_Remove_NotInFilter_DoesNotThrow()
        {
            var source = new ObservableList<int>();
            source.Add(2);                          // чётное, фильтр не пропустит
            var node = source.Filter(x => x % 2 == 1);
            Assert.That(node.Count, Is.EqualTo(0));

            Assert.DoesNotThrow(() => source.Remove(2));
        }
    }
}
