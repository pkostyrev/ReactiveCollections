using System;

namespace ReactiveCollections
{
    public abstract class ProjectionNode<TSource, TResult> : ObservableNode<TResult>
    {
        protected readonly IObservableList<TSource> Source;

        private bool _isInitialized;

        protected ProjectionNode(IObservableList<TSource> source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        protected void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException(
                    "Projection node is already initialized.");

            _isInitialized = true;

            foreach (var item in Source)
            {
                OnAdd(Change<TSource>.Add(item));
            }

            Source.Changed += OnSourceChanged;
        }

        private void OnSourceChanged(Change<TSource> change)
        {
            switch (change.Type)
            {
                case ChangeType.Add:
                    OnAdd(change);
                    break;

                case ChangeType.Remove:
                    OnRemove(change);
                    break;

                case ChangeType.Replace:
                    OnReplace(change);
                    break;

                case ChangeType.Update:
                    OnUpdate(change);
                    break;

                case ChangeType.Reset:
                    OnReset(change);
                    break;
            }
        }

        protected virtual void OnAdd(Change<TSource> change)
        {
        }

        protected virtual void OnRemove(Change<TSource> change)
        {
        }

        protected virtual void OnUpdate(Change<TSource> change)
        {
        }

        protected virtual void OnReplace(Change<TSource> change)
        {
            // стандартное поведение:
            // удалить старое
            // добавить новое

            OnRemove(Change<TSource>.Remove(change.OldItem!));

            OnAdd(Change<TSource>.Add(change.Item));
        }

        protected virtual void OnReset(Change<TSource> change)
        {
            ResetInternal();
        }
    }
}
