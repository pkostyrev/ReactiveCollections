using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="SelectManyNode{TSource, TResult}"/> — flattening
    /// вложенных реактивных коллекций.
    /// </summary>
    [TestFixture]
    public class SelectManyNodeTests
    {
        /// <summary>
        /// Вспомогательная модель: контейнер с собственной реактивной коллекцией.
        /// </summary>
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
        public void Constructor_InitializesFromNestedCollections()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1, 2));
            source.Add(CreateBag(2, 3));

            var flat = source.SelectMany(b => b.Items);

            Assert.That(flat.Count, Is.EqualTo(3));
            Assert.That(flat, Is.EqualTo(new[] { 1, 2, 3 }));
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
        // Add из источника
        // -------------------------------------------------------------------

        [Test]
        public void Add_NewSourceItem_FlattensItsItems()
        {
            var source = new ObservableList<Bag>();
            var flat = source.SelectMany(b => b.Items);

            source.Add(CreateBag(1, 10, 20));

            Assert.That(flat.Count, Is.EqualTo(2));
            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));
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
        public void Remove_SourceItem_RemovesItsContribution()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 1, 2);
            var b2 = CreateBag(2, 3);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat.Count, Is.EqualTo(3));

            source.Remove(b1);

            Assert.That(flat.Count, Is.EqualTo(1));
            Assert.That(flat[0], Is.EqualTo(3));
        }

        [Test]
        public void Remove_NonExistentSourceItem_DoesNothing()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 1));

            var flat = source.SelectMany(b => b.Items);

            Assert.DoesNotThrow(() => source.Remove(CreateBag(99, 5)));
            Assert.That(flat.Count, Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Update источника: та же ссылка
        // -------------------------------------------------------------------

        [Test]
        public void Update_SameInnerReference_DoesNothing()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            var originalItems = b1.Items;

            // меняем поле Id, но Items остаётся тем же
            b1.Id = 2;
            source.Update(b1);

            Assert.That(flat.Count, Is.EqualTo(2));
            Assert.That(ReferenceEquals(b1.Items, originalItems), Is.True);
        }

        // -------------------------------------------------------------------
        // Update источника: новая ссылка
        // -------------------------------------------------------------------

        [Test]
        public void Update_NewInnerReference_SwitchesSubscription()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat.Count, Is.EqualTo(2));

            // заменяем внутреннюю коллекцию
            b1.Items = new ObservableList<int>();
            b1.Items.Add(100);
            b1.Items.Add(200);
            b1.Items.Add(300);

            source.Update(b1);

            Assert.That(flat.Count, Is.EqualTo(3));
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

            // изменения в старой коллекции больше не влияют
            oldInner.Add(999);

            Assert.That(flat.Count, Is.EqualTo(0));
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

            Assert.That(flat.Count, Is.EqualTo(2));
            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void InnerRemove_DisappearsFromResult()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            b1.Items.Remove(10);

            Assert.That(flat.Count, Is.EqualTo(1));
            Assert.That(flat[0], Is.EqualTo(20));
        }

        [Test]
        public void InnerUpdate_RaisesUpdate()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);

            Change<int>? received = null;
            flat.Changed += c => received = c;

            b1.Items.Update(10);

            Assert.That(received, Is.Not.Null);
            Assert.That(received!.Type, Is.EqualTo(ChangeType.Update));
        }

        [Test]
        public void InnerReset_RemovesContributionAndRebuilds()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            var b2 = CreateBag(2, 30);
            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat.Count, Is.EqualTo(3));

            b1.Items.Reset();
            b1.Items.Add(100);

            Assert.That(flat.Count, Is.EqualTo(2));
            Assert.That(flat, Is.EqualTo(new[] { 30, 100 }));
        }

        // -------------------------------------------------------------------
        // Replace в источнике
        // -------------------------------------------------------------------

        [Test]
        public void Replace_SourceItem_ReplacesContribution()
        {
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1, 10, 20);
            var b2 = CreateBag(2, 100, 200);
            source.Add(b1);

            var flat = source.SelectMany(b => b.Items);
            Assert.That(flat, Is.EqualTo(new[] { 10, 20 }));

            source.Replace(b1, b2);

            Assert.That(flat.Count, Is.EqualTo(2));
            Assert.That(flat, Is.EqualTo(new[] { 100, 200 }));
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
            Assert.That(flat.Count, Is.EqualTo(3));

            source.Reset();

            Assert.That(flat.Count, Is.EqualTo(0));
        }

        [Test]
        public void Reset_Source_AllowsFurtherChanges()
        {
            var source = new ObservableList<Bag>();
            source.Add(CreateBag(1, 10));
            var flat = source.SelectMany(b => b.Items);

            source.Reset();
            source.Add(CreateBag(2, 20));

            Assert.That(flat.Count, Is.EqualTo(1));
            Assert.That(flat[0], Is.EqualTo(20));
        }

        // -------------------------------------------------------------------
        // Одинаковые по Equals элементы из разных коллекций
        // -------------------------------------------------------------------

        [Test]
        public void DuplicateEquals_FromDifferentInners_AreTrackedSeparately()
        {
            // Оба Bag добавляют по элементу с одинаковым Id.
            // Это разные экземпляры, но Equal по Player.Equals (Id).
            var source = new ObservableList<Bag>();
            var b1 = CreateBag(1);
            var b2 = CreateBag(2);

            source.Add(b1);
            source.Add(b2);

            var flat = source.SelectMany(b => b.Items);
            // оба добавляют по элементу Id = 1 — сейчас пусты
            Assert.That(flat.Count, Is.EqualTo(0));

            b1.Items.Add(1);
            b2.Items.Add(1);

            Assert.That(flat.Count, Is.EqualTo(2));

            // удаление из b1 не должно задеть элемент из b2
            b1.Items.Remove(1);

            Assert.That(flat.Count, Is.EqualTo(1));
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