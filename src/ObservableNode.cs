using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollections
{
    /// <summary>
    /// База для всех узлов реактивной коллекции: хранит элементы, публикует изменения
    /// и управляет своим жизненным циклом.
    /// </summary>
    /// <typeparam name="T">Тип элемента коллекции.</typeparam>
    /// <remarks>
    /// <para>
    /// Наследники изменяют состояние исключительно через методы
    /// <c>AddInternal</c> / <c>RemoveInternal</c> / <c>UpdateInternal</c> /
    /// <c>ReplaceInternal</c> / <c>ResetInternal</c> и их индексные варианты.
    /// Прямые мутации <see cref="Items"/> обходят событие <see cref="Changed"/>
    /// и нарушают инвариант «состояние ↔ событие».
    /// </para>
    /// <para>
    /// Все методы мутации возвращают <c>bool</c>:
    /// <c>true</c> — изменение применено, <c>false</c> — нет (элемент/индекс не найден,
    /// список пуст при <c>Reset</c>). Возврат <c>false</c> не райзит событие.
    /// </para>
    /// <para>
    /// <b>Модель жизненного цикла:</b> <see cref="Dispose"/> отписывает узел
    /// от источников, но не удаляет сами источники. Удаление цепочки — ответственность
    /// вызывающего: он владеет тем, что создал, и решает, когда освобождать.
    /// См. README, раздел 4.4.
    /// </para>
    /// </remarks>
    public abstract class ObservableNode<T> : IObservableList<T>
    {
        /// <summary>
        /// Хранилище элементов. Инвариант: изменения только через <c>*Internal</c>-методы.
        /// </summary>
        protected readonly List<T> Items = new();

        /// <summary>
        /// Буфер изменений при активном батче. <c>null</c>, если батч не открыт.
        /// </summary>
        /// <remarks>
        /// Пока буфер не <c>null</c>, все вызовы <see cref="Raise"/> складывают
        /// изменения сюда, а не райзят подписчикам. При закрытии батча
        /// накопленные изменения уходят одним <see cref="ChangeType.Batch"/>.
        /// </remarks>
        private List<Change<T>>? _batchBuffer;

        /// <summary>
        /// Флаг «узел освобождён». Устанавливается в <see cref="Dispose"/>.
        /// </summary>
        private bool _disposed;

        /// <inheritdoc cref="IObservableList{T}.Changed"/>
        public event Action<Change<T>>? Changed;

        /// <inheritdoc />
        public int Count => Items.Count;

        /// <inheritdoc />
        public T this[int index] => Items[index];

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Освобождает узел: отписывается от источников и переводит узел
        /// в состояние «disposed».
        /// </summary>
        /// <remarks>
        /// <para>
        /// Метод идемпотентен: повторный вызов не выполняет никаких действий.
        /// </para>
        /// <para>
        /// <b>Что делает:</b> вызывает <see cref="DisposeCore"/> — переопределяемый
        /// метод, в котором наследники отписываются от своих источников.
        /// </para>
        /// <para>
        /// <b>Что не делает:</b> не вызывает <c>Dispose</c> на источниках.
        /// Если узел был подписан на <see cref="IObservableList{T}"/>-источник,
        /// этот источник не удаляется — он может использоваться другими
        /// потребителями. См. README, раздел 4.4.
        /// </para>
        /// <para>
        /// После <see cref="Dispose"/> операции чтения (<see cref="Count"/>,
        /// индексатор, перечисление) разрешены. Операции мутации через
        /// <c>*Internal</c>-методы бросают <see cref="ObjectDisposedException"/>.
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Если батч остался открытым — просто выбрасываем буфер.
            // Пользователь, забывший EndUpdate, не получит события.
            _batchBuffer = null;

            DisposeCore();
        }

        /// <summary>
        /// Точка расширения для наследников: отписка от источников при <see cref="Dispose"/>.
        /// </summary>
        /// <remarks>
        /// Базовая реализация — no-op. Наследники, которые подписываются
        /// на <c>Changed</c> других узлов, обязаны переопределить этот метод
        /// и отписаться. Примеры: <see cref="ProjectionNode{TSource, TResult}"/>,
        /// <see cref="MergeNode{T}"/>.
        /// </remarks>
        protected virtual void DisposeCore()
        {
        }

        /// <summary>
        /// Бросает <see cref="ObjectDisposedException"/>, если узел уже освобождён.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Если узел освобождён через <see cref="Dispose"/>.
        /// </exception>
        /// <remarks>
        /// Вызывается в начале каждого <c>*Internal</c>-метода. Это делает
        /// мутации после <c>Dispose</c> громкой ошибкой, а не тихим no-op.
        /// </remarks>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Добавляет элемент и публикует <see cref="ChangeType.Add"/>.
        /// </summary>
        /// <returns>Всегда <c>true</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        protected bool AddInternal(T item)
        {
            ThrowIfDisposed();

            Items.Add(item);

            Raise(Change<T>.Add(item));

            return true;
        }

        /// <summary>
        /// Удаляет первый элемент, равный <paramref name="item"/> по
        /// <see cref="EqualityComparer{T}.Default"/>, и публикует <see cref="ChangeType.Remove"/>.
        /// </summary>
        /// <returns><c>true</c>, если элемент найден и удалён; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// При наличии дубликатов неопределённо, какой именно экземпляр будет удалён —
        /// см. README, раздел 4.3. Для удаления конкретного экземпляра используйте
        /// <see cref="RemoveAtInternal"/>.
        /// </remarks>
        protected bool RemoveInternal(T item)
        {
            ThrowIfDisposed();

            if (!Items.Remove(item))
                return false;

            Raise(Change<T>.Remove(item));

            return true;
        }

        /// <summary>
        /// Удаляет элемент по индексу и публикует <see cref="ChangeType.Remove"/>.
        /// </summary>
        /// <returns><c>true</c>, если индекс валиден; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        protected bool RemoveAtInternal(int index)
        {
            ThrowIfDisposed();

            if (index < 0 || index >= Items.Count) return false;
            var item = Items[index];
            Items.RemoveAt(index);
            Raise(Change<T>.Remove(item));
            return true;
        }

        /// <summary>
        /// Публикует <see cref="ChangeType.Update"/>, если элемент присутствует в коллекции.
        /// </summary>
        /// <returns><c>true</c>, если элемент найден; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// Ничего не мутирует — предполагается, что объект уже изменён извне
        /// (mutable-модель). Метод служит только для оповещения подписчиков.
        /// </remarks>
        protected bool UpdateInternal(T item)
        {
            ThrowIfDisposed();

            if (!Items.Contains(item))
                return false;

            Raise(Change<T>.Update(item));

            return true;
        }

        /// <summary>
        /// Публикует <see cref="ChangeType.Update"/> для элемента по индексу.
        /// </summary>
        /// <returns><c>true</c>, если индекс валиден; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        protected bool UpdateAtInternal(int index)
        {
            ThrowIfDisposed();

            if (index < 0 || index >= Items.Count) return false;
            Raise(Change<T>.Update(Items[index]));
            return true;
        }

        /// <summary>
        /// Заменяет первый элемент, равный <paramref name="oldItem"/>, на <paramref name="newItem"/>.
        /// </summary>
        /// <returns><c>true</c>, если элемент найден; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// Публикует <see cref="ChangeType.Replace"/>. Поиск — первый равный
        /// по <see cref="EqualityComparer{T}.Default"/>, как в <see cref="List{T}.IndexOf"/>.
        /// </remarks>
        protected bool ReplaceInternal(T oldItem, T newItem)
        {
            ThrowIfDisposed();

            int index = Items.IndexOf(oldItem);

            if (index < 0)
                return false;

            Items[index] = newItem;

            Raise(Change<T>.Replace(oldItem, newItem));

            return true;
        }

        /// <summary>
        /// Заменяет элемент по индексу и публикует <see cref="ChangeType.Replace"/>.
        /// </summary>
        /// <returns><c>true</c>, если индекс валиден; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        protected bool ReplaceAtInternal(int index, T newItem)
        {
            ThrowIfDisposed();

            if (index < 0 || index >= Items.Count) return false;
            var oldItem = Items[index];
            Items[index] = newItem;
            Raise(Change<T>.Replace(oldItem, newItem));
            return true;
        }

        /// <summary>
        /// Очищает коллекцию и публикует <see cref="ChangeType.Reset"/>.
        /// </summary>
        /// <returns><c>true</c>, если коллекция была непустой; иначе <c>false</c>.</returns>
        /// <exception cref="ObjectDisposedException">Если узел освобождён.</exception>
        /// <remarks>
        /// Если коллекция пуста — событие не райзится. Это значит, что
        /// <c>Reset</c> на пустой коллекции неотличим от «ничего не произошло».
        /// </remarks>
        protected bool ResetInternal()
        {
            ThrowIfDisposed();

            if (Items.Count == 0)
                return false;

            Items.Clear();

            Raise(Change<T>.Reset());

            return true;
        }

        /// <summary>
        /// Публикует изменение всем подписчикам <see cref="Changed"/>
        /// или складывает его в буфер, если открыт батч.
        /// </summary>
        /// <remarks>
        /// См. <see cref="RaiseImmediate"/> о поведении при отсутствии батча.
        /// </remarks>
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
        /// Публикует изменение всем подписчикам <see cref="Changed"/> немедленно,
        /// минуя буфер.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Если один из подписчиков выбрасывает исключение, остальные всё равно
        /// получат событие. Одиночное исключение пробрасывается как есть,
        /// несколько — как <see cref="AggregateException"/>.
        /// </para>
        /// <para>
        /// Это отличается от стандартного поведения multicast delegate,
        /// где первое исключение прерывает обход — см. README, раздел 4.8.
        /// </para>
        /// </remarks>
        protected void RaiseImmediate(Change<T> change)
        {
            var handler = Changed;
            if (handler == null)
                return;

            List<Exception>? errors = null;

            foreach (var d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<Change<T>>)d).Invoke(change);
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

        /// <summary>
        /// Открывает режим батчинга: последующие изменения копятся в буфере
        /// и райзятся одним <see cref="ChangeType.Batch"/> при
        /// <see cref="EndBatchMode"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Если батч уже открыт. Вложенные батчи запрещены.
        /// </exception>
        /// <remarks>
        /// Метод используется только <see cref="ObservableList{T}"/>.
        /// Проекции (узлы) не должны открывать батч — они разворачивают
        /// приходящий <see cref="ChangeType.Batch"/> в отдельные события.
        /// </remarks>
        protected void BeginBatchMode()
        {
            if (_batchBuffer is not null)
                throw new InvalidOperationException(
                    "Batch is already in progress. Nested batches are not allowed.");

            _batchBuffer = new List<Change<T>>();
        }

        /// <summary>
        /// Закрывает режим батчинга и райзит накопленные изменения одним
        /// <see cref="ChangeType.Batch"/>, если буфер не пуст.
        /// </summary>
        /// <returns>
        /// <c>true</c>, если событие было райзнуто; <c>false</c>, если буфер пуст.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Если батч не был открыт.
        /// </exception>
        protected bool EndBatchMode()
        {
            if (_batchBuffer is null)
                throw new InvalidOperationException(
                    "Batch is not in progress.");

            var buffer = _batchBuffer;
            _batchBuffer = null;

            if (buffer.Count == 0)
                return false;

            RaiseImmediate(Change<T>.Batch(buffer));

            return true;
        }
    }
}