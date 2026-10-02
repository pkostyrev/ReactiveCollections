using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="SelectManyNode{TSource, TResult}"/> — flattening
    /// вложенных реактивных коллекций (Concat-модель).
    /// </summary>
    [TestFixture]
    public class SelectManyNodeTests
    {
        private sealed class Bag
        {
            public int Id;
            public ObservableList<int> Items = new();
        }

        private static Bag CreateBag(int id, params int[] items)
        {
            var bag = new Bag { Id = id };
            foreach (var item in items)
                bag.Items.Add(item);
            return bag;
        }

        // -------------------------------------------------------------------
        // Инициализация
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_EmptySource_ResultIsEmpty()
        {
            var source = new ObservableList<Bag>();

            var flat = source.SelectMany(b => b.Items);

            Assert.That(flat.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_ConcatOrder_PreservesSourceOrder()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1, 2));
            source.Add(CreateBag(2, 3));
            source.Add(CreateBag(3, 4, 5));

            var flat = source.SelectMany(b => b.Items);

            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void Constructor_EmptyInnerInMiddle_DoesNotAffectOrder()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1));
            source.Add(new Bag { Id = 2 });   // пустой
            source.Add(CreateBag(3, 3));

            var flat = source.SelectMany(b => b.Items);

            Assert.That(flat, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void Constructor_NullSource_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                ObservableExtensions.SelectMany<Bag, int>(null!, b => b.Items));
        }

        [Test]
        public void Constructor_NullSelector_Throws()
        {
            var source = new ObservableList<Bag>();

            Assert.Throws<ArgumentNullException>(() =>
                source.SelectMany((Func<Bag, IObservableList<int>>)null!));
        }

        [Test]
        public void Constructor_SelectorReturnsNull_Throws()
        {
            var source = new ObservableList<Bag>();
            source.Add(new Bag { Id = 1 });

            Assert.Throws<InvalidOperationException>(() =>
                source.SelectMany(b => (IObservableList<int>)null!));
        }

        // -------------------------------------------------------------------
        // Add в источнике
        // -------------------------------------------------------------------

        [Test]
        public void Add_NewSourceItem_AppendsBlockAtEnd()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10));
            var flat = source.SelectMany(b => b.Items);

            source.Add(CreateBag(2, 20, 30));

            Assert.That(flat, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void AddAt_InsertsBlockAtCorrectPosition()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1));
            source.Add(CreateBag(2, 2));
            var flat = source.SelectMany(b => b.Items);

            // Вставляем между b1 и b2
            source.AddAt(1, CreateBag(3, 30, 31));

            Assert.That(flat, Is.EqualTo(new[] { 1, 30, 31, 2 }));
        }

        [Test]
        public void Add_EmptyBag_DoesNotChangeResult()
        {
            var source = new ObservableList<Bag>();
            var flat = source.SelectMany(b => b.Items);

            source.Add(new Bag { Id = 1 });

            Assert.That(flat.Count, Is.EqualTo(0));
        }

        // -------------------------------------------------------------------
        // Remove из источника
        // -------------------------------------------------------------------

        [Test]
        public void Remove_MiddleBag_RemovesOnlyItsBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1, 2);
            var b2 = CreateBag(2, 3, 4);
            var b3 = CreateBag(3, 5);
            source.Add(b1);
            source.Add(b2);
            source.Add(b3);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));

            source.Remove(b2);

            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 5 }));
        }

        [Test]
        public void Remove_NonExistent_DoesNothing()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1));

            var flat = source.SelectMany(b => b.Items);

            Assert.DoesNotThrow(() => source.Remove(CreateBag(99, 5)));
            Assert.That(flat.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update источника
        // -------------------------------------------------------------------

        [Test]
        public void Update_SameInnerReference_KeepsBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            var originalItems = b1.Items;

            bool raised = false;
            flat.Changed += _ => raised = true;

            b1.Id = 2;
            source.Update(b1);

            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));
            Assert.That(ReferenceEquals(b1.Items, originalItems), Is.True);
            Assert.That(raised, Is.False);
        }

        [Test]
        public void Update_NewInnerReference_ReplacesBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            b1.Items = new ObservableList<int>();
            b1.Items.Add(100);
            b1.Items.Add(200);
            b1.Items.Add(300);

            source.Update(b1);

            Assert.That(flat, Is.EqualTo(new[] { 100, 200, 300 }));
        }

        [Test]
        public void Update_NewInnerReference_UnsubscribesFromOld()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            var oldInner = b1.Items;

            b1.Items = new ObservableList<int>();
            source.Update(b1);

            oldInner.Add(999);

            Assert.That(flat.Count, Is.EqualTo(0));
        }

        [Test]
        public void Update_MiddleBlock_KeepsOtherBlocksIntact()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1);
            var b2 = CreateBag(2, 2, 3);
            var b3 = CreateBag(3, 4);
            source.Add(b1);
            source.Add(b2);
            source.Add(b3);

            var flat = source.SelectMany(b => b.Items);

            b2.Items = new ObservableList<int>();
            b2.Items.Add(99);
            source.Update(b2);

            Assert.That(flat, Is.EqualTo(new[] { 1, 99, 4 }));
        }

        // -------------------------------------------------------------------
        // Replace в источнике
        // -------------------------------------------------------------------

        [Test]
        public void Replace_SourceItem_SwapsBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            var b2 = CreateBag(2, 100, 200);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));

            source.Replace(b1, b2);

            Assert.That(flat, Is.EqualTo(new[] { 100, 200 }));
        }

        [Test]
        public void Replace_SameInnerReference_DoesNotChangeFlat()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            bool raised = false;
            flat.Changed += _ => raised = true;

            // b2 с той же самой inner-коллекцией
            var b2 = new Bag { Id = 2, Items = b1.Items };
            source.Replace(b1, b2);

            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));
            Assert.That(raised, Is.False);
        }

        // -------------------------------------------------------------------
        // Move в источнике
        // -------------------------------------------------------------------

        [Test]
        public void Move_SourceBlock_RepositionsFlatBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1, 2);
            var b2 = CreateBag(2, 3);
            var b3 = CreateBag(3, 4, 5);
            source.Add(b1);
            source.Add(b2);
            source.Add(b3);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));

            // b1 (блок [1,2]) в конец
            source.Move(0, 2);

            Assert.That(flat, Is.EqualTo(new[] { 3, 4, 5, 1, 2 }));
        }

        // -------------------------------------------------------------------
        // Изменения вложенных коллекций
        // -------------------------------------------------------------------

        [Test]
        public void InnerAdd_AppearsInResult()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            b1.Items.Add(20);

            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void InnerAddAt_InsertsAtCorrectFlatPosition()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1, 2, 3);
            var b2 = CreateBag(2, 5, 7);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            AddChange<int>? received = null;
            flat.Changed += c => received = (AddChange<int>)c;

            b2.Items.AddAt(1, 6);

            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3, 5, 6, 7 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(4));
        }

        [Test]
        public void InnerUpdate_TranslatesIndexToFlat()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            var b2 = CreateBag(2, 20, 30);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            UpdateChange<int>? received = null;
            flat.Changed += c => received = (UpdateChange<int>)c;

            b2.Items.UpdateAt(1);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Index, Is.EqualTo(2));   // 1 (b1.Count) + 1
        }

        [Test]
        public void InnerRemove_DisappearsFromResult()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            b1.Items.Remove(10);

            Assert.That(flat, Is.EqualTo(new[] { 20 }));
        }

        [Test]
        public void InnerUpdate_RaisesUpdateChange()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            Change<int>? received = null;
            flat.Changed += c => received = c;

            b1.Items.Update(10);

            Assert.That(received, Is.TypeOf<UpdateChange<int>>());
            Assert.That(((UpdateChange<int>)received!).Index, Is.EqualTo(0));
        }

        [Test]
        public void InnerMove_TranslatesIndexToFlat()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1, 2, 3);
            var b2 = CreateBag(2, 10, 11, 12);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            MoveChange<int>? received = null;
            flat.Changed += c => received = (MoveChange<int>)c;

            b2.Items.Move(2, 0);

            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3, 12, 10, 11 }));
            Assert.That(received, Is.Not.Null);
            Assert.That(received!.FromIndex, Is.EqualTo(5));
            Assert.That(received.ToIndex, Is.EqualTo(3));
        }

        [Test]
        public void InnerReset_KeepsOtherBlocksIntact()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            var b2 = CreateBag(2, 30);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat, Is.EqualTo(new[] { 10, 20, 30 }));

            b1.Items.Reset();

            Assert.That(flat, Is.EqualTo(new[] { 30 }));
        }

        [Test]
        public void InnerReset_ThenAdd_PlacesInCorrectBlock()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            var b2 = CreateBag(2, 30);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            b1.Items.Reset();
            b1.Items.Add(100);

            // блок b1 находится перед блоком b2
            Assert.That(flat, Is.EqualTo(new[] { 100, 30 }));
        }

        // -------------------------------------------------------------------
        // Reset источника
        // -------------------------------------------------------------------

        [Test]
        public void Reset_Source_ClearsResult()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10, 20));
            source.Add(CreateBag(2, 30));

            var flat = source.SelectMany(b => b.Items);

            source.Reset();

            Assert.That(flat.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_Source_RaisesSingleResetChange()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10));
            source.Add(CreateBag(2, 20));

            var flat = source.SelectMany(b => b.Items);

            var events = new List<Change<int>>();
            flat.Changed += c => events.Add(c);

            source.Reset();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0], Is.TypeOf<ResetChange<int>>());
        }

        [Test]
        public void Reset_Source_AllowsFurtherChanges()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10));
            var flat = source.SelectMany(b => b.Items);

            source.Reset();
            source.Add(CreateBag(2, 20));

            Assert.That(flat, Is.EqualTo(new[] { 20 }));
        }

        [Test]
        public void Reset_Source_ThenInnerChanges_StillWork()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10));
            var flat = source.SelectMany(b => b.Items);

            source.Reset();

            var b2 = CreateBag(2, 20);
            source.Add(b2);

            // после Reset подписки должны быть восстановлены корректно
            b2.Items.Add(30);

            Assert.That(flat, Is.EqualTo(new[] { 20, 30 }));
        }

        // -------------------------------------------------------------------
        // Дубликаты между разными inner
        // -------------------------------------------------------------------

        [Test]
        public void DuplicateValues_DifferentInners_TrackedSeparately()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1);
            var b2 = CreateBag(2);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            b1.Items.Add(1);
            b2.Items.Add(1);

            Assert.That(flat, Is.EqualTo(new[] { 1, 1 }));

            // удаление из b1 не задевает блок b2
            b1.Items.RemoveAt(0);

            Assert.That(flat, Is.EqualTo(new[] { 1 }));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_UnsubscribesFromSource()
        {
            var source = new ObservableList<Bag>();
            var flat = source.SelectMany(b => b.Items);

            bool received = false;
            flat.Changed += _ => received = true;

            flat.Dispose();
            source.Add(CreateBag(1, 10));

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_UnsubscribesFromAllInners()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            var b2 = CreateBag(2, 20);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);

            bool received = false;
            flat.Changed += _ => received = true;

            flat.Dispose();

            b1.Items.Add(100);
            b2.Items.Add(200);

            Assert.That(received, Is.False);
        }

        [Test]
        public void Dispose_IsIdempotent()
        {
            var source = new ObservableList<Bag>();
            var flat = source.SelectMany(b => b.Items);

            flat.Dispose();

            Assert.DoesNotThrow(() => flat.Dispose());
        }

        [Test]
        public void Dispose_DoesNotDisposeSources()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            flat.Dispose();

            Assert.DoesNotThrow(() => source.Add(CreateBag(2, 20)));
            Assert.DoesNotThrow(() => b1.Items.Add(100));
        }
    }
}