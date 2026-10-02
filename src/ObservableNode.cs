using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// База для всех узлов: хранит элементы, публикует изменения,
    /// управляет жизненным циклом.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// Наследники могут изменять состояние через методы <c>*Internal</c> или
    /// напрямую через <see cref="Items"/> при необходимости внутренней
    /// перестройки. Прямые изменения <see cref="Items"/> сами по себе не
    /// публикуют <see cref="Changed"/>; наследник должен самостоятельно
    /// уведомить подписчиков.
    /// </remarks>
    public abstract class ObservableNode<T> : IObservableList<T>
    {
        /// <summary>Хранилище элементов. Изменения — только через <c>*Internal</c>.</summary>
        protected readonly List<T> Items = new();

        private bool _disposed;
        private List<Change<T>>? _batchBuffer;

        /// <inheritdoc />
        public event Action<Change<T>>? Changed;

        /// <inheritdoc />
        public int Count => Items.Count;

        /// <inheritdoc />
        public T this[int index] => Items[index];

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // -------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------

        /// <summary>
        /// Помечает узел освобождённым и сбрасывает буфер активного батча.
        /// Идемпотентен.
        /// </summary>
        /// <remarks>
        /// После <c>Dispose</c> мутации бросают <see cref="ObjectDisposedException"/>,
        /// чтение разрешено. Источники, на которые подписан узел, не освобождаются.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _batchBuffer = null;
            DisposeCore();
        }

        /// <summary>
        /// Точка расширения для отписки от источников при <see cref="Dispose"/>.
        /// </summary>
        protected virtual void DisposeCore() { }

        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        // -------------------------------------------------------------------
        // Add
        // -------------------------------------------------------------------

        /// <summary>Добавляет элемент в конец с <see cref="AddChange{T}"/>.</summary>
        protected bool AddInternal(T item)
        {
            ThrowIfDisposed();
            Items.Add(item);
            Raise(new AddChange<T>(item, Items.Count - 1));
            return true;
        }

        /// <summary>Вставляет элемент на позицию с <see cref="AddChange{T}"/>.</summary>
        protected bool AddAtInternal(int index, T item)
        {
            ThrowIfDisposed();
            if (index < 0 || index > Items.Count) return false;
            Items.Insert(index, item);
            Raise(new AddChange<T>(item, index));
            return true;
        }

        // -------------------------------------------------------------------
        // Remove
        // -------------------------------------------------------------------

        /// <summary>Удаляет первый элемент, равный <paramref name="item"/> по <c>Equals</c>.</summary>
        protected bool RemoveInternal(T item)
        {
            ThrowIfDisposed();
            int index = Items.IndexOf(item);
            if (index < 0) return false;
            Items.RemoveAt(index);
            Raise(new RemoveChange<T>(item, index));
            return true;
        }

        /// <summary>Удаляет элемент по индексу.</summary>
        protected bool RemoveAtInternal(int index)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= Items.Count) return false;
            var item = Items[index];
            Items.RemoveAt(index);
            Raise(new RemoveChange<T>(item, index));
            return true;
        }

        // -------------------------------------------------------------------
        // Update
        // -------------------------------------------------------------------

        /// <summary>
        /// Публикует <see cref="UpdateChange{T}"/> для первого равного элемента.
        /// Ничего не мутирует.
        /// </summary>
        protected bool UpdateInternal(T item)
        {
            ThrowIfDisposed();

            int index = Items.IndexOf(item);
            if (index < 0)
                return false;

            Raise(new UpdateChange<T>(item, index));
            return true;
        }

        /// <summary>
        /// Публикует <see cref="UpdateChange{T}"/> для элемента по индексу.
        /// Ничего не мутирует.
        /// </summary>
        protected bool UpdateAtInternal(int index)
        {
            ThrowIfDisposed();

            if (index < 0 || index >= Items.Count)
                return false;

            Raise(new UpdateChange<T>(Items[index], index));
            return true;
        }

        // -------------------------------------------------------------------
        // Replace
        // -------------------------------------------------------------------

        /// <summary>
        /// Заменяет первый элемент, равный <paramref name="oldItem"/> по <c>Equals</c>,
        /// на <paramref name="newItem"/>.
        /// </summary>
        protected bool ReplaceInternal(T oldItem, T newItem)
        {
            ThrowIfDisposed();
            int index = Items.IndexOf(oldItem);
            if (index < 0) return false;
            Items[index] = newItem;
            Raise(new ReplaceChange<T>(oldItem, newItem, index));
            return true;
        }

        /// <summary>Заменяет элемент по индексу.</summary>
        protected bool ReplaceAtInternal(int index, T newItem)
        {
            ThrowIfDisposed();
            if (index < 0 || index >= Items.Count) return false;
            var oldItem = Items[index];
            Items[index] = newItem;
            Raise(new ReplaceChange<T>(oldItem, newItem, index));
            return true;
        }

        // -------------------------------------------------------------------
        // Move
        // -------------------------------------------------------------------

        /// <summary>
        /// Перемещает элемент с <paramref name="fromIndex"/> на
        /// <paramref name="toIndex"/>. <paramref name="toIndex"/> — позиция
        /// в финальном списке.
        /// </summary>
        protected bool MoveInternal(int fromIndex, int toIndex)
        {
            ThrowIfDisposed();
            if (fromIndex < 0 || fromIndex >= Items.Count) return false;
            if (toIndex < 0 || toIndex >= Items.Count) return false;
            if (fromIndex == toIndex) return false;

            var item = Items[fromIndex];
            Items.RemoveAt(fromIndex);
            Items.Insert(toIndex, item);

            Raise(new MoveChange<T>(item, fromIndex, toIndex));
            return true;
        }

        // -------------------------------------------------------------------
        // Reset
        // -------------------------------------------------------------------

        /// <summary>
        /// Очищает коллекцию. Возвращает <c>false</c> без события,
        /// если коллекция уже пуста.
        /// </summary>
        protected bool ResetInternal()
        {
            ThrowIfDisposed();
            if (Items.Count == 0) return false;
            Items.Clear();
            Raise(new ResetChange<T>());
            return true;
        }

        // -------------------------------------------------------------------
        // Raise
        // -------------------------------------------------------------------

        /// <summary>
        /// Публикует изменение. Если открыт батч — кладёт в буфер,
        /// иначе райзит немедленно.
        /// </summary>
        protected void Raise(Change<T> change)
        {
            if (_batchBuffer is not null)
            {
                _batchBuffer.Add(change);
                return;
            }
            RaiseImmediate(change);
        }

        /// <summary>
        /// Публикует изменение немедленно, минуя буфер батча.
        /// </summary>
        /// <remarks>
        /// Все подписчики вызываются, даже если один из них бросает исключение.
        /// Одиночное исключение пробрасывается как есть, несколько — как
        /// <see cref="AggregateException"/>.
        /// </remarks>
        protected void RaiseImmediate(Change<T> change)
        {
            var handler = Changed;
            if (handler == null) return;

            List<Exception>? errors = null;

            foreach (var d in handler.GetInvocationList())
            {
                try { ((Action<Change<T>>)d).Invoke(change); }
                catch (Exception ex) { (errors ??= new()).Add(ex); }
            }

            if (errors == null) return;
            if (errors.Count == 1) throw errors[0];
            throw new AggregateException(errors);
        }

        // -------------------------------------------------------------------
        // Batch
        // -------------------------------------------------------------------

        /// <summary>
        /// Открывает режим батчинга. Вложенные батчи запрещены.
        /// </summary>
        /// <exception cref="InvalidOperationException">Если батч уже открыт.</exception>
        protected void BeginBatchMode()
        {
            if (_batchBuffer is not null)
                throw new InvalidOperationException(
                    "Batch is already in progress. Nested batches are not allowed.");

            _batchBuffer = new List<Change<T>>();
        }

        /// <summary>
        /// Закрывает батч и публикует накопленные изменения одним
        /// <see cref="BatchChange{T}"/>. Возвращает <c>false</c> без события,
        /// если буфер пуст.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если батч не открыт или содержит вложенный <see cref="BatchChange{T}"/>.
        /// </exception>
        protected bool EndBatchMode()
        {
            if (_batchBuffer is null)
                throw new InvalidOperationException("Batch is not in progress.");

            var buffer = _batchBuffer;
            _batchBuffer = null;

            if (buffer.Count == 0)
                return false;

            for (int i = 0; i < buffer.Count; i++)
            {
                if (buffer[i] is BatchChange<T>)
                    throw new InvalidOperationException(
                        "Nested batches are not allowed.");
            }

            RaiseImmediate(new BatchChange<T>(buffer));
            return true;
        }
    }
}