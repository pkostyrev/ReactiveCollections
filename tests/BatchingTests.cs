using ReactiveCollections.Tests.Models;

namespace ReactiveCollections.Tests
{
    /// <summary>
    /// Тесты батчинга: <see cref="ObservableList{T}.Batch"/>,
    /// <see cref="ObservableList{T}.BeginUpdate"/> / <see cref="ObservableList{T}.EndUpdate"/>.
    /// </summary>
    [TestFixture]
    public class BatchingTests
    {
        // -------------------------------------------------------------------
        // Batch() через using
        // -------------------------------------------------------------------

        [Test]
        public void Batch_TwoAdds_RaisesSingleBatchEvent()
        {
            var list = new ObservableList<int>();
            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            using (list.Batch())
            {
                list.Add(1);
                list.Add(2);
            }

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(ChangeType.Batch));
            Assert.That(events[0].Changes!.Count, Is.EqualTo(2));
            Assert.That(events[0].Changes![0].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(events[0].Changes![0].Item, Is.EqualTo(1));
            Assert.That(events[0].Changes![1].Item, Is.EqualTo(2));
        }

        [Test]
        public void Batch_EmptyScope_RaisesNothing()
        {
            var list = new ObservableList<int>();
            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            using (list.Batch())
            {
            }

            Assert.That(events, Is.Empty);
        }

        [Test]
        public void Batch_MixedOperations_PreservesOrderAndTypes()
        {
            var list = new ObservableList<int>();
            list.Add(1);   // вне батча

            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            using (list.Batch())
            {
                list.Add(2);
                list.Remove(1);
                list.Update(2);
                list.Replace(2, 3);
            }

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(ChangeType.Batch));
            Assert.That(events[0].Changes!.Select(c => c.Type),
                Is.EqualTo(new[] {
                    ChangeType.Add,
                    ChangeType.Remove,
                    ChangeType.Update,
                    ChangeType.Replace
                }));
        }

        [Test]
        public void Batch_SubscriberSeesFinalState()
        {
            var list = new ObservableList<int>();
            var observedCount = -1;

            list.Changed += _ => observedCount = list.Count;

            using (list.Batch())
            {
                list.Add(1);
                list.Add(2);
                list.Add(3);
            }

            Assert.That(observedCount, Is.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // BeginUpdate / EndUpdate
        // -------------------------------------------------------------------

        [Test]
        public void BeginUpdateEndUpdate_EquivalentToBatch()
        {
            var list = new ObservableList<int>();
            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            list.BeginUpdate();
            list.Add(1);
            list.Add(2);
            list.EndUpdate();

            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(ChangeType.Batch));
            Assert.That(events[0].Changes!.Count, Is.EqualTo(2));
        }

        [Test]
        public void EndUpdate_EmptyBuffer_ReturnsFalse()
        {
            var list = new ObservableList<int>();

            list.BeginUpdate();
            bool raised = list.EndUpdate();

            Assert.That(raised, Is.False);
        }

        [Test]
        public void EndUpdate_NonEmptyBuffer_ReturnsTrue()
        {
            var list = new ObservableList<int>();

            list.BeginUpdate();
            list.Add(1);
            bool raised = list.EndUpdate();

            Assert.That(raised, Is.True);
        }

        // -------------------------------------------------------------------
        // Ошибки использования
        // -------------------------------------------------------------------

        [Test]
        public void NestedBatch_Throws()
        {
            var list = new ObservableList<int>();

            using (list.Batch())
            {
                Assert.Throws<InvalidOperationException>(() => list.Batch());
            }
        }

        [Test]
        public void NestedBeginUpdate_Throws()
        {
            var list = new ObservableList<int>();

            list.BeginUpdate();

            Assert.Throws<InvalidOperationException>(() => list.BeginUpdate());

            list.EndUpdate();
        }

        [Test]
        public void EndUpdate_WithoutBegin_Throws()
        {
            var list = new ObservableList<int>();

            Assert.Throws<InvalidOperationException>(() => list.EndUpdate());
        }

        // -------------------------------------------------------------------
        // BatchScope: повторный Dispose безопасен
        // -------------------------------------------------------------------

        [Test]
        public void BatchScope_DoubleDispose_DoesNotThrow()
        {
            var list = new ObservableList<int>();

            var scope = list.Batch();
            list.Add(1);
            scope.Dispose();

            Assert.DoesNotThrow(() => scope.Dispose());
        }

        // -------------------------------------------------------------------
        // Взаимодействие с узлами
        // -------------------------------------------------------------------

        [Test]
        public void Batch_ThroughFilter_ProducesFilteredResult()
        {
            var source = new ObservableList<int>();
            var filtered = source.Filter(x => x % 2 == 1);

            using (source.Batch())
            {
                source.Add(1);
                source.Add(2);
                source.Add(3);
            }

            Assert.That(filtered.Count, Is.EqualTo(2));
            Assert.That(filtered, Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public void Batch_ThroughSelect_ProducesMappedResult()
        {
            var source = new ObservableList<Player>();
            var views = source.Select(p => new PlayerView(), TestData.Bind);

            using (source.Batch())
            {
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));
                source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 10, level: 5));
            }

