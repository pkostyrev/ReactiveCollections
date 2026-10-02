using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Extension-методы агрегации над <see cref="IObservableList{T}"/>.
    /// </summary>
    /// <remarks>
    /// Имена начинаются с <c>Observe</c>, чтобы отличать живые агрегаты
    /// от одноразовых LINQ-агрегаций (<c>Count</c>, <c>Any</c>, <c>Sum</c>).
    /// Агрегаты не транслируют <c>Change&lt;T&gt;</c> наружу — они публикуют
    /// изменения итогового значения через <see cref="ValueChange{T}"/>.
    /// </remarks>
    public static class AggregateExtensions
    {
        // -------------------------------------------------------------------
        // ObserveCount
        // -------------------------------------------------------------------

        /// <summary>Количество элементов источника.</summary>
        public static IObservableValue<int> ObserveCount<T>(this IObservableList<T> source)
            => new CountNode<T>(source);

        // -------------------------------------------------------------------
        // ObserveAny
        // -------------------------------------------------------------------

        /// <summary>Содержит ли источник хотя бы один элемент.</summary>
        /// <remarks>Для пустого источника — <c>false</c>.</remarks>
        public static IObservableValue<bool> ObserveAny<T>(this IObservableList<T> source)
            => new AnyNode<T>(source);

        /// <summary>Есть ли элемент, удовлетворяющий предикату.</summary>
        /// <param name="predicate">Предикат, применяемый к элементам источника.</param>
        /// <remarks>Для пустого источника — <c>false</c>.</remarks>
        public static IObservableValue<bool> ObserveAny<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
            => new AnyWithPredicateNode<T>(source, predicate);

        // -------------------------------------------------------------------
        // ObserveAll
        // -------------------------------------------------------------------

        /// <summary>Все ли элементы источника удовлетворяют предикату.</summary>
        /// <param name="predicate">Предикат, применяемый к элементам источника.</param>
        /// <remarks>Для пустого источника — <c>true</c> (вакуумная истина).</remarks>
        public static IObservableValue<bool> ObserveAll<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
            => new AllNode<T>(source, predicate);

        // -------------------------------------------------------------------
        // ObserveSum
        // -------------------------------------------------------------------

        /// <summary>Сумма элементов источника.</summary>
        /// <remarks>Для пустого источника — <c>0</c>.</remarks>
        public static IObservableValue<int> ObserveSum(this IObservableList<int> source)
            => new SumNode<int>(source, zero: 0, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum(IObservableList{int})"/>
        public static IObservableValue<long> ObserveSum(this IObservableList<long> source)
            => new SumNode<long>(source, zero: 0L, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum(IObservableList{int})"/>
        public static IObservableValue<float> ObserveSum(this IObservableList<float> source)
            => new SumNode<float>(source, zero: 0f, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum(IObservableList{int})"/>
        public static IObservableValue<double> ObserveSum(this IObservableList<double> source)
            => new SumNode<double>(source, zero: 0d, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum(IObservableList{int})"/>
        public static IObservableValue<decimal> ObserveSum(this IObservableList<decimal> source)
            => new SumNode<decimal>(source, zero: 0m, add: (a, b) => a + b);

        /// <summary>Сумма значений, выбранных из элементов источника.</summary>
        /// <param name="selector">Функция выбора числового значения.</param>
        /// <remarks>Для пустого источника — <c>0</c>.</remarks>
        public static IObservableValue<int> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, int> selector)
            => new SumWithSelectorNode<TSource, int>(
                source, selector, zero: 0, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<long> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, long> selector)
            => new SumWithSelectorNode<TSource, long>(
                source, selector, zero: 0L, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<float> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, float> selector)
            => new SumWithSelectorNode<TSource, float>(
                source, selector, zero: 0f, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, double> selector)
            => new SumWithSelectorNode<TSource, double>(
                source, selector, zero: 0d, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<decimal> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, decimal> selector)
            => new SumWithSelectorNode<TSource, decimal>(
                source, selector, zero: 0m, add: (a, b) => a + b);

        // -------------------------------------------------------------------
        // ObserveMin
        // -------------------------------------------------------------------

        /// <summary>Минимальный элемент источника или <c>null</c>.</summary>
        public static IObservableValue<int?> ObserveMin(this IObservableList<int> source)
            => new MinNode<int>(source);

        /// <inheritdoc cref="ObserveMin(IObservableList{int})"/>
        public static IObservableValue<long?> ObserveMin(this IObservableList<long> source)
            => new MinNode<long>(source);

        /// <inheritdoc cref="ObserveMin(IObservableList{int})"/>
        public static IObservableValue<float?> ObserveMin(this IObservableList<float> source)
            => new MinNode<float>(source);

        /// <inheritdoc cref="ObserveMin(IObservableList{int})"/>
        public static IObservableValue<double?> ObserveMin(this IObservableList<double> source)
            => new MinNode<double>(source);

        /// <inheritdoc cref="ObserveMin(IObservableList{int})"/>
        public static IObservableValue<decimal?> ObserveMin(this IObservableList<decimal> source)
            => new MinNode<decimal>(source);

        /// <summary>Минимальное значение, выбранное из элементов источника, или <c>null</c>.</summary>
        /// <param name="selector">Функция выбора числового значения.</param>
        public static IObservableValue<int?> ObserveMin<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, int> selector)
            => new MinWithSelectorNode<TSource, int>(source, selector);

        /// <inheritdoc cref="ObserveMin{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<long?> ObserveMin<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, long> selector)
            => new MinWithSelectorNode<TSource, long>(source, selector);

        /// <inheritdoc cref="ObserveMin{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<float?> ObserveMin<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, float> selector)
            => new MinWithSelectorNode<TSource, float>(source, selector);

        /// <inheritdoc cref="ObserveMin{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double?> ObserveMin<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, double> selector)
            => new MinWithSelectorNode<TSource, double>(source, selector);

        /// <inheritdoc cref="ObserveMin{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<decimal?> ObserveMin<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, decimal> selector)
            => new MinWithSelectorNode<TSource, decimal>(source, selector);

        // -------------------------------------------------------------------
        // ObserveMax
        // -------------------------------------------------------------------

        /// <summary>Максимальный элемент источника или <c>null</c>.</summary>
        public static IObservableValue<int?> ObserveMax(this IObservableList<int> source)
            => new MaxNode<int>(source);

        /// <inheritdoc cref="ObserveMax(IObservableList{int})"/>
        public static IObservableValue<long?> ObserveMax(this IObservableList<long> source)
            => new MaxNode<long>(source);

        /// <inheritdoc cref="ObserveMax(IObservableList{int})"/>
        public static IObservableValue<float?> ObserveMax(this IObservableList<float> source)
            => new MaxNode<float>(source);

        /// <inheritdoc cref="ObserveMax(IObservableList{int})"/>
        public static IObservableValue<double?> ObserveMax(this IObservableList<double> source)
            => new MaxNode<double>(source);

        /// <inheritdoc cref="ObserveMax(IObservableList{int})"/>
        public static IObservableValue<decimal?> ObserveMax(this IObservableList<decimal> source)
            => new MaxNode<decimal>(source);

        /// <summary>Максимальное значение, выбранное из элементов источника, или <c>null</c>.</summary>
        /// <param name="selector">Функция выбора числового значения.</param>
        public static IObservableValue<int?> ObserveMax<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, int> selector)
            => new MaxWithSelectorNode<TSource, int>(source, selector);

        /// <inheritdoc cref="ObserveMax{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<long?> ObserveMax<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, long> selector)
            => new MaxWithSelectorNode<TSource, long>(source, selector);

        /// <inheritdoc cref="ObserveMax{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<float?> ObserveMax<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, float> selector)
            => new MaxWithSelectorNode<TSource, float>(source, selector);

        /// <inheritdoc cref="ObserveMax{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double?> ObserveMax<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, double> selector)
            => new MaxWithSelectorNode<TSource, double>(source, selector);

        /// <inheritdoc cref="ObserveMax{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<decimal?> ObserveMax<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, decimal> selector)
            => new MaxWithSelectorNode<TSource, decimal>(source, selector);

        // -------------------------------------------------------------------
        // ObserveAverage
        // -------------------------------------------------------------------

        /// <summary>Среднее арифметическое элементов источника или <c>null</c>.</summary>
        /// <remarks>
        /// Тип аккумулятора выбирается отдельно от типа результата:
        /// <c>long</c> для <see cref="int"/> и <see cref="long"/>,
        /// <c>double</c> для <see cref="float"/> и <see cref="double"/>,
        /// <c>decimal</c> для <see cref="decimal"/>.
        /// </remarks>
        public static IObservableValue<double?> ObserveAverage(this IObservableList<int> source)
            => new AverageNode<int, long, double>(
                source, zero: 0L, add: (s, x) => s + x, average: (s, c) => (double)s / c);

        /// <inheritdoc cref="ObserveAverage(IObservableList{int})"/>
        public static IObservableValue<double?> ObserveAverage(this IObservableList<long> source)
            => new AverageNode<long, long, double>(
                source, zero: 0L, add: (s, x) => s + x, average: (s, c) => (double)s / c);

        /// <inheritdoc cref="ObserveAverage(IObservableList{int})"/>
        public static IObservableValue<float?> ObserveAverage(this IObservableList<float> source)
            => new AverageNode<float, double, float>(
                source, zero: 0d, add: (s, x) => s + x, average: (s, c) => (float)(s / c));

        /// <inheritdoc cref="ObserveAverage(IObservableList{int})"/>
        public static IObservableValue<double?> ObserveAverage(this IObservableList<double> source)
            => new AverageNode<double, double, double>(
                source, zero: 0d, add: (s, x) => s + x, average: (s, c) => s / c);

        /// <inheritdoc cref="ObserveAverage(IObservableList{int})"/>
        public static IObservableValue<decimal?> ObserveAverage(this IObservableList<decimal> source)
            => new AverageNode<decimal, decimal, decimal>(
                source, zero: 0m, add: (s, x) => s + x, average: (s, c) => s / c);

        /// <summary>Среднее значений, выбранных из элементов источника, или <c>null</c>.</summary>
        /// <param name="selector">Функция выбора числового значения.</param>
        public static IObservableValue<double?> ObserveAverage<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, int> selector)
            => new AverageWithSelectorNode<TSource, int, long, double>(
                source, selector, zero: 0L, add: (s, x) => s + x, average: (s, c) => (double)s / c);

        /// <inheritdoc cref="ObserveAverage{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double?> ObserveAverage<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, long> selector)
            => new AverageWithSelectorNode<TSource, long, long, double>(
                source, selector, zero: 0L, add: (s, x) => s + x, average: (s, c) => (double)s / c);

        /// <inheritdoc cref="ObserveAverage{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<float?> ObserveAverage<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, float> selector)
            => new AverageWithSelectorNode<TSource, float, double, float>(
                source, selector, zero: 0d, add: (s, x) => s + x, average: (s, c) => (float)(s / c));

        /// <inheritdoc cref="ObserveAverage{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double?> ObserveAverage<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, double> selector)
            => new AverageWithSelectorNode<TSource, double, double, double>(
                source, selector, zero: 0d, add: (s, x) => s + x, average: (s, c) => s / c);

        /// <inheritdoc cref="ObserveAverage{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<decimal?> ObserveAverage<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, decimal> selector)
            => new AverageWithSelectorNode<TSource, decimal, decimal, decimal>(
                source, selector, zero: 0m, add: (s, x) => s + x, average: (s, c) => s / c);
    }
}