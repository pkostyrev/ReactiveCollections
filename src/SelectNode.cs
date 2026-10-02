using System;

namespace ReactiveCollections
{
    /// <summary>
    /// Преобразует TSource → TResult, сохраняя существующий результат при
    /// <c>Update</c> и <c>Move</c> и создавая новый при <c>Replace</c>.
    /// </summary>
    /// <typeparam name="TSource">Тип элемента источника.</typeparam>
    /// <typeparam name="TResult">Тип элемента результата.</typeparam>
    /// <remarks>
    /// Маппинг 1:1 — индекс результата совпадает с индексом источника.
    /// </remarks>
    public class SelectNode<TSource, TResult> : ProjectionNode<TSource, TResult>
    {
        private readonly Func<TSource, TResult> _factory;
        private readonly Action<TSource, TResult> _updater;

        /// <param name="factory">Фабрика нового результата.</param>
        /// <param name="updater">
        /// Обновлятор существующего результата при <c>Update</c>. Для случаев,
        /// когда обновление не требуется — <c>(_, _) =&gt; { }</c>.
        /// </param>
        public SelectNode(
            IObservableList<TSource> source,
            Func<TSource, TResult> factory,
            Action<TSource, TResult> updater) : base(source)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _updater = updater ?? throw new ArgumentNullException(nameof(updater));

            Initialize();
        }

        protected override void OnAdd(AddChange<TSource> change)
        {
            var result = _factory(change.Item);
            _updater(change.Item, result);

            AddAtInternal(change.Index, result);
        }

        protected override void OnRemove(RemoveChange<TSource> change)
        {
            RemoveAtInternal(change.Index);
        }

        protected override void OnUpdate(UpdateChange<TSource> change)
        {
            var result = Items[change.Index];
            _updater(change.Item, result);
            UpdateAtInternal(change.Index);
        }

        /// <summary>Создаёт новый <typeparamref name="TResult"/> на том же индексе.</summary>
        protected override void OnReplace(ReplaceChange<TSource> change)
        {
            var newResult = _factory(change.NewItem);
            _updater(change.NewItem, newResult);

            ReplaceAtInternal(change.Index, newResult);
        }

        /// <summary>Переставляет существующий результат без пересоздания.</summary>
        protected override void OnMove(MoveChange<TSource> change)
        {
            MoveInternal(change.FromIndex, change.ToIndex);
        }
    }
}