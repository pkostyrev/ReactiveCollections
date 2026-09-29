using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Any</c> без предиката: содержит ли источник хотя бы один элемент.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> возвращает
    /// <c>Source.Count &gt; 0</c> — операция O(1).
    /// </para>
    /// <para>
    /// Семантика совпадает с <c>System.Linq.Enumerable.Any()</c>:
    /// для пустой коллекции значение — <c>false</c>.
    /// </para>
    /// </remarks>
    public sealed class AnyNode<T> : AggregateNode<T, bool>
    {
        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        public AnyNode(IObservableList<T> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override bool Recalculate() => Source.Count > 0;
    }
}