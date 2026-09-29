using System;
using System.Collections;
using System.Collections.Generic;

namespace ReactiveCollections
{
    public abstract class ObservableNode<T> : IObservableList<T>
    {
        protected readonly List<T> Items = new();

        public event Action<Change<T>>? Changed;

        public int Count => Items.Count;

        public T this[int index] => Items[index];

        public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected bool AddInternal(T item)
        {
            Items.Add(item);

            Raise(Change<T>.Add(item));

            return true;
        }

        protected bool RemoveInternal(T item)
        {
            if (!Items.Remove(item))
                return false;

            Raise(Change<T>.Remove(item));

            return true;
        }

        protected bool RemoveAtInternal(int index)
        {
            if (index < 0 || index >= Items.Count) return false;
            var item = Items[index];
            Items.RemoveAt(index);
            Raise(Change<T>.Remove(item));
            return true;
        }

        protected bool UpdateInternal(T item)
        {
            if (!Items.Contains(item))
                return false;

            Raise(Change<T>.Update(item));

            return true;
        }

        protected bool UpdateAtInternal(int index)
        {
            if (index < 0 || index >= Items.Count) return false;
            Raise(Change<T>.Update(Items[index]));
            return true;
        }

        protected bool ReplaceInternal(T oldItem, T newItem)
        {
            int index = Items.IndexOf(oldItem);

            if (index < 0)
                return false;

            Items[index] = newItem;

            Raise(Change<T>.Replace(oldItem, newItem));

            return true;
        }

        protected bool ReplaceAtInternal(int index, T newItem)
        {
            if (index < 0 || index >= Items.Count) return false;
            var oldItem = Items[index];
            Items[index] = newItem;
            Raise(Change<T>.Replace(oldItem, newItem));
            return true;
        }

        protected bool ResetInternal()
        {
            if (Items.Count == 0)
                return false;

            Items.Clear();

            Raise(Change<T>.Reset());

            return true;
        }

        /// <summary>
        /// Публикует изменение всем подписчикам <see cref="Changed"/>.
        /// Если один из подписчиков выбрасывает исключение, остальные всё равно получат событие.
        /// Одиночное исключение пробрасывается как есть, несколько — как <see cref="AggregateException"/>.
        /// </summary>
        protected void Raise(Change<T> change)
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
    }
}
