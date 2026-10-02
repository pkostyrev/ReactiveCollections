using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Пропускает элементы источника, удовлетворяющие предикату. Порядок сохраняется.
    /// </summary>
    /// <remarks>
    /// Хранит маппинг source-index → filter-index, что позволяет корректно
    /// обрабатывать Move, AddAt, RemoveAt и дубликаты по Equals.
    /// Структурные операции — O(N) в худшем случае.
    /// </remarks>
    public class FilterNode<T> : ProjectionNode<T, T>
    {
        private readonly Func<T, bool> _predicate;

        /// <summary>
        /// Для каждого индекса источника хранит индекс соответствующего элемента
        /// в результате или −1, если элемент не проходит предикат.
        /// Инвариант: Count == Source.Count.
        /// </summary>
        private readonly List<int> _sourceToFilter = new();

        public FilterNode(IObservableList<T> source, Func<T, bool> predicate) : base(source)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            Initialize();
        }

        protected override void OnAdd(AddChange<T> change)
        {
            int sourceIndex = change.Index;
            bool valid = _predicate(change.Item);

            int filterIndex = -1;
            if (valid)
            {
                filterIndex = CountBefore(sourceIndex);
            }

            _sourceToFilter.Insert(sourceIndex, filterIndex);

            if (!valid)
                return;

            AddAtInternal(filterIndex, change.Item);

            for (int j = sourceIndex + 1; j < _sourceToFilter.Count; j++)
                if (_sourceToFilter[j] >= filterIndex) _sourceToFilter[j]++;
        }

        protected override void OnRemove(RemoveChange<T> change)
        {
            int sourceIndex = change.Index;
            int filterIndex = _sourceToFilter[sourceIndex];

            _sourceToFilter.RemoveAt(sourceIndex);

            if (filterIndex < 0)
                return;

            RemoveAtInternal(filterIndex);

            for (int j = sourceIndex; j < _sourceToFilter.Count; j++)
                if (_sourceToFilter[j] > filterIndex) _sourceToFilter[j]--;
        }

        protected override void OnUpdate(UpdateChange<T> change)
        {
            int sourceIndex = change.Index;
            int oldFilterIndex = _sourceToFilter[sourceIndex];
            bool wasValid = oldFilterIndex >= 0;
            bool isValid = _predicate(change.Item);

            if (wasValid && isValid)
            {
                UpdateAtInternal(oldFilterIndex);
            }
            else if (wasValid && !isValid)
            {
                RemoveAtInternal(oldFilterIndex);
                _sourceToFilter[sourceIndex] = -1;
                for (int j = sourceIndex + 1; j < _sourceToFilter.Count; j++)
                    if (_sourceToFilter[j] > oldFilterIndex) _sourceToFilter[j]--;
            }
            else if (!wasValid && isValid)
            {
                int filterIndex = CountBefore(sourceIndex);
                _sourceToFilter[sourceIndex] = filterIndex;
                AddAtInternal(filterIndex, change.Item);
                for (int j = sourceIndex + 1; j < _sourceToFilter.Count; j++)
                    if (_sourceToFilter[j] >= filterIndex) _sourceToFilter[j]++;
            }
        }

        protected override void OnReplace(ReplaceChange<T> change)
        {
            int sourceIndex = change.Index;
            int oldFilterIndex = _sourceToFilter[sourceIndex];
            bool wasValid = oldFilterIndex >= 0;
            bool newValid = _predicate(change.NewItem);

            if (wasValid && newValid)
            {
                ReplaceAtInternal(oldFilterIndex, change.NewItem);
            }
            else if (wasValid && !newValid)
            {
                RemoveAtInternal(oldFilterIndex);
                _sourceToFilter[sourceIndex] = -1;
                for (int j = sourceIndex + 1; j < _sourceToFilter.Count; j++)
                    if (_sourceToFilter[j] > oldFilterIndex) _sourceToFilter[j]--;
            }
            else if (!wasValid && newValid)
            {
                int filterIndex = CountBefore(sourceIndex);
                _sourceToFilter[sourceIndex] = filterIndex;
                AddAtInternal(filterIndex, change.NewItem);
                for (int j = sourceIndex + 1; j < _sourceToFilter.Count; j++)
                    if (_sourceToFilter[j] >= filterIndex) _sourceToFilter[j]++;
            }
        }

        protected override void OnMove(MoveChange<T> change)
        {
            int from = change.FromIndex;
            int to = change.ToIndex;
            int oldFilterIndex = _sourceToFilter[from];

            _sourceToFilter.RemoveAt(from);
            _sourceToFilter.Insert(to, oldFilterIndex);

            if (oldFilterIndex < 0)
                return;

            // Пересобираем filter-индексы, запоминаем новый индекс перемещённого.
            int newFilterIndex = -1;
            int rank = 0;

            for (int j = 0; j < _sourceToFilter.Count; j++)
            {
                if (_sourceToFilter[j] < 0)
                    continue;

                if (j == to) newFilterIndex = rank;
                _sourceToFilter[j] = rank;
                rank++;
            }

            MoveInternal(oldFilterIndex, newFilterIndex);
        }

        protected override void OnReset(ResetChange<T> change)
        {
            ThrowIfDisposed();

            bool hadItems = Items.Count > 0;

            Items.Clear();
            _sourceToFilter.Clear();

            foreach (var item in Source)
            {
                if (_predicate(item))
                {
                    _sourceToFilter.Add(Items.Count);
                    Items.Add(item);
                }
                else
                {
                    _sourceToFilter.Add(-1);
                }
            }

            if (hadItems || Items.Count > 0)
                Raise(new ResetChange<T>());
        }

        /// <summary>
        /// Количество валидных элементов до позиции <paramref name="sourceIndex"/>.
        /// </summary>
        private int CountBefore(int sourceIndex)
        {
            int count = 0;
            for (int j = 0; j < sourceIndex; j++)
                if (_sourceToFilter[j] >= 0) count++;
            return count;
        }
    }
}