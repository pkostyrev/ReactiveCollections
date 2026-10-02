using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Среднее арифметическое элементов источника.
    /// </summary>
    /// <typeparam name="TNumber">Числовой тип элемента источника.</typeparam>
    /// <typeparam name="TAccumulate">Тип аккумулятора суммы.</typeparam>
    /// <typeparam name="TResult">Тип результата деления.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
    public sealed class AverageNode<TNumber, TAccumulate, TResult>
        : AggregateNode<TNumber, TResult?>
        where TNumber : struct
        where TAccumulate : struct
        where TResult : struct
    {
        private readonly TAccumulate _zero;
        private readonly Func<TAccumulate, TNumber, TAccumulate> _add;
        private readonly Func<TAccumulate, long, TResult> _average;

        /// <param name="zero">Нейтральный элемент суммы.</param>
        /// <param name="add">Операция сложения.</param>
        /// <param name="average">Операция деления суммы на количество.</param>
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
    /// Среднее арифметическое значений, выбранных из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Числовой тип выбранного значения.</typeparam>
    /// <typeparam name="TAccumulate">Тип аккумулятора суммы.</typeparam>
    /// <typeparam name="TResult">Тип результата деления.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
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

        /// <param name="selector">Функция выбора числового значения.</param>
        /// <param name="zero">Нейтральный элемент суммы.</param>
        /// <param name="add">Операция сложения.</param>
        /// <param name="average">Функция вычисления среднего по сумме и количеству элементов.</param>
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