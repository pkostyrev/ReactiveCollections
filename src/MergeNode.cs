using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public sealed class MergeNode<T> : ObservableNode<T>
    {
        private sealed class Entry
        {
            public readonly T Item;
            public readonly int SourceId;

            public Entry(T item, int sourceId)
            {
                Item = item;
                SourceId = sourceId;
            }
        }

        private const int SourceFirst = 0;
        private const int SourceSecond = 1;

        private readonly IObservableList<T> _first;
        private readonly IObservableList<T> _second;
        private readonly List<Entry> _entries = new();

        public MergeNode(
            IObservableList<T> first,
            IObservableList<T> second)
        {
            _first = first
                ?? throw new ArgumentNullException(nameof(first));

            _second = second
                ?? throw new ArgumentNullException(nameof(second));

            Initialize();

            _first.Changed += OnFirstChanged;
            _second.Changed += OnSecondChanged;
        }

        private void Initialize()
        {
            foreach (var item in _first)
                AddFromSource(item, SourceFirst);

            foreach (var item in _second)
                AddFromSource(item, SourceSecond);
        }

        private void OnFirstChanged(Change<T> change) => ApplyChange(change, SourceFirst);

        private void OnSecondChanged(Change<T> change) => ApplyChange(change, SourceSecond);

        private void ApplyChange(Change<T> change, int sourceId)
        {
            switch (change.Type)
            {
                case ChangeType.Add:
                    AddFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Remove:
                    RemoveFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Update:
                    UpdateFromSource(change.Item, sourceId);
                    break;

                case ChangeType.Replace:
                    ReplaceFromSource(change.OldItem!, change.Item, sourceId);
                    break;

                case ChangeType.Reset:
                    Rebuild();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void AddFromSource(T item, int sourceId)
        {
            AddInternal(item);
            _entries.Add(new Entry(item, sourceId));
        }

        private void RemoveFromSource(T item, int sourceId)
        {
            int index = FindEntryIndex(item, sourceId);

            if (index < 0)
                return;

            RemoveAtInternal(index);
            _entries.RemoveAt(index);
        }

        private void UpdateFromSource(T item, int sourceId)
        {
            int index = FindEntryIndex(item, sourceId);

            if (index < 0)
                return;

            UpdateAtInternal(index);

            // Для value-type T обновляем запись, чтобы Equals работал с актуальным значением.
            // Для reference-type это тот же экземпляр — замена Entry безвредна.
            _entries[index] = new Entry(item, sourceId);
        }

        private void ReplaceFromSource(T oldItem, T newItem, int sourceId)
        {
            int index = FindEntryIndex(oldItem, sourceId);

            if (index < 0)
                return;

            ReplaceAtInternal(index, newItem);
            _entries[index] = new Entry(newItem, sourceId);
        }

        private int FindEntryIndex(T item, int sourceId)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].SourceId != sourceId)
                    continue;

                if (comparer.Equals(_entries[i].Item, item))
                    return i;
            }

            return -1;
        }

        private void Rebuild()
        {
            ResetInternal();
            _entries.Clear();

            foreach (var item in _first)
                AddFromSource(item, SourceFirst);

            foreach (var item in _second)
                AddFromSource(item, SourceSecond);
        }
    }
}