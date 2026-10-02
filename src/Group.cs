namespace ReactiveCollections
{
    /// <summary>
    /// Группа элементов с общим ключом — результат работы
    /// <see cref="GroupNode{TKey, TSource}"/>.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа группы.</typeparam>
    /// <typeparam name="T">Тип элемента группы.</typeparam>
    /// <remarks>
    /// Группа — самостоятельный реактивный источник. Её <see cref="Items"/>
    /// можно использовать как источник для следующих проекций или подписаться
    /// на его <c>Changed</c> напрямую, не дожидаясь событий от
    /// <see cref="GroupNode{TKey, TSource}"/>.
    /// </remarks>
    public sealed class Group<TKey, T>
    {
        /// <summary>
        /// Ключ группы. Фиксируется при создании и не меняется.
        /// </summary>
        public TKey Key { get; }

        /// <summary>
        /// Содержимое группы — живой реактивный список.
        /// </summary>
        /// <remarks>
        /// Мутации этого списка в обход
        /// <see cref="GroupNode{TKey, TSource}"/> не поддерживаются:
        /// они нарушают согласованность группы с внутренними индексами узла.
        /// </remarks>
        public ObservableList<T> Items { get; }

        /// <param name="key">Ключ группы.</param>
        public Group(TKey key)
        {
            Key = key;
            Items = new ObservableList<T>();
        }

        public override string ToString()
            => $"Group {Key}: {Items.Count} items";
    }
}