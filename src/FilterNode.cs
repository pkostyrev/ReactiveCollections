using System;

namespace ReactiveCollections
{
    public class FilterNode<T> : ProjectionNode<T, T>
    {
        private readonly Func<T, bool> _predicate;

        public FilterNode(IObservableList<T> source, Func<T, bool> predicate) : base(source)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            Initialize();
        }

        protected override void OnAdd(Change<T> change)
        {
            var item = change.Item;

            if (_predicate(item))
                AddInternal(item);
        }

        protected override void OnRemove(Change<T> change)
        {
            RemoveInternal(change.Item);
        }

        protected override void OnUpdate(Change<T> change)
        {
            var item = change.Item;

            int index = Items.IndexOf(item);
            bool exists = index >= 0;
            bool valid = _predicate(item);

            if (valid && !exists)
                AddInternal(item);
            else if (!valid && exists)
                RemoveAtInternal(index);
            else if (valid && exists)
                UpdateAtInternal(index);
        }

        protected override void OnReplace(Change<T> change)
        {
            var oldItem = change.OldItem!;
            var newItem = change.Item;

            int index = Items.IndexOf(oldItem);
            bool newValid = _predicate(newItem);

            if (index >= 0)
            {
                if (newValid)
                    ReplaceAtInternal(index, newItem);
                else
                    RemoveAtInternal(index);
            }
            else
            {
                if (newValid)
                    AddInternal(newItem);
            }
        }
    }
}