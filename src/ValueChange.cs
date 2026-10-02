namespace ReactiveCollections
{
    /// <summary>
    /// Описание одного изменения значения в <see cref="IObservableValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <remarks>
    /// В отличие от <see cref="Change{T}"/>, описывающего изменения списка,
    /// <see cref="ValueChange{T}"/> описывает переход от старого значения
    /// к новому. Старое и новое значения могут совпадать, если изменение
    /// используется для уведомления об обновлении.
    /// </remarks>
    public readonly struct ValueChange<T>
    {
        public T OldValue { get; }
        public T NewValue { get; }

        public ValueChange(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }

        public override string ToString()
            => $"{OldValue} -> {NewValue}";
    }
}