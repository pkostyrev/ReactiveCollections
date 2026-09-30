using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Average</c> без селектора: среднее арифметическое элементов источника.
    /// </summary>
    /// <typeparam name="TNumber">Числовой тип элемента источника.</typeparam>
    /// <typeparam name="TAccumulate">
    /// Тип аккумулятора суммы. Для <see cref="int"/> и <see cref="long"/> — <see cref="long"/>
    /// (во избежание переполнения), для <see cref="float"/> и <see cref="double"/> —
    /// <see cref="double"/>, для <see cref="decimal"/> — <see cref="decimal"/>.
    /// </typeparam>
    /// <typeparam name="TResult">Тип результата деления.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника, накапливает сумму и количество — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>null</c>. Это отличается от LINQ
    /// (<c>Average()</c> на пустом бросает <see cref="InvalidOperationException"/>),
    /// но согласовано с моделью агрегатов: «нет значения» представлено как <c>null</c>.
    /// </para>
    /// </remarks>
    public sealed class AverageNode<TNumber, TAccumulate, TResult>
        : AggregateNode<TNumber, TResult?>
        where TNumber : struct
        where TAccumulate : struct
        where TResult : struct
    {
        private readonly TAccumulate _zero;
        private readonly Func<TAccumulate, TNumber, TAccumulate> _add;
        private readonly Func<TAccumulate, long, TResult> _average;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="zero">Нейтральный элемент суммы.</param>
        /// <param name="add">Операция сложения. Не может быть <c>null</c>.</param>
        /// <param name="average">Операция деления суммы на количество. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/>, <paramref name="add"/>
        /// или <paramref name="average"/> — <c>null</c>.
        /// </exception>
        public AverageNode(
            IObservableList<TNumber> source,
            TAccumulate zero,
            Func<TAccumulate, TNumber, TAccumulate> add,
            Func<TAccumulate, long, TResult> average) : base(source)
        {
            _zero = zero;
            _add = add ?? throw new ArgumentNullException(nameof(add));
            _average = average ?? throw new ArgumentNullException(nameof(average));

            Initialize();
        }

        /// <inheritdoc />
        protected override TResult? Recalculate()
        {
            var sum = _zero;
            long count = 0;

            foreach (var item in Source)
            {
                sum = _add(sum, item);
                count++;
            }

            if (count == 0)
                return null;

            return _average(sum, count);
        }
    }

    /// <summary>
    /// Агрегат <c>Average</c> с селектором: среднее арифметическое значений,
    /// выбранных из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Числовой тип выбранного значения.</typeparam>
    /// <typeparam name="TAccumulate">Тип аккумулятора суммы.</typeparam>
    /// <typeparam name="TResult">Тип результата деления.</typeparam>
    /// <remarks>
    /// Для пустого источника значение — <c>null</c>.
    /// </remarks>
    public sealed class AverageWithSelectorNode<TSource, TNumber, TAccumulate, TResult>
        : AggregateNode<TSource, TResult?>
        where TNumber : struct
        where TAccumulate : struct
        where TResult : struct
    {
        private readonly Func<TSource, TNumber> _selector;
        private readonly TAccumulate _zero;
        private readonly Func<TAccumulate, TNumber, TAccumulate> _add;
        private readonly Func<TAccumulate, long, TResult> _average;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">Селектор значения. Не может быть <c>null</c>.</param>
        /// <param name="zero">Нейтральный элемент суммы.</param>
        /// <param name="add">Операция сложения. Не может быть <c>null</c>.</param>
        /// <param name="average">Операция деления суммы на количество. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если любой из аргументов — <c>null</c>.
        /// </exception>
        public AverageWithSelectorNode(
            IObservableList<TSource> source,
            Func<TSource, TNumber> selector,
            TAccumulate zero,
            Func<TAccumulate, TNumber, TAccumulate> add,
            Func<TAccumulate, long, TResult> average) : base(source)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));
            _zero = zero;
            _add = add ?? throw new ArgumentNullException(nameof(add));
            _average = average ?? throw new ArgumentNullException(nameof(average));

            Initialize();
        }

        /// <inheritdoc />
        protected override TResult? Recalculate()
        {
            var sum = _zero;
            long count = 0;

            foreach (var item in Source)
            {
                sum = _add(sum, _selector(item));
                count++;
            }

            if (count == 0)
                return null;

            return _average(sum, count);
        }
    }
}