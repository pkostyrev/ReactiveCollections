using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public abstract class KeyedProjectionNode<TKey, TSource, TResult> : ProjectionNode<TSource, TResult>
    {
        private sealed class Entry
        {
            public readonly TSource Source;
            public readonly TResult Result;

            public Entry(TSource source, TResult result)
            {
                Source = source;
                Result = result;
            }
        }

        private readonly Dictionary<TKey, TResult> _results = new();
        private readonly List<Entry> _itemEntries = new();
        private readonly Func<TSource, TKey> _keySelector;
        private readonly IEqualityComparer<TSource> _itemComparer;

        protected KeyedProjectionNode(
            IObservableList<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TSource>? comparer = null) : base(source)
        {
            _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
            _itemComparer = comparer ?? EqualityComparer<TSource>.Default;
        }

        protected override void OnAdd(Change<TSource> change)
        {
            var item = change.Item;
            var key = _keySelector(item);

            if (!_results.TryGetValue(key, out var result))
            {
                result = CreateResult(key);
                _results.Add(key, result);
                AddInternal(result);
            }

            AddItem(result, item);
            _itemEntries.Add(new Entry(item, result));
        }

        protected override void OnRemove(Change<TSource> change)
        {
            var item = change.Item;

            int index = FindItemEntryIndex(item);
            if (index < 0)
                return;

            var result = _itemEntries[index].Result;

            RemoveItem(result, item);
            _itemEntries.RemoveAt(index);

            if (!IsEmpty(result))
                return;

            _results.Remove(GetKey(result));
            RemoveInternal(result);
        }

        protected override void OnUpdate(Change<TSource> change)
        {
            var item = change.Item;

            int index = FindItemEntryIndex(item);
            if (index < 0)
                return;

            var oldResult = _itemEntries[index].Result;
            var oldKey = GetKey(oldResult);
            var newKey = _keySelector(item);

            // элемент остался в той же группе
            if (EqualityComparer<TKey>.Default.Equals(oldKey, newKey))
            {
                UpdateItem(oldResult, item);
                return;
            }

            // элемент переехал в другую группу
            RemoveItem(oldResult, item);

            if (IsEmpty(oldResult))
            {
                _results.Remove(oldKey);
                RemoveInternal(oldResult);
            }

            if (!_results.TryGetValue(newKey, out var newResult))
            {
                newResult = CreateResult(newKey);
                _results.Add(newKey, newResult);
                AddInternal(newResult);
            }

            AddItem(newResult, item);
            _itemEntries[index] = new Entry(item, newResult);
        }

        protected override void OnReset(Change<TSource> change)
        {
            _results.Clear();
            _itemEntries.Clear();
            base.OnReset(change);
        }

        private int FindItemEntryIndex(TSource item)
        {
            for (int i = 0; i < _itemEntries.Count; i++)
            {
                if (_itemComparer.Equals(_itemEntries[i].Source, item))
                    return i;
            }

            return -1;
        }

        protected abstract TResult CreateResult(TKey key);
        protected abstract void AddItem(TResult result, TSource item);
        protected abstract void RemoveItem(TResult result, TSource item);
        protected virtual void UpdateItem(TResult result, TSource item) { }
        protected abstract bool IsEmpty(TResult result);
        protected abstract TKey GetKey(TResult result);
    }
}

