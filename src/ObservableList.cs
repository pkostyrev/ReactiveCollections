using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Корневой изменяемый список — единственный узел, который пользователь
    /// создаёт вручную как источник данных.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Поддерживает режим батчинга: накопление изменений и их публикация
    /// одним <see cref="ChangeType.Batch"/>. См. <see cref="Batch"/>,
    /// <see cref="BeginUpdate"/>, <see cref="EndUpdate"/>.
    /// </para>
    /// <para>
    /// Коллекция не является потокобезопасной — см. README, раздел 4.7.
    /// </para>
    /// </remarks>
    public class ObservableList<T> : ObservableNode<T>
    {
        /// <summary>
        /// Обёртка <see cref="IDisposable"/>, закрывающая батч при выходе из
        /// <c>using</c>-блока.
        /// </summary>
        private sealed class BatchScope : IDisposable
        {
            private readonly ObservableList<T> _owner;
            private bool _disposed;

            public BatchScope(ObservableList<T> owner)
            {
                _owner = owner;
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                _owner.EndBatchMode();
            }
        }

        /// <summary>Добавляет элемент в конец коллекции и публикует <see cref="ChangeType.Add"/>.</summary>
        /// <returns>Всегда <c>true</c>.</returns>
        public bool Add(T item) => AddInternal(item);

        /// <summary>Удаляет первый элемент, равный <paramref name="item"/>, и публикует <see cref="ChangeType.Remove"/>.</summary>
        /// <returns><c>true</c>, если элемент найден и удалён; иначе <c>false</c>.</returns>
        public bool Remove(T item) => RemoveInternal(item);

        /// <summary>Заменяет первый элемент, равный <paramref name="oldItem"/>, на <paramref name="newItem"/>.</summary>
        /// <returns><c>true</c>, если <paramref name="oldItem"/> найден; иначе <c>false</c>.</returns>
        public bool Replace(T oldItem, T newItem) => ReplaceInternal(oldItem, newItem);

        /// <summary>Публикует <see cref="ChangeType.Update"/> для элемента, если он присутствует в коллекции.</summary>
        /// <returns><c>true</c>, если элемент найден; иначе <c>false</c>.</returns>
        public bool Update(T item) => UpdateInternal(item);

        /// <summary>Полностью очищает коллекцию и публикует <see cref="ChangeType.Reset"/>.</summary>
        /// <returns><c>true</c>, если коллекция была непустой; иначе <c>false</c>.</returns>
        public bool Reset() => ResetInternal();

        /// <summary>
        /// Открывает режим батчинга на время жизни <see cref="IDisposable"/>.
        /// </summary>
        /// <returns>
        /// <see cref="IDisposable"/>, при вызове <c>Dispose</c> закрывающий батч
        /// и райзящий накопленные изменения одним <see cref="ChangeType.Batch"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Если батч уже открыт.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Предпочтительный способ батчинга — через <c>using</c>:
        /// </para>
        /// <code>
        /// using (list.Batch())
        /// {
        ///     list.Add(1);
        ///     list.Add(2);
        /// }
        /// // одно Change&lt;T&gt;.Batch-событие с двумя Add
        /// </code>
        /// <para>
        /// Все изменения, выполненные внутри блока, копятся в буфере.
        /// Подписчики <see cref="ObservableNode{T}.Changed"/> не получают
        /// событий до закрытия батча.
        /// </para>
        /// </remarks>
        public IDisposable Batch()
        {
            BeginBatchMode();
            return new BatchScope(this);
        }

        /// <summary>
        /// Открывает режим батчинга. Парный метод — <see cref="EndUpdate"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если батч уже открыт.
        /// </exception>
        /// <remarks>
        /// Предпочитайте <see cref="Batch"/> — он закрывает батч даже при
        /// исключении. Этот метод — для случаев, когда <c>using</c> неудобен.
        /// </remarks>
        public void BeginUpdate() => BeginBatchMode();

        /// <summary>
        /// Закрывает режим батчинга и райзит накопленные изменения одним
        /// <see cref="ChangeType.Batch"/>.
        /// </summary>
        /// <returns>
        /// <c>true</c>, если событие было райзнуто; <c>false</c>, если буфер пуст.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Если батч не был открыт.
        /// </exception>
        public bool EndUpdate() => EndBatchMode();
    }
}