using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// Корневое реактивное одиночное значение.
    /// </summary>
    /// <typeparam name="T">Тип значения.</typeparam>
    /// <remarks>
    /// Аналог <see cref="ObservableList{T}"/> для одного значения. Создаётся
    /// пользователем вручную и мутируется через <see cref="Set"/> или
    /// <see cref="Update"/>.
    /// </remarks>
    public sealed class ObservableValue<T> : IObservableValue<T>
    {
        private T _value;
        private bool _disposed;

        /// <inheritdoc />
        public event Action<ValueChange<T>>? Changed;

        /// <inheritdoc />
        public T Value => _value;

        /// <param name="initialValue">Начальное значение.</param>
        public ObservableValue(T initialValue)
        {
            _value = initialValue;
        }

        /// <summary>
        /// Устанавливает новое значение и публикует <see cref="ValueChange{T}"/>.
        /// </summary>
        /// <param name="newValue">Новое значение.</param>
        /// <remarks>
        /// Событие райзится всегда, даже если <paramref name="newValue"/> равен
        /// текущему по <see cref="EqualityComparer{T}.Default"/>. Значение
        /// обновляется до вызова подписчиков.
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
        /// <remarks>
        /// Для mutable-моделей объект уже изменён извне. В этом случае
        /// <see cref="ValueChange{T}.OldValue"/> и <see cref="ValueChange{T}.NewValue"/>
        /// ссылаются на тот же объект.
        /// </remarks>
        public void Update()
        {
            ThrowIfDisposed();

            Raise(new ValueChange<T>(_value, _value));
        }

        /// <summary>
        /// Помечает узел освобождённым. Идемпотентен.
        /// </summary>
        /// <remarks>
        /// После <c>Dispose</c> <see cref="Set"/> и <see cref="Update"/> бросают
        /// <see cref="ObjectDisposedException"/>. Источников у узла нет —
        /// отписываться не от чего.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <remarks>
        /// Все подписчики вызываются, даже если один из них бросает исключение.
        /// Одиночное исключение пробрасывается как есть, несколько — как
        /// <see cref="AggregateException"/>.
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