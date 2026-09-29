namespace ReactiveCollections
{
    public enum ChangeType
    {
        Add,
        Remove,
        Replace,
        Update,
        Reset
    }

    public sealed class Change<T>
    {
        public ChangeType Type { get; }

        /// <summary>
        /// Текущее значение.
        /// Для Add/Update/Replace используется как новое значение.
        /// </summary>
        public T Item { get; }

        /// <summary>
        /// Предыдущее значение.
        /// Используется для Replace.
        /// </summary>
        public T? OldItem { get; }

        private Change(ChangeType type, T item, T? oldItem = default)
        {
            Type = type;
            Item = item;
            OldItem = oldItem;
        }

        public static Change<T> Add(T item) => new Change<T>(ChangeType.Add, item);

        public static Change<T> Remove(T item) => new Change<T>(ChangeType.Remove, item);

        public static Change<T> Update(T item, T? oldItem = default) => new Change<T>(ChangeType.Update, item, oldItem);

        public static Change<T> Replace(T oldItem, T newItem) => new Change<T>(ChangeType.Replace, newItem, oldItem);

        public static Change<T> Reset() => new Change<T>(ChangeType.Reset, default!);

        public override string ToString()
        {
            return Type switch
            {
                ChangeType.Replace => $"{Type}: {OldItem} -> {Item}",

                ChangeType.Reset => $"{Type}",

                _ => $"{Type}: {Item}"
            };
        }
    }
}