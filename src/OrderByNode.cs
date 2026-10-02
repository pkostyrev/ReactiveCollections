using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Живой отсортированный список: переупорядочивает элементы источника по ключу.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TKey">Тип ключа сортировки.</typeparam>
    /// <remarks>
    /// Порядок элементов с равными ключами не определён. Для устойчивого порядка
    /// включайте в ключ вторичный признак.
    /// </remarks>
    public sealed class OrderByNode<TSource, TKey> : ObservableNode<TSource>
    { 
        /// <summary>
        /// Идентификатор occurrence в source и sorted-порядке с его ключом
        /// сортировки. Один Entry присутствует в обоих индексных списках.
        /// </summary>
        private sealed class Entry
        {
            public readonly TKey Key;

            public Entry(TKey key)
            {
                Key = key;
            }
        }

        private readonly IObservableList<TSource> _source;
        private readonly Func<TSource, TKey> _selector;
        private readonly IComparer<TKey> _comparer;
        private readonly bool _descending;

        /// <summary>
        /// Source-порядок: <c>_sourceOrder[i]</c> соответствует occurrence
        /// источника с индексом <c>i</c>.
        /// </summary>
        private readonly List<Entry> _sourceOrder = new();

        /// <summary>
        /// Отсортированный порядок: <c>_sortedOrder[i]</c> соответствует
        /// <c>Items[i]</c>.
        /// </summary>
        private readonly List<Entry> _sortedOrder = new();

        /// <param name="selector">Селектор ключа сортировки.</param>
        /// <param name="comparer">
        /// Компаратор ключей. Если <c>null</c> — <see cref="Comparer{TKey}.Default"/>.
        /// </param>
        /// <param name="descending">Обратный порядок сортировки.</param>
        public OrderByNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> selector,
            IComparer<TKey>? comparer = null,
            bool descending = false)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _comparer = comparer ?? Comparer<TKey>.Default;
            _descending = descending;

            Initialize();
        }

        private void Initialize()
        {
            foreach (var item in _source)
            {
                var entry = new Entry(_selector(item));
                _sourceOrder.Add(entry);

                int sortedIndex = FindSortedIndex(entry);
                _sortedOrder.Insert(sortedIndex, entry);
                Items.Insert(sortedIndex, item);
            }

            _source.Changed += OnSourceChanged;
        }

        protected override void DisposeCore()
        {
            _source.Changed -= OnSourceChanged;
            base.DisposeCore();
        }

        // -------------------------------------------------------------------
        // Source changes
        // -------------------------------------------------------------------

        private void OnSourceChanged(Change<TSource> change)
        {
            switch (change)
            {
                case BatchChange<TSource> batch:
                    foreach (var inner in batch.Changes)
                        OnSourceChanged(inner);
                    break;

                case AddChange<TSource> add: OnAdd(add); break;
                case RemoveChange<TSource> remove: OnRemove(remove); break;
                case UpdateChange<TSource> update: OnUpdate(update); break;
                case ReplaceChange<TSource> replace: OnReplace(replace); break;
                case MoveChange<TSource> move: OnMove(move); break;
                case ResetChange<TSource>: OnReset(); break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported change: {change.GetType().Name}");
            }
        }

        private void OnAdd(AddChange<TSource> change)
        {
            var entry = new Entry(_selector(change.Item));

            _sourceOrder.Insert(change.Index, entry);

            int sortedIndex = FindSortedIndex(entry);
            _sortedOrder.Insert(sortedIndex, entry);
            AddAtInternal(sortedIndex, change.Item);
        }

        private void OnRemove(RemoveChange<TSource> change)
        {
            var entry = _sourceOrder[change.Index];
            _sourceOrder.RemoveAt(change.Index);

            int sortedIndex = IndexInSorted(entry);
            _sortedOrder.RemoveAt(sortedIndex);
            RemoveAtInternal(sortedIndex);
        }

        /// <summary>
        /// Ключ эквивалентен по компаратору — <c>Update</c>. Иначе — переезд
        /// через <c>Remove + Add</c>.
        /// </summary>
        private void OnUpdate(UpdateChange<TSource> change)
        {
            var oldEntry = _sourceOrder[change.Index];
            var newEntry = new Entry(_selector(change.Item));

            int oldSortedIndex = IndexInSorted(oldEntry);

            if (CompareKeys(oldEntry.Key, newEntry.Key) == 0)
            {
                _sourceOrder[change.Index] = newEntry;
                _sortedOrder[oldSortedIndex] = newEntry;
                UpdateAtInternal(oldSortedIndex);
                return;
            }

            _sourceOrder[change.Index] = newEntry;
            _sortedOrder.RemoveAt(oldSortedIndex);

            int newSortedIndex = FindSortedIndex(newEntry);
            _sortedOrder.Insert(newSortedIndex, newEntry);

            RemoveAtInternal(oldSortedIndex);
            AddAtInternal(newSortedIndex, change.Item);
        }

        /// <summary>
        /// Ключ эквивалентен по компаратору — <c>Replace</c> на месте.
        /// Иначе — <c>Remove + Add</c>.
        /// </summary>
        private void OnReplace(ReplaceChange<TSource> change)
        {
            var oldEntry = _sourceOrder[change.Index];
            var newEntry = new Entry(_selector(change.NewItem));

            int oldSortedIndex = IndexInSorted(oldEntry);

            if (CompareKeys(oldEntry.Key, newEntry.Key) == 0)
            {
                _sourceOrder[change.Index] = newEntry;
                _sortedOrder[oldSortedIndex] = newEntry;
                ReplaceAtInternal(oldSortedIndex, change.NewItem);
                return;
            }

            _sourceOrder[change.Index] = newEntry;
            _sortedOrder.RemoveAt(oldSortedIndex);

            int newSortedIndex = FindSortedIndex(newEntry);
            _sortedOrder.Insert(newSortedIndex, newEntry);

            RemoveAtInternal(oldSortedIndex);
            AddAtInternal(newSortedIndex, change.NewItem);
        }

        /// <summary>
        /// Перестановка в source не влияет на sorted-порядок — обновляется
        /// только <c>_sourceOrder</c>.
        /// </summary>
        private void OnMove(MoveChange<TSource> change)
        {
            var entry = _sourceOrder[change.FromIndex];
            _sourceOrder.RemoveAt(change.FromIndex);
            _sourceOrder.Insert(change.ToIndex, entry);
        }

        private void OnReset()
        {
            ThrowIfDisposed();

            _sourceOrder.Clear();
            _sortedOrder.Clear();

            bool hadItems = Items.Count > 0;
            Items.Clear();

            foreach (var item in _source)
            {
                var entry = new Entry(_selector(item));
                _sourceOrder.Add(entry);

                int sortedIndex = FindSortedIndex(entry);
                _sortedOrder.Insert(sortedIndex, entry);
                Items.Insert(sortedIndex, item);
            }

            if (hadItems || Items.Count > 0)
                Raise(new ResetChange<TSource>());
        }

        // -------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------

        private int CompareKeys(TKey x, TKey y)
            => _descending
                ? _comparer.Compare(y, x)
                : _comparer.Compare(x, y);

        /// <summary>
        /// Индекс для вставки <paramref name="entry"/> в <c>_sortedOrder</c>.
        /// Равные по компаратору вставляются после существующих.
        /// </summary>
        private int FindSortedIndex(Entry entry)
        {
            int lo = 0, hi = _sortedOrder.Count;

            while (lo < hi)
            {
                int mid = (lo + hi) / 2;

                if (CompareKeys(_sortedOrder[mid].Key, entry.Key) <= 0)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            return lo;
        }

        /// <summary>
        /// Позиция <paramref name="entry"/> в <c>_sortedOrder</c> по ссылке.
        /// </summary>
        private int IndexInSorted(Entry entry)
        {
            for (int i = 0; i < _sortedOrder.Count; i++)
                if (ReferenceEquals(_sortedOrder[i], entry))
                    return i;

            throw new InvalidOperationException("Entry not found in sorted order.");
        }
    }
}