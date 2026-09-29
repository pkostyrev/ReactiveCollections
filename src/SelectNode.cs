using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public class SelectNode<TSource, TResult> : ProjectionNode<TSource, TResult>
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

        private readonly Func<TSource, TResult> _factory;
        private readonly Action<TSource, TResult> _updater;
        private readonly List<Entry> _entries = new();

        public SelectNode(
            IObservableList<TSource> source,
            Func<TSource, TResult> factory,
            Action<TSource, TResult> updater) : base(source)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));

            Initialize();
        }

        protected override void OnAdd(Change<TSource> change)
        {
            var sourceItem = change.Item;

            var result = _factory(sourceItem);
            _updater(sourceItem, result);

            _entries.Add(new Entry(sourceItem, result));

            AddInternal(result);
        }

        protected override void OnRemove(Change<TSource> change)
        {
            int index = FindEntryIndex(change.Item);

            if (index < 0)
                return;

            var result = _entries[index].Result;

            _entries.RemoveAt(index);

            RemoveInternal(result);
        }

        protected override void OnUpdate(Change<TSource> change)
        {
            int index = FindEntryIndex(change.Item);

            if (index < 0)
                return;

            var result = _entries[index].Result;

            _updater(change.Item, result);

            UpdateInternal(result);
        }

        private int FindEntryIndex(TSource sourceItem)
        {
            var comparer = EqualityComparer<TSource>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (comparer.Equals(_entries[i].Source, sourceItem))
                    return i;
            }

            return -1;
        }
    }
}
