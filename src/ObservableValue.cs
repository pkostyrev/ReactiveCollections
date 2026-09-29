using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Корневое реактивное одиночное значение.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <remarks>
    /// <para>
    /// Аналог <see cref="ObservableList{T}"/> для одного значения. Создаётся
    /// пользователем вручную и мутируется через <see cref="Set"/> или
    /// <see cref="Update"/>.
    /// </para>
    /// <para>
    /// Не является коллекцией и не поддерживает подписки на «части» —
    /// только на изменение значения целиком.
    /// </para>
    /// </remarks>
    public sealed class ObservableValue<T> : IObservableValue<T>
    {
        private T _value;
        private bool _disposed;

        /// <inheritdoc />
        public event Action<ValueChange<T>>? Changed;

        /// <inheritdoc />
        public T Value => _value;

        /// <summary>
        /// Создаёт значение с указанным начальным состоянием.
        /// </summary>
        /// <param name="initialValue">Начальное значение.</param>
        public ObservableValue(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Устанавливает новое значение и публикует <see cref="ValueChange{T}"/>.
        /// </summary>
        /// <param name="newValue">Новое значение.</param>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// <para>
        /// Событие райзится <b>всегда</b>, даже если <paramref name="newValue"/>
        /// равен текущему значению по <see cref="EqualityComparer{T}.Default"/>.
        /// Это явное намерение: «я хочу уведомить подписчиков». Если нужно
        /// уведомлять только при реальном изменении — сравнивай перед вызовом.
        /// </para>
        /// <para>
        /// Значение обновляется <b>до</b> вызова подписчиков: обработчик,
        /// читающий <see cref="Value"/>, видит уже новое значение.
        /// </para>
        /// </remarks>
        public void Set(T newValue)
        {
            ThrowIfDisposed();

            var oldValue = _value;
            _value = newValue;

            Raise(new ValueChange<T>(oldValue, newValue));
        }

        /// <summary>
        /// Публикует <see cref="ValueChange{T}"/>, не меняя значение.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// <para>
        /// Используется для mutable-моделей: пользователь меняет поля объекта
        /// и уведомляет подписчиков, что значение (та же ссылка) могло измениться
        /// внутри.
        /// </para>
        /// <para>
        /// <see cref="ValueChange{T}.OldValue"/> и <see cref="ValueChange{T}.NewValue"/>
        /// в этом случае указывают на один и тот же объект. Для ссылочных типов
        /// подписчик может это заметить через <see cref="object.ReferenceEquals"/>.
        /// </para>
        /// </remarks>
        public void Update()
        {
            ThrowIfDisposed();

            Raise(new ValueChange<T>(_value, _value));
        }

        /// <summary>
        /// Освобождает узел: дальнейшие <see cref="Set"/> и <see cref="Update"/>
        /// будут бросать <see cref="ObjectDisposedException"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Метод идемпотентен: повторный вызов не выполняет никаких действий.
        /// </para>
        /// <para>
        /// <see cref="ObservableValue{T}"/> — источник. У него нет источников,
        /// от которых надо отписываться, поэтому <c>Dispose</c> только помечает
        /// узел освобождённым. Подписчики <see cref="Changed"/> не уведомляются.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        /// <summary>
        /// Бросает <see cref="ObjectDisposedException"/>, если узел освобождён.
        /// </summary>
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Публикует изменение всем подписчикам <see cref="Changed"/>.
        /// </summary>
        /// <remarks>
        /// Если один из подписчиков выбрасывает исключение, остальные всё равно
        /// получат событие. Одиночное исключение пробрасывается как есть,
        /// несколько — как <see cref="AggregateException"/>. См. README, раздел 4.8.
        /// </remarks>
        private void Raise(ValueChange<T> change)
        {
            var handler = Changed;
            if (handler == null)
                return;

            List<Exception>? errors = null;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<ValueChange<T>>)d).Invoke(change);
                }
                catch (Exception ex)
                {
                    (errors ??= new List<Exception>()).Add(ex);
                }
            }

            if (errors == null)
                return;

            if (errors.Count == 1)
                throw errors[0];

            throw new AggregateException(errors);
        }
    }
}