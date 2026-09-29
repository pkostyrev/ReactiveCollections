using NUnit.Framework;
using ReactiveCollections;

namespace ReactiveCollections.Tests
{
    [TestFixture]
    public class ObservableValueTests
    {
        [Test]
        public void Value_Set_RaisesChanged()
        {
            var v = new ObservableValue<int>(1);

            ValueChange<int>? captured = null;
            v.Changed += c => captured = c;

            v.Set(2);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Value.OldValue, Is.EqualTo(1));
            Assert.That(captured!.Value.NewValue, Is.EqualTo(2));
            Assert.That(v.Value, Is.EqualTo(2));
        }

        [Test]
        public void Value_Set_SameValue_StillRaises()
        {
            var v = new ObservableValue<int>(1);

            bool raised = false;
            v.Changed += _ => raised = true;

            v.Set(1);

            Assert.That(raised, Is.True);
        }

        [Test]
        public void Value_Update_RaisesWithoutChangingValue()
        {
            var player = new object();
            var v = new ObservableValue<object>(player);

            ValueChange<object>? captured = null;
            v.Changed += c => captured = c;

            v.Update();

            Assert.That(captured!.Value.OldValue, Is.SameAs(player));
            Assert.That(captured!.Value.NewValue, Is.SameAs(player));
            Assert.That(v.Value, Is.SameAs(player));
        }

        [Test]
        public void Value_Dispose_IsIdempotent()
        {
            var v = new ObservableValue<int>(1);

            v.Dispose();

            Assert.DoesNotThrow(() => v.Dispose());
        }

        [Test]
        public void Value_SetAfterDispose_Throws()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.Throws<ObjectDisposedException>(() => v.Set(2));
        }

        [Test]
        public void Value_ReadAfterDispose_DoesNotThrow()
        {
            var v = new ObservableValue<int>(1);
            v.Dispose();

            Assert.That(v.Value, Is.EqualTo(1));
        }

        [Test]
        public void Value_OneSubscriberThrows_OthersStillGetEvent()
        {
            var v = new ObservableValue<int>(0);
            bool secondCalled = false;

            v.Changed += _ => throw new Exception("boom");
            v.Changed += _ => secondCalled = true;

            Assert.Throws<Exception>(() => v.Set(1));
            Assert.That(secondCalled, Is.True);
        }
    }
}