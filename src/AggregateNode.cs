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
    /// В отличие от <see cref="ObservableValue{T}"/> (ручное значение),
    /// <see cref="AggregateNode{TSource, TResult}"/> — <b>производное</b>
    /// значение. Оно пересчитывается при каждом изменении источника.
    /// </para>
    /// <para>
    /// <b>Событие <see cref="Changed"/> райзится только тогда, когда
    /// агрегированное значение реально изменилось</b> (сравнение через
    /// <see cref="EqualityComparer{T}.Default"/>). Например, <c>Count</c>
    /// не отреагирует на <c>Update</c> или <c>Replace</c> в источнике,
    /// потому что количество элементов не изменилось.
    /// Это отличается от <see cref="ObservableValue{T}.Set"/>, который
    /// райзит событие всегда — см. README, раздел 4.10.
    /// </para>
    /// <para>
    /// Пересчёт выполняется синхронно в момент изменения источника.
    /// Для <c>Count</c> это O(1), для <c>Sum</c> / <c>Min</c> / <c>Max</c> — O(N).
    /// Оптимизация (инкрементальный пересчёт) — в дорожной карте.
    /// </para>
    /// </remarks>
    public abstract class AggregateNode<TSource, TResult> : IObservableValue<TResult>
    {
        /// <summary>
        /// Компаратор для определения, изменилось ли значение.
        /// </summary>
        private readonly IEqualityComparer<TResult> _comparer;

        /// <summary>
        /// Текущее агрегированное значение.
        /// </summary>
        private TResult _value = default!;

        /// <summary>
        /// Флаг «узел инициализирован» — выставляется в <see cref="Initialize"/>.
        /// </summary>
        private bool _initialized;

        /// <summary>
        /// Флаг «узел освобождён» — выставляется в <see cref="Dispose"/>.
        /// </summary>
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

        /// <summary>
        /// Источник агрегации. Устанавливается в конструкторе.
        /// </summary>
        protected IObservableList<TSource> Source { get; }

        /// <summary>
        /// Создаёт агрегат над указанным источником.
        /// </summary>
        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <param name="comparer">
        /// Компаратор для сравнения значений. Если <c>null</c> —
        /// используется <see cref="EqualityComparer{T}.Default"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Если <paramref name="source"/> — <c>null</c>.</exception>
        /// <remarks>
        /// Конструктор <b>не</b> подписывается на источник и не вычисляет
        /// начальное значение. Это делает <see cref="Initialize"/>, который
        /// должен вызвать наследник в конце своего конструктора.
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
        /// Метод вызывается:
        /// <list type="bullet">
        /// <item>один раз при <see cref="Initialize"/> — для начального значения;</item>
        /// <item>при каждом изменении источника.</item>
        /// </list>
        /// Метод не должен иметь побочных эффектов.
        /// </remarks>
        protected abstract TResult Recalculate();

        /// <summary>
        /// Инициализирует узел: вычисляет начальное значение и подписывается
        /// на изменения источника.
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
        /// Отписывается от источника.
        /// </summary>
        /// <remarks>
        /// Идемпотентен. Источник не удаляется — это ответственность вызывающего.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_initialized)
                Source.Changed -= OnSourceChanged;
        }

        /// <summary>
        /// Обработчик изменений источника: пересчитывает значение
        /// и райзит <see cref="Changed"/>, если оно изменилось.
        /// </summary>
        private void OnSourceChanged(Change<TSource> change)
        {
            var oldValue = _value;
            var newValue = Recalculate();

            if (_comparer.Equals(oldValue, newValue))
                return;

            _value = newValue;

            Raise(new ValueChange<TResult>(oldValue, newValue));
        }

        /// <summary>
        /// Публикует изменение всем подписчикам <see cref="Changed"/>.
        /// </summary>
        /// <remarks>
        /// Если один из подписчиков выбрасывает исключение, остальные всё равно
        /// получат событие. См. README, раздел 4.8.
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