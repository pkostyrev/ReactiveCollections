namespace ReactiveCollections
{
    /// <summary>
    /// Описание одного изменения значения в <see cref="IObservableValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <remarks>
    /// <para>
    /// Иммутабельная структура — аллокаций не создаёт. Используется как
    /// аргумент события <see cref="IObservableValue{T}.Changed"/>.
    /// </para>
    /// <para>
    /// В отличие от <see cref="Change{T}"/>, описывающего изменения
    /// <b>списка</b>, <see cref="ValueChange{T}"/> описывает замену одного
    /// значения другим. Промежуточных состояний (<c>Add</c>/<c>Remove</c>)
    /// у одиночного значения нет.
    /// </para>
    /// </remarks>
    public readonly struct ValueChange<T>
    {
        /// <summary>
        /// Предыдущее значение.
        /// </summary>
        public T OldValue { get; }

        /// <summary>
        /// Новое значение.
        /// </summary>
        public T NewValue { get; }

        /// <summary>
        /// Создаёт описание изменения значения.
        /// </summary>
        /// <param name="oldValue">Предыдущее значение.</param>
        /// <param name="newValue">Новое значение.</param>
        public ValueChange(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        /// <summary>
        /// Возвращает строковое представление изменения для отладки и логов.
        /// </summary>
        public override string ToString()
            => $"{OldValue} -> {NewValue}";
    }
}