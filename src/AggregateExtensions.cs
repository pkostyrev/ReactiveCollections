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
    /// <c>ObserveAll(pred)</c> ↔ <c>All(pred)</c>.
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
        /// Совпадает с семантикой
        /// <see cref="System.Linq.Enumerable.All{T}(System.Collections.Generic.IEnumerable{T}, System.Func{T, bool})"/>.
        /// </remarks>
        public static IObservableValue<bool> ObserveAll<T>(
            this IObservableList<T> source,
            Func<T, bool> predicate)
        {
            return new AllNode<T>(source, predicate);
        }
    }
}