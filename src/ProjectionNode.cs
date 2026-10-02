using System;

namespace ReactiveCollections
{
    /// <summary>
    /// База для одноисточниковых проекций. Подписывается на <see cref="Source"/>,
    /// транслирует изменения через виртуальные хуки.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента результата.</typeparam>
    /// <remarks>
    /// Наследник обязан вызвать <see cref="Initialize"/> в своём конструкторе
    /// после инициализации всех полей.
    /// </remarks>
    public abstract class ProjectionNode<TSource, TResult> : ObservableNode<TResult>
    {
        /// <summary>Источник изменений.</summary>
        protected readonly IObservableList<TSource> Source;

        private bool _isInitialized;

        /// <param name="source">Источник. Не может быть <c>null</c>.</param>
        /// <remarks>Не подписывается на источник — это делает <see cref="Initialize"/>.</remarks>
        protected ProjectionNode(IObservableList<TSource> source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Читает текущее состояние источника, генерирует <c>OnAdd</c> для каждого
        /// элемента, подписывается на изменения.
        /// </summary>
        /// <exception cref="InvalidOperationException">Если метод вызван повторно.</exception>
        protected void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException(
                    "Projection node is already initialized.");

            ThrowIfDisposed();

            _isInitialized = true;

            for (int i = 0; i < Source.Count; i++)
            {
                OnAdd(new AddChange<TSource>(Source[i], i));
            }

            Source.Changed += OnSourceChanged;
        }

        protected override void DisposeCore()
        {
            if (_isInitialized)
                Source.Changed -= OnSourceChanged;
            base.DisposeCore();
        }

        // -------------------------------------------------------------------
        // Диспетчеризация
        // -------------------------------------------------------------------

        private void OnSourceChanged(Change<TSource> change)
        {
            switch (change)
            {
                case BatchChange<TSource> batch:
                    foreach (var inner in batch.Changes)
                        OnSourceChanged(inner);
                    break;

                case AddChange<TSource> add: OnAdd(add); break;
                case RemoveChange<TSource> remove: OnRemove(remove); break;
                case UpdateChange<TSource> update: OnUpdate(update); break;
                case ReplaceChange<TSource> replace: OnReplace(replace); break;
                case MoveChange<TSource> move: OnMove(move); break;
                case ResetChange<TSource> reset: OnReset(reset); break;

                default:
                    throw new NotSupportedException(
                        $"Unsupported change: {change.GetType().Name}");
            }
        }

        // -------------------------------------------------------------------
        // Хуки
        // -------------------------------------------------------------------

        /// <summary>Добавление элемента источника. По умолчанию — no-op.</summary>
        protected virtual void OnAdd(AddChange<TSource> change) { }

        /// <summary>Удаление элемента источника. По умолчанию — no-op.</summary>
        protected virtual void OnRemove(RemoveChange<TSource> change) { }

        /// <summary>Обновление элемента источника. По умолчанию — no-op.</summary>
        protected virtual void OnUpdate(UpdateChange<TSource> change) { }

        /// <summary>
        /// Замена элемента источника. По умолчанию эмулируется двумя
        /// изменениями: <see cref="RemoveChange{TSource}"/> и
        /// <see cref="AddChange{TSource}"/>.
        /// </summary>
        protected virtual void OnReplace(ReplaceChange<TSource> change)
        {
            OnRemove(new RemoveChange<TSource>(change.OldItem, change.Index));
            OnAdd(new AddChange<TSource>(change.NewItem, change.Index));
        }

        /// <summary>
        /// Перемещение элемента источника. По умолчанию бросает
        /// <see cref="NotSupportedException"/> — наследник должен либо
        /// реализовать трансляцию индексов, либо явно признать, что Move
        /// для него не поддерживается.
        /// </summary>
        protected virtual void OnMove(MoveChange<TSource> change)
        {
            throw new NotSupportedException(
                $"{GetType().Name} does not support Move changes.");
        }

        /// <summary>
        /// Полный сброс источника. По умолчанию очищает результат через
        /// <see cref="ResetInternal"/>.
        /// </summary>
        protected virtual void OnReset(ResetChange<TSource> change)
        {
            ResetInternal();
        }
    }
}