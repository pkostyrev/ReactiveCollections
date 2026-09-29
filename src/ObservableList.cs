namespace ReactiveCollections
{
    public class ObservableList<T> : ObservableNode<T>
    {
        public bool Add(T item)
        {
            return AddInternal(item);
        }

        public bool Remove(T item)
        {
            return RemoveInternal(item);
        }

        public bool Replace(T oldItem, T newItem)
        {
            return ReplaceInternal(oldItem, newItem);
        }

        public bool Update(T item)
        {
            return UpdateInternal(item);
        }

        public bool Reset()
        {
            return ResetInternal();
        }
    }
}