namespace ReactiveCollections
{
    /// <summary>
    /// Количество элементов в источнике.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>
    /// Событие райзится только если итоговое количество элементов изменилось.
    /// <c>Update</c> и <c>Replace</c> количество не меняют.
    /// </remarks>
    public sealed class CountNode<T> : AggregateNode<T, int>
    {
        public CountNode(IObservableList<T> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override int Recalculate() => Source.Count;
    }
}