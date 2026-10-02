using System;

namespace ReactiveCollections
{
     /// <summary>
    /// Объединение двух источников одного типа по семантике Concat.
    /// </summary>
    /// <typeparam name="T">Тип элемента обоих источников.</typeparam>
    /// <remarks>
    public sealed class MergeNode<T> : ObservableNode<T>
    {
        private const int SourceFirst = 0;
        private const int SourceSecond = 1;

        private readonly IObservableList<T> _first;
        private readonly IObservableList<T> _second;

        public MergeNode(IObservableList<T> first, IObservableList<T> second)
        {
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));

            Initialize();

            _first.Changed += OnFirstChanged;
            _second.Changed += OnSecondChanged;
        }

        private void Initialize()
        {
            foreach (var item in _first)
                AddInternal(item);

            foreach (var item in _second)
                AddInternal(item);
        }

        protected override void DisposeCore()
        {
            _first.Changed -= OnFirstChanged;
            _second.Changed -= OnSecondChanged;
            base.DisposeCore();
        }

        private void OnFirstChanged(Change<T> change) => ApplyChange(change, SourceFirst);
        private void OnSecondChanged(Change<T> change) => ApplyChange(change, SourceSecond);

        private void ApplyChange(Change<T> change, int sourceId)
        {
            switch (change)
            {
                case BatchChange<T> batch:
                    foreach (var inner in batch.Changes)
                        ApplyChange(inner, sourceId);
                    break;

                case AddChange<T> add:
                    AddAtInternal(OffsetFor(sourceId) + add.Index, add.Item);
                    break;

                case RemoveChange<T> remove:
                    RemoveAtInternal(OffsetFor(sourceId) + remove.Index);
                    break;

                case UpdateChange<T> update:
                    UpdateAtInternal(OffsetFor(sourceId) + update.Index);
                    break;

                case ReplaceChange<T> replace:
                    ReplaceAtInternal(OffsetFor(sourceId) + replace.Index, replace.NewItem);
                    break;

                case MoveChange<T> move:
                    MoveInternal(
                        OffsetFor(sourceId) + move.FromIndex,
                        OffsetFor(sourceId) + move.ToIndex);
                    break;

                case ResetChange<T>:
                    OnReset();
                    break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported change: {change.GetType().Name}");
            }
        }

        /// <summary>
        /// Смещение элементов источника в merged-списке: 0 для первого,
        /// количество элементов первого — для второго.
        /// </summary>
        private int OffsetFor(int sourceId)
            => sourceId == SourceFirst ? 0 : _first.Count;

        private void OnReset()
        {
            ThrowIfDisposed();

            bool hadItems = Items.Count > 0;

            Items.Clear();

            foreach (var item in _first) Items.Add(item);
            foreach (var item in _second) Items.Add(item);

            if (hadItems || Items.Count > 0)
                Raise(new ResetChange<T>());
        }
    }
}