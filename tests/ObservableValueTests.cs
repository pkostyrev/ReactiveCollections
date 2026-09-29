namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты <see cref="ObservableValue{T}"/> — реактивного одиночного значения.
    /// </summary>
    [TestFixture]
    public class ObservableValueTests
    {
        // -------------------------------------------------------------------
        // Конструктор
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_StoresInitialValue()
        {
            var v = new ObservableValue<int>(42);

            Assert.That(v.Value, Is.EqualTo(42));
        }

        [Test]
        public void Constructor_NullReference_IsAllowed()
        {
            var v = new ObservableValue<string?>(null);

            Assert.That(v.Value, Is.Null);
        }

        // -------------------------------------------------------------------
        // Set
        // -------------------------------------------------------------------

        [Test]
        public void Set_NewValue_UpdatesAndRaises()
        {
            var v = new ObservableValue<int>(1);

            ValueChange<int>? captured = null;
            v.Changed += c => captured = c;

            v.Set(2);

            Assert.That(v.Value, Is.EqualTo(2));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(1));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(2));
        }

        [Test]
        public void Set_SameValue_StillRaises()
        {
            var v = new ObservableValue<int>(1);

            bool raised = false;
            v.Changed += _ => raised = true;

            v.Set(1);

            Assert.That(raised, Is.True);
        }

        [Test]
        public void Set_ValueIsUpdatedBeforeSubscriberCalled()
        {
            var v = new ObservableValue<int>(1);

            int observed = -1;
            v.Changed += _ => observed = v.Value;

            v.Set(2);

            Assert.That(observed, Is.EqualTo(2));
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        [Test]
        public void Update_DoesNotChangeReference()
        {
            var player = new object();
            var v = new ObservableValue<object>(player);

            ValueChange<object>? captured = null;
            v.Changed += c => captured = c;

            v.Update();

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.SameAs(player));
            Assert.That(captured!.Value.NewValue, Is.SameAs(player));
            Assert.That(v.Value, Is.SameAs(player));
        }

        // -------------------------------------------------------------------
        // Raise: обработка исключений подписчиков
        // -------------------------------------------------------------------

        [Test]
        public void Raise_OneSubscriberThrows_OthersStillGetEvent()
        {
            var v = new ObservableValue<int>(0);
            bool secondCalled = false;

            v.Changed += _ => throw new Exception("boom");
            v.Changed += _ => secondCalled = true;

            Assert.Throws<Exception>(() => v.Set(1));
            Assert.That(secondCalled, Is.True);
        }

        [Test]
        public void Raise_TwoSubscribersThrow_ThrowsAggregateException()
        {
            var v = new ObservableValue<int>(0);

            v.Changed += _ => throw new Exception("first");
            v.Changed += _ => throw new Exception("second");

            var ex = Assert.Throws<AggregateException>(() => v.Set(1));
            Assert.That(ex!.InnerExceptions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Raise_SubscribersCalledInSubscriptionOrder()
        {
            var v = new ObservableValue<int>(0);
            var order = new List<int>();

            v.Changed += _ => order.Add(1);
            v.Changed += _ => order.Add(2);
            v.Changed += _ => order.Add(3);

            v.Set(1);

            Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        // -------------------------------------------------------------------
        // IDisposable
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_IsIdempotent()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.DoesNotThrow(() => v.Dispose());
        }

        [Test]
        public void Dispose_SetAfterDispose_Throws()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.Throws<ObjectDisposedException>(() => v.Set(2));
        }

        [Test]
        public void Dispose_UpdateAfterDispose_Throws()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.Throws<ObjectDisposedException>(() => v.Update());
        }

        [Test]
        public void Dispose_ReadAfterDispose_DoesNotThrow()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.That(v.Value, Is.EqualTo(1));
        }
    }

    /// <summary>
    /// Тесты <see cref="ValueChange{T}"/> — DTO-структуры одного изменения значения.
    /// </summary>
    [TestFixture]
    public class ValueChangeTests
    {
        [Test]
        public void Constructor_StoresOldAndNew()
        {
            var change = new ValueChange<int>(1, 2);

            Assert.That(change.OldValue, Is.EqualTo(1));
            Assert.That(change.NewValue, Is.EqualTo(2));
        }

        [Test]
        public void ToString_FormatsOldAndNew()
        {
            var change = new ValueChange<int>(1, 2);

            Assert.That(change.ToString(), Is.EqualTo("1 -> 2"));
        }
    }
}