namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Count</c> — количество элементов в источнике.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> возвращает
    /// <see cref="IObservableList{T}.Count"/> — операция O(1).
    /// Событие <see cref="IObservableValue{T}.Changed"/> райзится только
    /// при <c>Add</c>, <c>Remove</c> и <c>Reset</c> источника. При
    /// <c>Update</c> и <c>Replace</c> значение не меняется — событие не райзится.
    /// </remarks>
    public sealed class CountNode<T> : AggregateNode<T, int>
    {
        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <exception cref="System.ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        public CountNode(IObservableList<T> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override int Recalculate() => Source.Count;
    }
}