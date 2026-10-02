using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Минимальный элемент источника.
    /// </summary>
    /// <typeparam name="TNumber">Тип значения, поддерживающий сравнение.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
    public sealed class MinNode<TNumber> : AggregateNode<TNumber, TNumber?>
        where TNumber : struct
    {
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
    /// Минимальное значение, выбранное из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Тип значения, поддерживающий сравнение.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
    public sealed class MinWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber?>
        where TNumber : struct
    {
        private readonly Func<TSource, TNumber> _selector;

        /// <param name="selector">Функция выбора числового значения.</param>
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