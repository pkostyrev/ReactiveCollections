using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Контракт реактивного одиночного значения: чтение + событие изменений.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <remarks>
    /// <para>
    /// Аналог <see cref="IObservableList{T}"/> для одного значения. Используется
    /// для агрегатов (<c>Count</c>, <c>Sum</c> и т.п.) и для пользовательских
    /// значений, которые нужно наблюдать.
    /// </para>
    /// <para>
    /// Не реализует <see cref="System.Collections.Generic.IReadOnlyList{T}"/> —
    /// это не коллекция. Не наследует <c>ObservableNode&lt;T&gt;</c> — там
    /// другая структура хранения.
    /// </para>
    /// <para>
    /// Контракт не гарантирует потокобезопасность — см. README, раздел 4.7.
    /// </para>
    /// </remarks>
    public interface IObservableValue<T> : IDisposable
    {
        /// <summary>
        /// Текущее значение.
        /// </summary>
        /// <remarks>
        /// После <see cref="IDisposable.Dispose"/> чтение разрешено и возвращает
        /// последнее установленное значение.
        /// </remarks>
        T Value { get; }

        /// <summary>
        /// Возникает при каждом изменении значения.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Событие райзится <b>после</b> применения изменения. Подписчик,
        /// читающий <see cref="Value"/> в обработчике, видит уже новое значение.
        /// </para>
        /// <para>
        /// Если один подписчик выбрасывает исключение, остальные всё равно
        /// получают событие. См. README, раздел 4.8.
        /// </para>
        /// </remarks>
        event Action<ValueChange<T>>? Changed;
    }
}