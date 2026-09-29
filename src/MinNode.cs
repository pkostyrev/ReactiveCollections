using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Агрегат <c>Min</c> без селектора: минимальный элемент источника.
    /// </summary>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать сравнение.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника и находит минимальный — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>null</c>. Это отличается от
    /// LINQ (<c>Min()</c> на пустом бросает <see cref="InvalidOperationException"/>),
    /// но согласовано с моделью <see cref="AggregateNode{TSource, TResult}"/>:
    /// значение всегда доступно, а «нет значения» представлено как <c>null</c>.
    /// </para>
    /// </remarks>
    public sealed class MinNode<TNumber> : AggregateNode<TNumber, TNumber?>
        where TNumber : struct
    {
        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        public MinNode(IObservableList<TNumber> source) : base(source)
        {
            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber? Recalculate()
        {
            TNumber? min = null;
            var comparer = Comparer<TNumber>.Default;

            foreach (var item in Source)
            {
                if (min is null || comparer.Compare(item, min.Value) < 0)
                    min = item;
            }

            return min;
        }
    }

    /// <summary>
    /// Агрегат <c>Min</c> с селектором: минимальное значение, выбранное
    /// из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Числовой тип. Должен поддерживать сравнение.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="AggregateNode{TSource, TResult}.Recalculate"/> перебирает
    /// элементы источника, применяет селектор и находит минимальное значение — O(N).
    /// </para>
    /// <para>
    /// Для пустого источника значение — <c>null</c>.
    /// </para>
    /// </remarks>
    public sealed class MinWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber?>
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
        public MinWithSelectorNode(
            IObservableList<TSource> source,
            Func<TSource, TNumber> selector) : base(source)
        {
            _selector = selector ?? throw new ArgumentNullException(nameof(selector));

            Initialize();
        }

        /// <inheritdoc />
        protected override TNumber? Recalculate()
        {
            TNumber? min = null;
            var comparer = Comparer<TNumber>.Default;

            foreach (var item in Source)
            {
                var value = _selector(item);

                if (min is null || comparer.Compare(value, min.Value) < 0)
                    min = value;
            }

            return min;
        }
    }
}