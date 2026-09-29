using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Sum</c> без селектора: сумма элементов источника.
    /// </summary>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать операцию сложения.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника и накапливает сумму — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>zero</c>, переданный в конструктор
    /// (обычно <c>0</c> соответствующего типа). Совпадает с семантикой
    /// <c>System.Linq.Enumerable.Sum()</c>.
    /// </para>
    /// </remarks>
    public sealed class SumNode<TNumber> : AggregateNode<TNumber, TNumber>
    {
        /// <summary>
        /// Операция сложения двух чисел.
        /// </summary>
        private readonly Func<TNumber, TNumber, TNumber> _add;

        /// <summary>
        /// Нейтральный элемент для операции сложения.
        /// </summary>
        private readonly TNumber _zero;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="zero">Нейтральный элемент — результат на пустом источнике.</param>
        /// <param name="add">Операция сложения. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="add"/> — <c>null</c>.
        /// </exception>
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
    /// Агрегат <c>Sum</c> с селектором: сумма значений, выбранных из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать операцию сложения.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника, применяет селектор и накапливает сумму — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>zero</c>, переданный в конструктор.
    /// </para>
    /// </remarks>
    public sealed class SumWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber>
    {
        /// <summary>
        /// Селектор значения из элемента источника.
        /// </summary>
        private readonly Func<TSource, TNumber> _selector;

        /// <summary>
        /// Операция сложения двух чисел.
        /// </summary>
        private readonly Func<TNumber, TNumber, TNumber> _add;

        /// <summary>
        /// Нейтральный элемент для операции сложения.
        /// </summary>
        private readonly TNumber _zero;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">Селектор значения. Не может быть <c>null</c>.</param>
        /// <param name="zero">Нейтральный элемент — результат на пустом источнике.</param>
        /// <param name="add">Операция сложения. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/>, <paramref name="selector"/>
        /// или <paramref name="add"/> — <c>null</c>.
        /// </exception>
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