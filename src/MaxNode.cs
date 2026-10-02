using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Максимальный элемент источника.
    /// </summary>
    /// <typeparam name="TNumber">Тип значения, поддерживающий сравнение.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
    public sealed class MaxNode<TNumber> : AggregateNode<TNumber, TNumber?>
        where TNumber : struct
    {
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
    /// Максимальное значение, выбранное из элементов источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TNumber">Тип значения, поддерживающий сравнение.</typeparam>
    /// <remarks>Для пустого источника значение — <c>null</c>.</remarks>
    public sealed class MaxWithSelectorNode<TSource, TNumber> : AggregateNode<TSource, TNumber?>
        where TNumber : struct
    {
        private readonly Func<TSource, TNumber> _selector;

        /// <param name="selector">Функция выбора числового значения.</param>
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