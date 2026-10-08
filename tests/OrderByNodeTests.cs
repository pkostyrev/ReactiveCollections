using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="OrderByNode{TSource, TKey}"/>.
    /// </summary>
    [TestFixture]
    public class OrderByNodeTests
    {
        private static Player P(int id, int level, string name = "")
            => TestData.CreatePlayer(id: id, name: name, teamId: 0, level: level);

        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ResultIsEmpty()
        {
            var source = new ObservableList<Player>();

            var sorted = source.ObserveOrderBy(p => p.Level);

            Assert.That(sorted.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_SortsExistingItems()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 30));
            source.Add(P(id: 2, level: 10));
            source.Add(P(id: 3, level: 20));

            var sorted = source.ObserveOrderBy(p => p.Level);

            Assert.That(sorted.Count, Is.EqualTo(3));
            Assert.That(sorted[0].Level, Is.EqualTo(10));
            Assert.That(sorted[1].Level, Is.EqualTo(20));
            Assert.That(sorted[2].Level, Is.EqualTo(30));
        }

        [Test]
        public void Constructor_ResolvesToReactiveExtension_NotLinq()
        {
            var source = new ObservableList<int>();
            source.Add(3);
            source.Add(1);

            var sorted = source.ObserveOrderBy(x => x);

            Assert.That(sorted, Is.TypeOf<OrderByNode<int, int>>());
        }

        [Test]
        public void Constructor_Descending_SortsInReverseOrder()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));
            source.Add(P(id: 2, level: 30));
            source.Add(P(id: 3, level: 20));

            var sorted = source.ObserveOrderByDescending(p => p.Level);

            Assert.That(sorted[0].Level, Is.EqualTo(30));
            Assert.That(sorted[1].Level, Is.EqualTo(20));
            Assert.That(sorted[2].Level, Is.EqualTo(10));
        }

        [Test]
        public void Constructor_CustomComparer_UsesComparer()
        {
            var source = new ObservableList<int>();
            source.Add(1);
            source.Add(2);
            source.Add(3);

            // обратный порядок через кастомный компаратор
            var descending = Comparer<int>.Create((a, b) => b.CompareTo(a));
            var sorted = source.ObserveOrderBy(x => x, descending);

            Assert.That(sorted, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        [Test]
        public void Add_ToEmpty_InsertsItem()
        {
            var source = new ObservableList<Player>();
            var sorted = source.ObserveOrderBy(p => p.Level);

            var p = P(id: 1, level: 10);
            source.Add(p);

            Assert.That(sorted.Count, Is.EqualTo(1));
            Assert.That(sorted[0], Is.SameAs(p));
        }

        [Test]
        public void Add_AtEnd_AppendsItem()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));
            source.Add(P(id: 2, level: 20));

            var sorted = source.ObserveOrderBy(p => p.Level);

            var newP = P(id: 3, level: 30);
            source.Add(newP);

            Assert.That(sorted[2], Is.SameAs(newP));
        }

        [Test]
        public void Add_AtBeginning_InsertsFirst()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 20));
            source.Add(P(id: 2, level: 30));

            var sorted = source.ObserveOrderBy(p => p.Level);

            var newP = P(id: 3, level: 10);
            source.Add(newP);

            Assert.That(sorted[0], Is.SameAs(newP));
            Assert.That(sorted[1].Level, Is.EqualTo(20));
            Assert.That(sorted[2].Level, Is.EqualTo(30));
        }

        [Test]
        public void Add_InMiddle_InsertsAtCorrectPosition()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));
            source.Add(P(id: 2, level: 30));

            var sorted = source.ObserveOrderBy(p => p.Level);

            var newP = P(id: 3, level: 20);
            source.Add(newP);

            Assert.That(sorted[0].Level, Is.EqualTo(10));
            Assert.That(sorted[1], Is.SameAs(newP));
            Assert.That(sorted[2].Level, Is.EqualTo(30));
        }

        [Test]
        public void AddAt_InsertsCorrectSourceMapping()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 30);
            var p2 = P(id: 2, level: 20);
            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(x => x.Level);

            // Вставляем p3 на source-index 0
            var p3 = P(id: 3, level: 10);
            source.AddAt(0, p3);

            Assert.That(sorted.Count, Is.EqualTo(3));
            Assert.That(sorted[0], Is.SameAs(p3));
            Assert.That(sorted[1], Is.SameAs(p2));
            Assert.That(sorted[2], Is.SameAs(p1));

            // Проверяем, что _sourceOrder тоже перестроился:
            // UpdateAt(0) должен обновить p3, а не p1.
            p3.Name = "Changed";
            source.UpdateAt(0);

            Assert.That(sorted[0].Name, Is.EqualTo("Changed"));
            Assert.That(sorted[0], Is.SameAs(p3));
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        [Test]
        public void Remove_ExistingItem_RemovesFromSorted()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            var p3 = P(id: 3, level: 30);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(p => p.Level);

            source.Remove(p2);

            Assert.That(sorted.Count, Is.EqualTo(2));
            Assert.That(sorted[0], Is.SameAs(p1));
            Assert.That(sorted[1], Is.SameAs(p3));
        }

        [Test]
        public void RemoveAt_DuplicateEquals_RemovesCorrectEntry()
        {
            var source = new ObservableList<Player>();

            // Player.Equals по Id — оба равны, но это разные occurrences
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 1, level: 10);

            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(p => p.Level);
            Assert.That(sorted.Count, Is.EqualTo(2));

            source.RemoveAt(1);

            Assert.That(sorted.Count, Is.EqualTo(1));
            Assert.That(sorted[0], Is.SameAs(p1));
        }

        // -------------------------------------------------------------------
        // Update — эквивалентный ключ
        // -------------------------------------------------------------------

        [Test]
        public void Update_SameKey_RaisesUpdateChangeOnSamePosition()
        {
            var source = new ObservableList<Player>();
            var p = P(id: 1, level: 10);
            source.Add(p);

            var sorted = source.ObserveOrderBy(x => x.Level);

            UpdateChange<Player>? received = null;
            sorted.Changed += c => received = (UpdateChange<Player>)c;

            p.Name = "Updated";
            source.Update(p);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));
            Assert.That(sorted[0], Is.SameAs(p));
        }

        // -------------------------------------------------------------------
        // Update — изменённый ключ
        // -------------------------------------------------------------------

        [Test]
        public void Update_ChangedKey_MovesItemToNewPosition()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            var p3 = P(id: 3, level: 30);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);
            Assert.That(sorted[0], Is.SameAs(p1));

            // p1 становится самым большим
            p1.Level = 100;
            source.Update(p1);

            Assert.That(sorted[0], Is.SameAs(p2));
            Assert.That(sorted[1], Is.SameAs(p3));
            Assert.That(sorted[2], Is.SameAs(p1));
        }

        [Test]
        public void Update_ComparatorEquivalentKey_RaisesUpdateNotRemoveAdd()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 11);
            source.Add(p1);

            // Компаратор считает 11 и 19 эквивалентными (оба в [10, 20))
            var comparer = Comparer<int>.Create((a, b) => (a / 10).CompareTo(b / 10));
            var sorted = source.ObserveOrderBy(x => x.Level, comparer);

            var events = new List<Change<Player>>();
            sorted.Changed += events.Add;

            p1.Level = 19;
            source.Update(p1);

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<UpdateChange<Player>>());

            var update = (UpdateChange<Player>)events[0];
            Assert.That(update.Index, Is.EqualTo(0));
            Assert.That(update.Item, Is.SameAs(p1));
            Assert.That(sorted[0], Is.SameAs(p1));
        }

        [Test]
        public void Update_ChangedKey_RaisesRemoveThenAdd()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            var p3 = P(id: 3, level: 30);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);

            var events = new List<Change<Player>>();
            sorted.Changed += events.Add;

            p1.Level = 100;
            source.Update(p1);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0], Is.TypeOf<RemoveChange<Player>>());
            Assert.That(events[1], Is.TypeOf<AddChange<Player>>());

            var remove = (RemoveChange<Player>)events[0];
            var add = (AddChange<Player>)events[1];

            Assert.That(remove.Index, Is.EqualTo(0));   // удалён с начала
            Assert.That(add.Index, Is.EqualTo(2));      // добавлен в конец

            Assert.That(remove.Item, Is.SameAs(p1));
            Assert.That(add.Item, Is.SameAs(p1));
        }

        [Test]
        public void Update_ChangedKey_SameSortedPosition_StillRemoveAdd()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(x => x.Level);

            var events = new List<Change<Player>>();
            sorted.Changed += events.Add;

            // 10 → 15, но позиция в sorted остаётся 0 (перед 20)
            p1.Level = 15;
            source.Update(p1);

            // Даже на той же позиции — Remove + Add, потому что ключ изменился
            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0], Is.TypeOf<RemoveChange<Player>>());
            Assert.That(events[1], Is.TypeOf<AddChange<Player>>());

            Assert.That(((RemoveChange<Player>)events[0]).Index, Is.EqualTo(0));
            Assert.That(((AddChange<Player>)events[1]).Index, Is.EqualTo(0));
        }

        [Test]
        public void UpdateAt_DuplicateEquals_UpdatesCorrectEntry()
        {
            var source = new ObservableList<Player>();

            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 1, level: 10);

            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(p => p.Level);

            p2.Name = "Changed";
            source.UpdateAt(1);

            Assert.That(sorted[0], Is.SameAs(p1));
            Assert.That(sorted[1], Is.SameAs(p2));
            Assert.That(sorted[1].Name, Is.EqualTo("Changed"));
        }

        // -------------------------------------------------------------------
        // Replace — эквивалентный ключ
        // -------------------------------------------------------------------

        [Test]
        public void Replace_SameKey_RaisesReplaceChangeOnSamePosition()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(x => x.Level);

            ReplaceChange<Player>? received = null;
            sorted.Changed += c => received = (ReplaceChange<Player>)c;

            var replacement = P(id: 99, level: 10);
            source.Replace(p1, replacement);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(0));
            Assert.That(received.OldItem, Is.SameAs(p1));
            Assert.That(received.NewItem, Is.SameAs(replacement));

            Assert.That(sorted[0], Is.SameAs(replacement));
            Assert.That(sorted[1], Is.SameAs(p2));

            // Событие содержит правильный replacement
            var replace = (ReplaceChange<Player>)received;
            Assert.That(replace.NewItem, Is.SameAs(replacement));
        }

        // -------------------------------------------------------------------
        // Replace — изменённый ключ
        // -------------------------------------------------------------------

        [Test]
        public void Replace_ChangedKey_MovesItemToNewPosition()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(x => x.Level);

            var replacement = P(id: 99, level: 100);
            source.Replace(p1, replacement);

            Assert.That(sorted[0], Is.SameAs(p2));
            Assert.That(sorted[1], Is.SameAs(replacement));
        }

        [Test]
        public void Replace_ComparatorEquivalentKey_RaisesReplaceNotRemoveAdd()
        {
            var source = new ObservableList<int>();
            source.Add(11);

            var comparer = Comparer<int>.Create((a, b) => (a / 10).CompareTo(b / 10));
            var sorted = source.ObserveOrderBy(x => x, comparer);

            var events = new List<Change<int>>();
            sorted.Changed += events.Add;

            source.Replace(11, 19);   // компаратор считает 11 и 19 эквивалентными

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ReplaceChange<int>>());
        }

        [Test]
        public void Replace_ChangedKey_RaisesRemoveThenAdd()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 20);
            source.Add(p1);
            source.Add(p2);

            var sorted = source.ObserveOrderBy(x => x.Level);

            var events = new List<Change<Player>>();
            sorted.Changed += events.Add;

            var replacement = P(id: 99, level: 100);
            source.Replace(p1, replacement);

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0], Is.TypeOf<RemoveChange<Player>>());
            Assert.That(events[1], Is.TypeOf<AddChange<Player>>());

            var remove = (RemoveChange<Player>)events[0];
            var add = (AddChange<Player>)events[1];

            Assert.That(remove.Index, Is.EqualTo(0));   // p1 был первым
            Assert.That(remove.Item, Is.SameAs(p1));

            Assert.That(add.Index, Is.EqualTo(1));      // replacement встал после p2
            Assert.That(add.Item, Is.SameAs(replacement));
        }

        // -------------------------------------------------------------------
        // Move источника — sorted не меняется
        // -------------------------------------------------------------------

        [Test]
        public void Move_Source_DoesNotAffectSortedOrder()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 30);
            var p2 = P(id: 2, level: 10);
            var p3 = P(id: 3, level: 20);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);

            var events = new List<Change<Player>>();
            sorted.Changed += events.Add;

            source.Move(0, 2);

            Assert.That(events, Is.Empty);
            Assert.That(sorted[0], Is.SameAs(p2));
            Assert.That(sorted[1], Is.SameAs(p3));
            Assert.That(sorted[2], Is.SameAs(p1));
        }

        [Test]
        public void Move_Source_ThenRemoveUsesCorrectEntry()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 30);
            var p2 = P(id: 2, level: 10);
            var p3 = P(id: 3, level: 20);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);

            // source: [p1, p2, p3] → [p2, p1, p3]
            source.Move(0, 1);

            // Удаляем p1 (source-index 1 после перемещения)
            source.Remove(p1);

            Assert.That(sorted.Count, Is.EqualTo(2));
            Assert.That(sorted[0], Is.SameAs(p2));
            Assert.That(sorted[1], Is.SameAs(p3));
        }

        [Test]
        public void Move_Source_ThenUpdateAtUsesCorrectEntry()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 30);
            var p2 = P(id: 2, level: 10);
            var p3 = P(id: 3, level: 20);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);

            // source: [p1, p2, p3] → [p2, p1, p3]
            source.Move(0, 1);

            // Теперь p1 на source-index 1. UpdateAt(1) должен обновить именно p1.
            p1.Name = "Changed";
            source.UpdateAt(1);

            Assert.That(sorted[2], Is.SameAs(p1));
            Assert.That(sorted[2].Name, Is.EqualTo("Changed"));
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        [Test]
        public void Reset_ClearsSorted()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));
            source.Add(P(id: 2, level: 20));

            var sorted = source.ObserveOrderBy(x => x.Level);

            source.Reset();

            Assert.That(sorted.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_RaisesSingleResetChange()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));
            source.Add(P(id: 2, level: 20));

            var sorted = source.ObserveOrderBy(x => x.Level);

            var events = new List<Change<Player>>();
            sorted.Changed += c => events.Add(c);

            source.Reset();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ResetChange<Player>>());
        }

        [Test]
        public void Reset_AllowsFurtherChanges()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 10));

            var sorted = source.ObserveOrderBy(x => x.Level);

            source.Reset();
            source.Add(P(id: 2, level: 5));

            Assert.That(sorted.Count, Is.EqualTo(1));
            Assert.That(sorted[0].Level, Is.EqualTo(5));
        }

        // -------------------------------------------------------------------
        // Равные ключи
        // -------------------------------------------------------------------

        [Test]
        public void EqualKeys_AllItemsPresent()
        {
            var source = new ObservableList<Player>();
            var p1 = P(id: 1, level: 10);
            var p2 = P(id: 2, level: 10);
            var p3 = P(id: 3, level: 10);
            source.Add(p1);
            source.Add(p2);
            source.Add(p3);

            var sorted = source.ObserveOrderBy(x => x.Level);

            // Вставка равных ключей идёт "после существующих" — порядок вставки сохраняется.
            // Это детерминированное поведение текущей реализации, но не контракт.
            Assert.That(sorted[0], Is.SameAs(p1));
            Assert.That(sorted[1], Is.SameAs(p2));
            Assert.That(sorted[2], Is.SameAs(p3));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<Player>();
            var sorted = source.ObserveOrderBy(x => x.Level);

            bool received = false;
            sorted.Changed += _ => received = true;

            sorted.Dispose();
            source.Add(P(id: 1, level: 10));

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<Player>();
            var sorted = source.ObserveOrderBy(x => x.Level);

            sorted.Dispose();

            Assert.DoesNotThrow(() => sorted.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSource()
        {
            var source = new ObservableList<Player>();
            var sorted = source.ObserveOrderBy(x => x.Level);

            sorted.Dispose();

            Assert.DoesNotThrow(() => source.Add(P(id: 1, level: 10)));
            Assert.That(source.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Цепочка
        // -------------------------------------------------------------------

        [Test]
        public void Chain_FilterOrderBy_Works()
        {
            var source = new ObservableList<Player>();
            source.Add(P(id: 1, level: 30, name: "Alive"));
            source.Add(P(id: 2, level: 5, name: "Dead"));
            source.Add(P(id: 3, level: 10, name: "Alive"));

            var sorted = source
                .ObserveWhere(p => p.Name == "Alive")
                .ObserveOrderBy(p => p.Level);

            Assert.That(sorted.Count, Is.EqualTo(2));
            Assert.That(sorted[0].Id, Is.EqualTo(3));   // level 10
            Assert.That(sorted[1].Id, Is.EqualTo(1));   // level 30
        }
    }
}