using System;

namespace ReactiveCollections
{
    /// <summary>
    /// База для проекций с одним источником.
    /// Подписывается на <see cref="Source"/>, транслирует изменения через
    /// виртуальные хуки <c>OnAdd</c> / <c>OnRemove</c> / <c>OnUpdate</c> /
    /// <c>OnReplace</c> / <c>OnReset</c>.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента результата.</typeparam>
    /// <remarks>
    /// <para>
    /// Наследник обязан вызвать <see cref="Initialize"/> в своём конструкторе
    /// <b>после</b> инициализации всех своих полей. Это связано с тем, что
    /// <see cref="Initialize"/> вызывает виртуальные методы, которые могут
    /// обращаться к полям наследника.
    /// </para>
    /// <para>
    /// Само <see cref="ProjectionNode{TSource, TResult}"/> не вызывает
    /// <see cref="Initialize"/> — это делает конкретный наследник
    /// (<see cref="FilterNode{T}"/>, <see cref="SelectNode{TSource, TResult}"/>
    /// и т.п.).
    /// </para>
    /// </remarks>
    public abstract class ProjectionNode<TSource, TResult> : ObservableNode<TResult>
    {
        /// <summary>
        /// Источник изменений. Не переприсваивается после конструирования.
        /// </summary>
        /// <remarks>
        /// <c>protected</c> — для наследников, которым нужен доступ к элементам
        /// источника напрямую (например, <c>SelectNode.OnAdd</c> читает
        /// <c>change.Item</c>, но не сам <see cref="Source"/>).
        /// </remarks>
        protected readonly IObservableList<TSource> Source;

        /// <summary>
        /// Защита от повторного вызова <see cref="Initialize"/>.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Создаёт проекцию с указанным источником.
        /// </summary>
        /// <param name="source">Источник изменений. Не может быть <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Если <paramref name="source"/> — <c>null</c>.</exception>
        /// <remarks>
        /// Конструктор <b>не</b> вызывает <see cref="Initialize"/> и не подписывается
        /// на источник. Это делает наследник.
        /// </remarks>
        protected ProjectionNode(IObservableList<TSource> source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        /// <summary>
        /// Считывает текущее состояние <see cref="Source"/> и подписывается на его изменения.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если метод вызван повторно.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Если узел уже освобождён.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Метод вызывается ровно один раз — из конструктора наследника,
        /// после инициализации его полей. Ручной вызов извне не предполагается.
        /// </para>
        /// <para>
        /// На каждый существующий элемент <see cref="Source"/> генерируется
        /// синтетический <see cref="ChangeType.Add"/> через <see cref="OnAdd"/>.
        /// Это значит, что N элементов источника дадут N вызовов <c>OnAdd</c>
        /// на этапе инициализации — см. README, раздел 4.6.
        /// </para>
        /// </remarks>
        protected void Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException(
                    "Projection node is already initialized.");

            ThrowIfDisposed();

            _isInitialized = true;

            foreach (var item in Source)
            {
                OnAdd(Change<TSource>.Add(item));
            }

            Source.Changed += OnSourceChanged;
        }

        /// <summary>
        /// Отписывается от источника при <see cref="ObservableNode{TResult}.Dispose"/>.
        /// </summary>
        /// <remarks>
        /// Источник <b>не</b> удаляется — это ответственность вызывающего.
        /// Если <see cref="Initialize"/> не был вызван (например, конструктор
        /// наследника бросил исключение), отписка не производится.
        /// </remarks>
        protected override void DisposeCore()
        {
            if (_isInitialized)
            {
                Source.Changed -= OnSourceChanged;
            }

            base.DisposeCore();
        }

        /// <summary>
        /// Обработчик событий источника. Диспетчеризует по <see cref="Change{T}.Type"/>.
        /// </summary>
        /// <remarks>
        /// <c>switch</c> без ветки <c>default</c> — сознательно: если в
        /// <see cref="ChangeType"/> появится новый член, компилятор об этом
        /// не сообщит, но обработка явно требует обновления. Пока новых членов
        /// нет, поведение безопасно.
        /// </remarks>
        private void OnSourceChanged(Change<TSource> change)
        {
            switch (change.Type)
            {
                case ChangeType.Add:
                    OnAdd(change);
                    break;

                case ChangeType.Remove:
                    OnRemove(change);
                    break;

                case ChangeType.Replace:
                    OnReplace(change);
                    break;

                case ChangeType.Update:
                    OnUpdate(change);
                    break;

                case ChangeType.Reset:
                    OnReset(change);
                    break;
            }
        }

        /// <summary>
        /// Хук добавления элемента источника. По умолчанию — no-op.
        /// </summary>
        /// <param name="change">Изменение типа <see cref="ChangeType.Add"/>.</param>
        protected virtual void OnAdd(Change<TSource> change)
        {
        }

        /// <summary>
        /// Хук удаления элемента источника. По умолчанию — no-op.
        /// </summary>
        /// <param name="change">Изменение типа <see cref="ChangeType.Remove"/>.</param>
        protected virtual void OnRemove(Change<TSource> change)
        {
        }

        /// <summary>
        /// Хук обновления элемента источника. По умолчанию — no-op.
        /// </summary>
        /// <param name="change">Изменение типа <see cref="ChangeType.Update"/>.</param>
        protected virtual void OnUpdate(Change<TSource> change)
        {
        }

        /// <summary>
        /// Хук замены элемента источника. По умолчанию эмулирует замену через
        /// <see cref="OnRemove"/> + <see cref="OnAdd"/>.
        /// </summary>
        /// <param name="change">Изменение типа <see cref="ChangeType.Replace"/>.</param>
        /// <exception cref="InvalidOperationException">
        /// Если <see cref="Change{T}.OldItem"/> не заполнен.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Ожидается, что <see cref="Change{T}.OldItem"/> содержит заменяемый
        /// элемент. Если он равен <c>default</c> (для ссылочных типов — <c>null</c>,
        /// для значимых — <c>default(T)</c>), выбрасывается исключение.
        /// </para>
        /// <para>
        /// Наследники могут переопределить этот метод, чтобы дать более точную
        /// семантику. Например, <see cref="FilterNode{T}"/> реализует замену
        /// с учётом предиката, а <see cref="SelectNode{TSource, TResult}"/> —
        /// через <c>_updater</c>.
        /// </para>
        /// </remarks>
        protected virtual void OnReplace(Change<TSource> change)
        {
            if (change.OldItem is null)
                throw new InvalidOperationException(
                    "Change.Replace was raised without OldItem.");

            // Стандартное поведение: удалить старое, добавить новое.
            OnRemove(Change<TSource>.Remove(change.OldItem));
            OnAdd(Change<TSource>.Add(change.Item));
        }

        /// <summary>
        /// Хук полного сброса источника. По умолчанию очищает результат
        /// через <see cref="ObservableNode{T}.ResetInternal"/>.
        /// </summary>
        /// <param name="change">Изменение типа <see cref="ChangeType.Reset"/>.</param>
        /// <remarks>
        /// <see cref="ObservableNode{T}.ResetInternal"/> не райзит событие,
        /// если результат уже пуст. Учитывай это в наследниках, если логика
        /// <c>Reset</c> должна отрабатывать всегда.
        /// </remarks>
        protected virtual void OnReset(Change<TSource> change)
        {
            ResetInternal();
        }
    }
}