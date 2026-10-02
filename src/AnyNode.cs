namespace ReactiveCollections
{
    /// <summary>
    /// Содержит ли источник хотя бы один элемент.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>Для пустого источника значение — <c>false</c>.</remarks>
    public sealed class AnyNode<T> : AggregateNode<T, bool>
    {
        public AnyNode(IObservableList<T> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override bool Recalculate() => Source.Count > 0;
    }
}