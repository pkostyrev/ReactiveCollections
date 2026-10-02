using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Все ли элементы источника удовлетворяют предикату.
    /// </summary>
    /// <typeparam name="T">Тип элемента источника.</typeparam>
    /// <remarks>
    /// Для пустого источника значение — <c>true</c> (вакуумная истина).
    /// </remarks>
    public sealed class AllNode<T> : AggregateNode<T, bool>
    {
        private readonly Func<T, bool> _predicate;

        /// <param name="predicate">Предикат, применяемый к элементам источника.</param>
        public AllNode(IObservableList<T> source, Func<T, bool> predicate)
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
                if (!_predicate(item))
                    return false;
            }

            return true;
        }
    }
}