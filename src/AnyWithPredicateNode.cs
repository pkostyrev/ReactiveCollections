using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Есть ли в источнике хотя бы один элемент, удовлетворяющий предикату.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>Для пустого источника значение — <c>false</c>.</remarks>
    public sealed class AnyWithPredicateNode<T> : AggregateNode<T, bool>
    {
        private readonly Func<T, bool> _predicate;

        /// <param name="predicate">Предикат, применяемый к элементам источника.</param>
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