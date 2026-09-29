using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Max</c> без селектора: максимальный элемент источника.
    /// </summary>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать сравнение.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника и находит максимальный — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>null</c>. См. <see cref="MinNode{TNumber}"/>
    /// про обоснование.
    /// </para>
    /// </remarks>
    public sealed class MaxNode<TNumber> : AggregateNode<TNumber, TNumber?>
        where TNumber : struct
    {
        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        public MaxNode(IObservableList<TNumber> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber? Recalculate()
        {
            TNumber? max = null;
            var comparer = Comparer<TNumber>.Default;

            foreach (var item in Source)
            {
                if (max is null || comparer.Compare(item, max.Value) > 0)
                    max = item;
            }

            return max;
        }
    }

    /// <summary>
    /// Агрегат <c>Max</c> с селектором: максимальное значение, выбранное
    /// из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать сравнение.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника, применяет селектор и находит максимальное значение — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>null</c>.
    /// </para>
    /// </remarks>
    public sealed class MaxWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber?>
        where TNumber : struct
    {
        /// <summary>
        /// Селектор значения из элемента источника.
        /// </summary>
        private readonly Func<TSource, TNumber> _selector;

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">Селектор значения. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="selector"/> — <c>null</c>.
        /// </exception>
        public MaxWithSelectorNode(
            IObservableList<TSource> source,
            Func<TSource, TNumber> selector) : base(source)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));

            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber? Recalculate()
        {
            TNumber? max = null;
            var comparer = Comparer<TNumber>.Default;

            foreach (var item in Source)
            {
                var value = _selector(item);

                if (max is null || comparer.Compare(value, max.Value) > 0)
                    max = value;
            }

            return max;
        }
    }
}