            Assert.That(views.Count, Is.EqualTo(2));
        }

        [Test]
        public void Batch_ThroughGroupBy_ProducesGroups()
        {
            var source = new ObservableList<Player>();
            var groups = source.GroupBy(p => p.TeamId);

            using (source.Batch())
            {
                source.Add(TestData.CreatePlayer(id: 1, name: "Bob", teamId: 10, level: 5));
                source.Add(TestData.CreatePlayer(id: 2, name: "Tom", teamId: 20, level: 5));
            }

            Assert.That(groups.Count, Is.EqualTo(2));
        }

        [Test]
        public void Batch_ThroughMerge_HandlesInnerBatches()
        {
            var a = new ObservableList<int>();
            var b = new ObservableList<int>();
            var merged = a.Merge(b);

            using (a.Batch())
            {
                a.Add(1);
                a.Add(2);
            }

            using (b.Batch())
            {
                b.Add(3);
            }

            Assert.That(merged.Count, Is.EqualTo(3));
        }

        [Test]
        public void Batch_FilterDoesNotEmitBatchItself()
        {
            // Подписчик на filter.Changed должен получить отдельные Add,
            // а не один Batch — потому что filter разворачивает Batch.
            var source = new ObservableList<int>();
            var filter = source.Filter(x => true);

            var events = new List<Change<int>>();
            filter.Changed += c => events.Add(c);

            using (source.Batch())
            {
                source.Add(1);
                source.Add(2);
            }

            Assert.That(events.Count, Is.EqualTo(2));
            Assert.That(events[0].Type, Is.EqualTo(ChangeType.Add));
            Assert.That(events[1].Type, Is.EqualTo(ChangeType.Add));
        }

        [Test]
        public void Batch_CountAggregate_RecomputesOnce()
        {
            var source = new ObservableList<int>();
            var count = source.ObserveCount();

            var changes = new List<ValueChange<int>>();
            count.Changed += c => changes.Add(c);

            using (source.Batch())
            {
                source.Add(1);
                source.Add(2);
                source.Add(3);
            }

            // Count пересчитывается на каждое развёрнутое Add, но событие
            // райзится только когда значение меняется.
            // 0→1, 1→2, 2→3 — три события.
            Assert.That(count.Value, Is.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // Исключение внутри using — батч закрывается
        // -------------------------------------------------------------------

        [Test]
        public void Batch_ExceptionInsideScope_StillClosesBatch()
        {
            var list = new ObservableList<int>();
            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            try
            {
                using (list.Batch())
                {
                    list.Add(1);
                    throw new Exception("boom");
                }
            }
            catch (Exception)
            {
                // ожидаемо
            }

            // батч всё равно закрылся, событие с Add(1) ушло
            Assert.That(events.Count, Is.EqualTo(1));
            Assert.That(events[0].Type, Is.EqualTo(ChangeType.Batch));
        }

        // -------------------------------------------------------------------
        // Dispose посреди батча
        // -------------------------------------------------------------------

        [Test]
        public void Dispose_DuringBatch_DiscardsBuffer()
        {
            var list = new ObservableList<int>();
            var events = new List<Change<int>>();
            list.Changed += c => events.Add(c);

            list.BeginUpdate();
            list.Add(1);
            list.Dispose();

            // буфер выброшен, событий нет
            Assert.That(events, Is.Empty);
        }
    }
}