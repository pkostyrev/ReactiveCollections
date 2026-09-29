using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Extension-методы агрегации над <see cref="IObservableList{T}"/>.
    /// Возвращают <see cref="IObservableValue{T}"/> — живое значение.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Имена начинаются с <c>Observe</c>, чтобы не конфликтовать с LINQ
    /// (<see cref="System.Linq.Enumerable"/>). C# при разрешении extension-методов
    /// не учитывает специфичность <c>this</c>-параметра, поэтому методы
    /// <c>Count()</c>, <c>Any()</c>, <c>Sum()</c> разрешались бы в LINQ-версии,
    /// возвращающие однократный результат.
    /// </para>
    /// <para>
    /// Соответствие с LINQ:
    /// <c>ObserveCount()</c> ↔ <c>Count()</c>,
    /// <c>ObserveAny(pred)</c> ↔ <c>Any(pred)</c>,
    /// <c>ObserveAll(pred)</c> ↔ <c>All(pred)</c>,
    /// <c>ObserveSum(sel)</c> ↔ <c>Sum(sel)</c>.
    /// </para>
    /// </remarks>
    public static class AggregateExtensions
    {
        // -------------------------------------------------------------------
        // ObserveCount
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: количество элементов в источнике.
        /// </summary>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <returns>Новый <see cref="CountNode{T}"/> как <see cref="IObservableValue{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Значение обновляется при <c>Add</c>, <c>Remove</c>, <c>Reset</c>.
        /// При <c>Update</c> и <c>Replace</c> количество не меняется, событие не райзится.
        /// </remarks>
        public static IObservableValue<int> ObserveCount<T>(this IObservableList<T> source)
        {
            return new CountNode<T>(source);
        }

        // -------------------------------------------------------------------
        // ObserveAny
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: содержит ли источник хотя бы один элемент.
        /// </summary>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <returns>Новый <see cref="AnyNode{T}"/> как <see cref="IObservableValue{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Для пустого источника значение — <c>false</c>. Совпадает с
        /// семантикой <see cref="System.Linq.Enumerable.Any{T}(System.Collections.Generic.IEnumerable{T})"/>.
        /// </remarks>
        public static IObservableValue<bool> ObserveAny<T>(this IObservableList<T> source)
        {
            return new AnyNode<T>(source);
        }

        /// <summary>
        /// Возвращает живое значение: есть ли в источнике хотя бы один элемент,
        /// удовлетворяющий предикату.
        /// </summary>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="predicate">
        /// Предикат. Не может быть <c>null</c>. Вызывается синхронно при
        /// каждом изменении источника.
        /// </param>
        /// <returns>Новый <see cref="AnyWithPredicateNode{T}"/> как <see cref="IObservableValue{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="predicate"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Для пустого источника значение — <c>false</c>.
        /// </remarks>
        public static IObservableValue<bool> ObserveAny<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
        {
            return new AnyWithPredicateNode<T>(source, predicate);
        }

        // -------------------------------------------------------------------
        // ObserveAll
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: все ли элементы источника удовлетворяют предикату.
        /// </summary>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="predicate">
        /// Предикат. Не может быть <c>null</c>. Вызывается синхронно при
        /// каждом изменении источника.
        /// </param>
        /// <returns>Новый <see cref="AllNode{T}"/> как <see cref="IObservableValue{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Если <paramref name="source"/> или <paramref name="predicate"/> — <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Для пустого источника значение — <c>true</c> (вакуумная истина).
        /// </remarks>
        public static IObservableValue<bool> ObserveAll<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
        {
            return new AllNode<T>(source, predicate);
        }

        // -------------------------------------------------------------------
        // ObserveSum (без селектора)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: сумма элементов источника.
        /// </summary>
        /// <remarks>
        /// Для пустого источника значение — <c>0</c>. Совпадает с семантикой
        /// <see cref="System.Linq.Enumerable.Sum(System.Collections.Generic.IEnumerable{int})"/>.
        /// </remarks>
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

        // -------------------------------------------------------------------
        // ObserveSum (с селектором)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: сумма значений, выбранных из элементов источника.
        /// </summary>
        /// <typeparam name="TSource">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="selector">Селектор значения. Не может быть <c>null</c>.</param>
        /// <remarks>
        /// Для пустого источника значение — <c>0</c>.
        /// </remarks>
        public static IObservableValue<int> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, int> selector)
            => new SumWithSelectorNode<TSource, int>(source, selector, zero: 0, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<long> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, long> selector)
            => new SumWithSelectorNode<TSource, long>(source, selector, zero: 0L, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<float> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, float> selector)
            => new SumWithSelectorNode<TSource, float>(source, selector, zero: 0f, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<double> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, double> selector)
            => new SumWithSelectorNode<TSource, double>(source, selector, zero: 0d, add: (a, b) => a + b);

        /// <inheritdoc cref="ObserveSum{TSource}(IObservableList{TSource}, Func{TSource, int})"/>
        public static IObservableValue<decimal> ObserveSum<TSource>(
            this IObservableList<TSource> source,
            Func<TSource, decimal> selector)
            => new SumWithSelectorNode<TSource, decimal>(source, selector, zero: 0m, add: (a, b) => a + b);

        // -------------------------------------------------------------------
        // ObserveMin (без селектора)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: минимальный элемент источника или
        /// <c>null</c>, если источник пуст.
        /// </summary>
        /// <remarks>
        /// Для пустого источника значение — <c>null</c>. Это отличается от LINQ
        /// (<c>Min()</c> бросает <see cref="InvalidOperationException"/>),
        /// но согласовано с моделью агрегатов.
        /// </remarks>
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

        // -------------------------------------------------------------------
        // ObserveMin (с селектором)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: минимальное значение, выбранное из элементов
        /// источника, или <c>null</c>, если источник пуст.
        /// </summary>
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
        // ObserveMax (без селектора)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: максимальный элемент источника или
        /// <c>null</c>, если источник пуст.
        /// </summary>
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

        // -------------------------------------------------------------------
        // ObserveMax (с селектором)
        // -------------------------------------------------------------------

        /// <summary>
        /// Возвращает живое значение: максимальное значение, выбранное из элементов
        /// источника, или <c>null</c>, если источник пуст.
        /// </summary>
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
    }
}