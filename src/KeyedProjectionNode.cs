using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// База для группирующих проекций: ключ → группа, элемент → группа.
    /// </summary>
    /// <remarks>
    /// Операции над occurrence источника адресуются по индексам из
    /// <c>Change&lt;TSource&gt;</c>. <c>Equals</c> не используется для поиска
    /// элемента — это позволяет корректно обрабатывать дубликаты.
    /// </remarks>
    public abstract class KeyedProjectionNode<TKey, TSource, TResult> : ProjectionNode<TSource, TResult>
    {
        /// <summary>Связывает occurrence источника с его группой.</summary>
        private sealed class Entry
        {
            public readonly TResult Result;

            public Entry(TResult result)
            {
                Result = result;
            }
        }

        private readonly Dictionary<TKey, TResult> _results = new();

        /// <summary>
        /// Для каждого occurrence источника хранит соответствующую группу.
        /// Инвариант: Count == Source.Count.
        /// </summary>
        private readonly List<Entry> _itemEntries = new();
        private readonly Func<TSource, TKey> _keySelector;

        /// <param name="keySelector">Селектор ключа группы.</param>
        protected KeyedProjectionNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> keySelector) : base(source)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        }

        protected override void OnAdd(AddChange<TSource> change)
        {
            var item = change.Item;
            int sourceIndex = change.Index;
            var key = _keySelector(item);

            var result = GetOrCreateGroup(key);
            int innerIndex = CountInnerBefore(sourceIndex, result);

            AddItemAt(result, innerIndex, item);
            _itemEntries.Insert(sourceIndex, new Entry(result));
        }

        protected override void OnRemove(RemoveChange<TSource> change)
        {
            int sourceIndex = change.Index;
            var result = _itemEntries[sourceIndex].Result;

            int innerIndex = CountInnerBefore(sourceIndex, result);

            RemoveItemAt(result, innerIndex);
            _itemEntries.RemoveAt(sourceIndex);

            if (!IsEmpty(result))
                return;

            _results.Remove(GetKey(result));
            RemoveInternal(result);
        }

        protected override void OnUpdate(UpdateChange<TSource> change)
        {
            int sourceIndex = change.Index;
            var oldResult = _itemEntries[sourceIndex].Result;
            var oldKey = GetKey(oldResult);
            var newKey = _keySelector(change.Item);

            if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            {
                int innerIndex = CountInnerBefore(sourceIndex, oldResult);
                UpdateItem(oldResult, innerIndex, change.Item);
                return;
            }

            // Переезд в другую группу.
            int oldInner = CountInnerBefore(sourceIndex, oldResult);
            RemoveItemAt(oldResult, oldInner);
            _itemEntries.RemoveAt(sourceIndex);

            if (IsEmpty(oldResult))
            {
                _results.Remove(oldKey);
                RemoveInternal(oldResult);
            }

            var newResult = GetOrCreateGroup(newKey);
            int newInner = CountInnerBefore(sourceIndex, newResult);

            AddItemAt(newResult, newInner, change.Item);
            _itemEntries.Insert(sourceIndex, new Entry(newResult));
        }

        protected override void OnReplace(ReplaceChange<TSource> change)
        {
            int sourceIndex = change.Index;
            var result = _itemEntries[sourceIndex].Result;
            var oldKey = GetKey(result);
            var newKey = _keySelector(change.NewItem);

            if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            {
                int innerIndex = CountInnerBefore(sourceIndex, result);
                ReplaceItemAt(result, innerIndex, change.NewItem);
                return;
            }

            // Ключ изменился — переезд в другую группу.
            int oldInner = CountInnerBefore(sourceIndex, result);
            RemoveItemAt(result, oldInner);
            _itemEntries.RemoveAt(sourceIndex);

            if (IsEmpty(result))
            {
                _results.Remove(oldKey);
                RemoveInternal(result);
            }

            var newResult = GetOrCreateGroup(newKey);
            int newInner = CountInnerBefore(sourceIndex, newResult);

            AddItemAt(newResult, newInner, change.NewItem);
            _itemEntries.Insert(sourceIndex, new Entry(newResult));
        }

        protected override void OnMove(MoveChange<TSource> change)
        {
            int from = change.FromIndex;
            int to = change.ToIndex;

            var entry = _itemEntries[from];
            var result = entry.Result;

            int oldInner = CountInnerBefore(from, result);

            _itemEntries.RemoveAt(from);
            _itemEntries.Insert(to, entry);

            int newInner = CountInnerBefore(to, result);

            if (oldInner != newInner)
                MoveItem(result, oldInner, newInner);
        }

        protected override void OnReset(ResetChange<TSource> change)
        {
            _results.Clear();
            _itemEntries.Clear();
            base.OnReset(change);
        }

        /// <summary>
        /// Возвращает существующую группу по ключу или создаёт новую.
        /// Созданная группа добавляется в <c>Items</c>.
        /// </summary>
        private TResult GetOrCreateGroup(TKey key)
        {
            if (_results.TryGetValue(key, out var result))
                return result;

            result = CreateResult(key);
            _results.Add(key, result);
            AddInternal(result);
            return result;
        }

        /// <summary>
        /// Количество элементов группы <paramref name="group"/> до позиции
        /// <paramref name="sourceIndex"/> в source-порядке.
        /// </summary>
        private int CountInnerBefore(int sourceIndex, TResult group)
        {
            int count = 0;
            for (int i = 0; i < sourceIndex; i++)
            {
                if (ReferenceEquals(_itemEntries[i].Result, group))
                    count++;
            }
            return count;
        }

        /// <summary>Создаёт новый результат для указанного ключа.</summary>
        protected abstract TResult CreateResult(TKey key);

        /// <summary>Добавляет элемент в результат на позицию <paramref name="index"/>.</summary>
        protected abstract void AddItemAt(TResult result, int index, TSource item);

        /// <summary>Удаляет элемент из результата по его позиции.</summary>
        protected abstract void RemoveItemAt(TResult result, int index);

        /// <summary>Уведомляет результат об изменении элемента на позиции.</summary>
        protected virtual void UpdateItem(TResult result, int index, TSource item) { }

        /// <summary>Заменяет элемент на позиции <paramref name="index"/>.</summary>
        protected abstract void ReplaceItemAt(TResult result, int index, TSource newItem);

        /// <summary>Перемещает элемент внутри результата.</summary>
        protected abstract void MoveItem(TResult result, int fromIndex, int toIndex);

        /// <summary>Пуст ли результат.</summary>
        protected abstract bool IsEmpty(TResult result);

        /// <summary>Возвращает ключ результата.</summary>
        protected abstract TKey GetKey(TResult result);
    }
}