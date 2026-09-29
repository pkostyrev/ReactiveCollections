namespace ReactiveCollections
{
    /// <summary>
    /// Группа элементов с общим ключом — результат работы
    /// <see cref="GroupNode{TKey, TSource}"/>.
    /// </summary>
    /// <typeparam name="TKey">Тип ключа группы.</typeparam>
    /// <typeparam name="T">Тип элемента группы.</typeparam>
    /// <remarks>
    /// <para>
    /// Группа — самостоятельный реактивный источник. Её <see cref="Items"/>
    /// можно использовать как источник для следующих проекций или подписаться
    /// на его <c>Changed</c> напрямую, не дожидаясь событий от
    /// <see cref="GroupNode{TKey, TSource}"/>.
    /// </para>
    /// <para>
    /// Экземпляры создаются только <see cref="GroupNode{TKey, TSource}"/>.
    /// Пользовательский код не должен создавать группы вручную — иначе они
    /// не будут связаны с источником группировки.
    /// </para>
    /// </remarks>
    public sealed class Group<TKey, T>
    {
        /// <summary>
        /// Ключ группы. Значение фиксируется при создании и не меняется.
        /// </summary>
        /// <remarks>
        /// <see cref="KeyedProjectionNode{TKey, TSource, TResult}.GetKey"/>
        /// полагается на стабильность ключа: он читается как до, так и после
        /// операций над содержимым группы.
        /// </remarks>
        public TKey Key { get; }

        /// <summary>
        /// Содержимое группы — живой реактивный список.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Публичный <see cref="ObservableList{T}"/> даёт возможность подписки
        /// на изменения внутри группы отдельно от событий узла группировки.
        /// Это осознанный отказ от каскадной маршрутизации вложенных событий
        /// в пользу «каждый узел — сам себе источник» — см. README, раздел 6.
        /// </para>
        /// <para>
        /// <b>Важно:</b> мутации <see cref="Items"/> в обход
        /// <see cref="GroupNode{TKey, TSource}"/> нарушают согласованность
        /// между группой и внутренними индексами узла. Такие мутации не
        /// отслеживаются и не проверяются — ответственность на вызывающем.
        /// </para>
        /// </remarks>
        public ObservableList<T> Items { get; }

        /// <summary>
        /// Создаёт пустую группу с указанным ключом.
        /// </summary>
        /// <param name="key">Ключ группы.</param>
        /// <remarks>
        /// Конструктор вызывается <see cref="GroupNode{TKey, TSource}.CreateResult"/>.
        /// Пользовательский код обычно не создаёт группы напрямую.
        /// </remarks>
        public Group(TKey key)
        {
            Key = key;

            Items = new ObservableList<T>();
        }

        /// <summary>
        /// Возвращает строковое представление группы для отладки и логов.
        /// </summary>
        /// <returns>Строка вида <c>Group {key}: N items</c>.</returns>
        public override string ToString()
        {
            return $"Group {Key}: {Items.Count} items";
        }
    }
}