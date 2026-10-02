using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Единый контракт для всех реактивных коллекций: чтение, событие изменений
    /// и освобождение подписок.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// Реализуется источниками (<see cref="ObservableList{T}"/>) и проекциями
    /// (<see cref="FilterNode{T}"/>, <see cref="SelectNode{TSource, TResult}"/>
    /// и т.д.). Потокобезопасность не гарантируется.
    /// </remarks>
    public interface IObservableList<T> : IReadOnlyList<T>, IDisposable
    {
        /// <summary>
        /// Возникает при изменении состояния коллекции.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Событие райзится после применения изменения к состоянию коллекции:
        /// подписчик видит уже новое состояние.
        /// </para>
        /// <para>
        /// Если один подписчик выбрасывает исключение, остальные всё равно
        /// получают событие. Исключения пробрасываются вызывающему после
        /// обхода всех подписчиков.
        /// </para>
        /// </remarks>
        event Action<Change<T>>? Changed;
    }
}