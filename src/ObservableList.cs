using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Корневой изменяемый список и основной источник коллекционных изменений.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    public class ObservableList<T> : ObservableNode<T>
    {
        private sealed class BatchScope : IDisposable
        {
            private readonly ObservableList<T> _owner;
            private bool _disposed;

            public BatchScope(ObservableList<T> owner) => _owner = owner;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _owner.EndBatchMode();
            }
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        /// <summary>
        /// Добавляет элемент в конец. Возвращает <c>true</c>, если элемент добавлен.
        /// </summary>
        public bool Add(T item) => AddInternal(item);

        /// <summary>
        /// Вставляет элемент на позицию <paramref name="index"/>. Возвращает
        /// <c>true</c>, если операция выполнена.
        /// </summary>
        public bool AddAt(int index, T item) => AddAtInternal(index, item);

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        /// <summary>
        /// Удаляет первый элемент, равный <paramref name="item"/> по <c>Equals</c>.
        /// Возвращает <c>true</c>, если элемент найден и удалён.
        /// </summary>
        public bool Remove(T item) => RemoveInternal(item);

        /// <summary>
        /// Удаляет элемент по индексу. Возвращает <c>true</c>, если операция выполнена.
        /// </summary>
        public bool RemoveAt(int index) => RemoveAtInternal(index);

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        /// <summary>
        /// Уведомляет об изменении полей объекта (mutable-модель). Возвращает
        /// <c>true</c>, если элемент присутствует в коллекции.
        /// </summary>
        public bool Update(T item) => UpdateInternal(item);

        /// <summary>
        /// Уведомляет об изменении элемента по индексу (mutable-модель).
        /// Возвращает <c>true</c>, если операция выполнена.
        /// </summary>
        public bool UpdateAt(int index) => UpdateAtInternal(index);

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        /// <summary>
        /// Заменяет первый элемент, равный <paramref name="oldItem"/> по <c>Equals</c>,
        /// на <paramref name="newItem"/>. Возвращает <c>true</c>, если элемент найден.
        /// </summary>
        public bool Replace(T oldItem, T newItem) => ReplaceInternal(oldItem, newItem);

        /// <summary>
        /// Заменяет элемент по индексу. Возвращает <c>true</c>, если операция выполнена.
        /// </summary>
        public bool ReplaceAt(int index, T newItem) => ReplaceAtInternal(index, newItem);

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        /// <summary>
        /// Перемещает элемент с позиции <paramref name="fromIndex"/>
        /// на <paramref name="toIndex"/>. Возвращает <c>true</c>, если индексы
        /// валидны и различаются.
        /// </summary>
        public bool Move(int fromIndex, int toIndex) => MoveInternal(fromIndex, toIndex);

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        /// <summary>
        /// Полностью очищает коллекцию. Возвращает <c>true</c>, если состояние
        /// коллекции изменилось.
        /// </summary>
        public bool Reset() => ResetInternal();

        // -------------------------------------------------------------------
        // Batch
        // -------------------------------------------------------------------

        /// <summary>
        /// Открывает режим батчинга. Все изменения накапливаются и публикуются
        /// одним <see cref="BatchChange{T}"/> при закрытии возвращённого scope.
        /// </summary>
        public IDisposable Batch()
        {
            BeginBatchMode();
            return new BatchScope(this);
        }

        /// <summary>Открывает батч. Парный метод — <see cref="EndUpdate"/>.</summary>
        public void BeginUpdate() => BeginBatchMode();

        /// <summary>
        /// Закрывает батч. Если накоплены изменения, публикует один
        /// <see cref="BatchChange{T}"/> и возвращает <c>true</c>.
        /// </summary>
        public bool EndUpdate() => EndBatchMode();
    }
}