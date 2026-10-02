using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Суммирует элементы источника с помощью заданной операции сложения.
    /// </summary>
    /// <typeparam name="TNumber">Тип элемента источника.</typeparam>
    /// <remarks>
    /// Для пустого источника результатом является значение <c>zero</c>,
    /// переданное в конструктор.
    /// </remarks>
    public sealed class SumNode<TNumber> : AggregateNode<TNumber, TNumber>
    {
        private readonly Func<TNumber, TNumber, TNumber> _add;
        private readonly TNumber _zero;

        /// <param name="zero">
        /// Начальное значение аккумулятора и результат для пустого источника.
        /// </param>
        /// <param name="add">Операция сложения.</param>
        public SumNode(
            IObservableList<TNumber> source,
            TNumber zero,
            Func<TNumber, TNumber, TNumber> add) : base(source)
        {
            _zero = zero;
            _add = add ?? throw new ArgumentNullException(nameof(add));

            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber Recalculate()
        {
            var sum = _zero;

            foreach (var item in Source)
                sum = _add(sum, item);

            return sum;
        }
    }

    /// <summary>
    /// Сумма значений, выбранных из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Тип выбранного значения.</typeparam>
    /// <remarks>
    /// Для пустого источника значение — <c>zero</c>, переданный в конструктор.
    /// </remarks>
    public sealed class SumWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber>
    {
        private readonly Func<TSource, TNumber> _selector;
        private readonly Func<TNumber, TNumber, TNumber> _add;
        private readonly TNumber _zero;

        /// <param name="selector">Функция выбора значения.</param>
        /// <param name="zero">
        /// Начальное значение аккумулятора и результат для пустого источника.
        /// </param>
        /// <param name="add">Операция сложения.</param>
        public SumWithSelectorNode(
            IObservableList<TSource> source,
            Func<TSource, TNumber> selector,
            TNumber zero,
            Func<TNumber, TNumber, TNumber> add) : base(source)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _zero = zero;
            _add = add ?? throw new ArgumentNullException(nameof(add));

            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber Recalculate()
        {
            var sum = _zero;

            foreach (var item in Source)
                sum = _add(sum, _selector(item));

            return sum;
        }
    }
}