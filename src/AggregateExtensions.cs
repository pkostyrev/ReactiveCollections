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
    /// Соответствие:
    /// <list type="table">
    /// <item><term><c>ObserveCount()</c></term><description>живой <c>Count()</c></description></item>
    /// <item><term><c>ObserveAny(pred)</c></term><description>живой <c>Any(pred)</c></description></item>
    /// <item><term><c>ObserveAll(pred)</c></term><description>живой <c>All(pred)</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class AggregateExtensions
    {
        /// <summary>
        /// Возвращает живое значение: количество элементов в источнике.
        /// </summary>
        /// <typeparam name="T">Тип элемента источника.</typeparam>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <returns>Новый <see cref="CountNode{T}"/> как <see cref="IObservableValue{T}"/>.</returns>
        /// <exception cref="System.ArgumentNullException">
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
    }
}