namespace ReactiveCollections
{
    public sealed class Group<TKey, T>
    {
        public TKey Key { get; }

        public ObservableList<T> Items { get; }

        public Group(TKey key)
        {
            Key = key;

            Items = new ObservableList<T>();
        }

        public override string ToString()
        {
            return $"Group {Key}: {Items.Count} items";
        }
    }
}
