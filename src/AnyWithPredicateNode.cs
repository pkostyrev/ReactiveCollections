using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Any</c> с предикатом: есть ли в источнике хотя бы один
    /// элемент, удовлетворяющий предикату.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника и останавливается на первом подходящем — O(N)
    /// в худшем случае.
    /// </para>
    /// <para>
    /// Семантика совпадает с <c>System.Linq.Enumerable.Any(predicate)</c>:
    /// для пустой коллекции значение — <c>false</c>.
    /// </para>
    /// </remarks>
    public sealed class AnyWithPredicateNode<T> : AggregateNode<T, bool>
    {
        /// <summary>
        /// Предикат: <c>true</c> — элемент удовлетворяет условию.
        /// </summary>
        private readonly Func<T, bool> _predicate;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="predicate">
        /// Предикат. Не может быть <c>null</c>. Вызывается синхронно при
        /// каждом изменении источника.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="predicate"/> — <c>null</c>.
        /// </exception>
        public AnyWithPredicateNode(IObservableList<T> source, Func<T, bool> predicate)
            : base(source)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

            Initialize();
        }

        /// <inheritdoc />
        protected override bool Recalculate()
        {
            foreach (var item in Source)
            {
                if (_predicate(item))
                    return true;
            }

            return false;
        }
    }
}