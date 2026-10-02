using System;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// База для агрегатов: узлов, которые сворачивают список в одиночное
    /// значение, пересчитывая его при изменении источника.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип агрегированного значения.</typeparam>
    /// <remarks>
    /// <para>
    /// Агрегированное значение пересчитывается при каждом изменении источника.
    /// Событие <see cref="Changed"/> райзится только если агрегированное
    /// значение реально изменилось (сравнение через
    /// <see cref="EqualityComparer{T}.Default"/>). Это отличается от
    /// <see cref="ObservableValue{T}.Set"/>, который райзит всегда.
    /// </para>
    /// <para>
    /// Пересчёт синхронный. Для <c>Count</c> — O(1), для
    /// <c>Sum</c> / <c>Min</c> / <c>Max</c> / <c>Average</c> — O(N).
    /// Оптимизация — в дорожной карте.
    /// </para>
    /// </remarks>
    public abstract class AggregateNode<TSource, TResult> : IObservableValue<TResult>
    {
        private readonly IEqualityComparer<TResult> _comparer;

        private TResult _value = default!;
        private bool _initialized;
        private bool _disposed;

        /// <inheritdoc />
        public event Action<ValueChange<TResult>>? Changed;

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">
        /// Если узел ещё не инициализирован (наследник забыл вызвать
        /// <see cref="Initialize"/>).
        /// </exception>
        public TResult Value
        {
            get
            {
                if (!_initialized)
                    throw new InvalidOperationException(
                        "Aggregate node was not initialized. " +
                        "Call Initialize() from the constructor of the derived class.");

                return _value;
            }
        }

        /// <summary>Источник агрегации.</summary>
        protected IObservableList<TSource> Source { get; }

        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="comparer">
        /// Компаратор для сравнения значений. Если <c>null</c> —
        /// используется <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <remarks>
        /// Конструктор не подписывается на источник и не вычисляет начальное
        /// значение — это делает <see cref="Initialize"/>. Наследник обязан
        /// вызвать его в конце своего конструктора.
        /// </remarks>
        protected AggregateNode(
            IObservableList<TSource> source,
            IEqualityComparer<TResult>? comparer = null)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            _comparer = comparer ?? EqualityComparer<TResult>.Default;
        }

        /// <summary>
        /// Вычисляет текущее агрегированное значение по состоянию источника.
        /// </summary>
        /// <remarks>
        /// Вызывается один раз при <see cref="Initialize"/> и при каждом
        /// изменении источника. Не должен иметь побочных эффектов.
        /// </remarks>
        protected abstract TResult Recalculate();

        /// <summary>
        /// Вычисляет начальное значение и подписывается на изменения источника.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если метод вызван повторно.
        /// </exception>
        /// <remarks>
        /// Вызывается ровно один раз из конструктора наследника, после
        /// инициализации его полей (например, селекторов).
        /// </remarks>
        protected void Initialize()
        {
            if (_initialized)
                throw new InvalidOperationException(
                    "Aggregate node is already initialized.");

            _initialized = true;

            _value = Recalculate();
            Source.Changed += OnSourceChanged;
        }

        /// <summary>
        /// Отписывается от источника. Идемпотентен.
        /// </summary>
        /// <remarks>Источник не удаляется — это ответственность вызывающего.</remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_initialized)
                Source.Changed -= OnSourceChanged;
        }

        private void OnSourceChanged(Change<TSource> change)
        {
            var oldValue = _value;
            var newValue = Recalculate();

            if (_comparer.Equals(oldValue, newValue))
                return;

            _value = newValue;

            Raise(new ValueChange<TResult>(oldValue, newValue));
        }

        /// <remarks>
        /// Если один из подписчиков выбрасывает исключение, остальные всё
        /// равно получат событие.
        /// </remarks>
        private void Raise(ValueChange<TResult> change)
        {
            var handler = Changed;
            if (handler == null)
                return;

            List<Exception>? errors = null;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<ValueChange<TResult>>)d).Invoke(change);
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