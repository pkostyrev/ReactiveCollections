# ReactiveCollections

Реактивная библиотека коллекций для Unity: живые проекции, группы,
объединения и (в будущем) одиночные значения.

## Содержание

1. [Цель](#1-цель)
2. [Архитектура](#2-архитектура)
3. [Публичный API](#3-публичный-api)
4. [Ограничения и допущения](#4-ограничения-и-допущения)
5. [Сценарии использования](#5-сценарии-использования)
6. [История решений](#6-история-решений)
7. [Дорожная карта](#7-дорожная-карта)

---

## 1. Цель

ReactiveCollections — не копия `ObservableCollection<T>` и не аналог LINQ.
Основная идея — **живой граф данных**: один узел получает изменения
другого узла, преобразует их и автоматически поддерживает своё текущее
состояние.

Ключевой принцип:

> Коллекция хранит **текущее состояние**, а `Change<T>` описывает **переход
> состояния**. Проекции — это живые узлы и могут быть источниками для
> следующих проекций.

Целевые сценарии:

- фильтрация игровых объектов по HP, активности и т.п.;
- преобразование доменной модели в UI-модель (`Player` → `PlayerView`);
- группировка по ключу (`Player` → `Group<TeamId, Player>`);
- объединение двух живых источников;
- наблюдение за вложенными коллекциями (в разработке).

---

## 2. Архитектура

<table>
<tr><th>Уровень</th><th>Ответственность</th><th>Пример</th></tr>
<tr><td><code>Change&lt;T&gt;</code></td><td>Описание изменения состояния</td><td>Add, Remove, Update, Replace, Reset</td></tr>
<tr><td><code>IObservableList&lt;T&gt;</code></td><td>Единый контракт чтения + событие</td><td>Источник или проекция</td></tr>
<tr><td><code>ObservableNode&lt;T&gt;</code></td><td>Хранение результата и публикация событий</td><td>Items + Changed</td></tr>
<tr><td><code>ObservableList&lt;T&gt;</code></td><td>Корневой изменяемый источник</td><td>Add / Remove / Update / Replace / Reset</td></tr>
<tr><td><code>ProjectionNode&lt;TSource, TResult&gt;</code></td><td>База для одноисточниковых проекций</td><td>Filter / Select</td></tr>
<tr><td><code>KeyedProjectionNode&lt;TKey, TSource, TResult&gt;</code></td><td>Общий механизм индексированных групп</td><td>GroupBy</td></tr>
<tr><td><code>MergeNode&lt;T&gt;</code></td><td>Объединение двух живых источников</td><td>Concat двух списков</td></tr>
</table>

Поток изменений:

ObservableList<TSource>
│
▼
ProjectionNode<TSource, TResult> ─── Filter / Select
│
▼
ObservableNode<TResult> ─── сам является IObservableList<TResult>
│
▼
следующий узел / UI / ...

## 3. Публичный API

### 3.1 `Change<T>`

Иммутабельный DTO одного изменения. Пять видов: `Add`, `Remove`, `Update`,
`Replace`, `Reset`.

| Фабрика | Назначение |
|---|---|
| `Change<T>.Add(item)` | Элемент добавлен |
| `Change<T>.Remove(item)` | Элемент удалён |
| `Change<T>.Update(item)` | Элемент изменился, ссылка та же |
| `Change<T>.Replace(old, new)` | Один экземпляр заменён другим |
| `Change<T>.Reset()` | Содержимое могло измениться целиком |

### 3.2 `IObservableList<T>`

```csharp
public interface IObservableList<T> : IReadOnlyList<T>
{
    event Action<Change<T>>? Changed;
}