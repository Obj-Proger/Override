---
id: csharp.oop.struct
title: Структуры
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 3
requires: [csharp.oop.properties]
quickCheck: [struct-copy, struct-method-copy, struct-default-ctor, struct-property-copy]
testMinutes: 6
---

::::warmup

```quiz
id: struct-copy
type: output
prompt: Что выведет код?
code: |
  var a = new Point { X = 1 };
  var b = a;
  b.X = 5;
  Console.WriteLine(a.X);

  struct Point
  {
      public int X;
      public int Y;
  }
answer: "1"
explain: |
  `Point` — структура, значимый тип. `b = a` копирует все поля, и дальше `a` и `b` независимы.
variants:
  - code: |
      var a = new Point { X = 1 };
      var b = a;
      b.X = 5;
      Console.WriteLine(a.X);

      class Point
      {
          public int X;
          public int Y;
      }
    answer: "5"
    explain: |
      Здесь `Point` — класс. `b = a` копирует ссылку, объект один, и изменение через `b` видно через `a`.
```

::::

::::part Объявление и отличия от класса

**Структура** объявляется как класс, только словом `struct`. В ней те же поля, свойства, методы и конструкторы. Главное отличие — структура **значимый тип**:

```csharp
struct Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public double Length() => Math.Sqrt(X * X + Y * Y);
}
```

| | Класс | Структура |
|---|---|---|
| Вид типа | ссылочный | значимый |
| Присваивание и передача в метод | копирует ссылку | копирует все поля |
| Может быть `null` | да | нет (только `Point?`) |
| Где лежит | объект всегда в куче | там, где объявлена переменная или поле |
| Наследование | да | нет: структура не наследуется и не наследует |
| Интерфейсы | да | да |
| `==` по умолчанию | сравнивает ссылки | не определён, пока не объявить |

Из стандартной библиотеки структуры — это `int`, `double`, `bool`, `DateTime`, `TimeSpan`, `Guid`, `decimal`. Все встроенные числовые типы — структуры.

```quiz
id: struct-null
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  Point origin = new Point();
  Point missing = null;
  Console.WriteLine(origin.X);

  struct Point
  {
      public int X;
  }
answer: 2
explain: |
  Структура — значимый тип, у неё не бывает `null`. Для «точки может не быть» есть `Point?`, то есть `Nullable<Point>`.
```

::::

::::part Конструкторы и default

У структуры **всегда** есть способ получить «пустое» значение — все поля нулевые. Его дают `default` и, если в структуре нет своего конструктора без параметров, `new Point()`:

```csharp
Point a = default;      // X = 0, Y = 0
Point b = new Point();  // тоже нули
Point c = new(3, 4);    // конструктор с параметрами
```

С C# 10 в структуре можно объявить свой конструктор без параметров. Ловушка: **`default` его не вызывает**. `default` и элементы нового массива всегда просто обнулены:

```csharp
struct Settings
{
    public int Retries;
    public Settings() { Retries = 3; }
}

var a = new Settings();   // Retries = 3: вызван конструктор
Settings b = default;     // Retries = 0: конструктор не вызывается
var c = new Settings[2];  // у обоих элементов Retries = 0
```

Поэтому структуру проектируют так, чтобы «все нули» было разумным значением: `Money` с нулевой суммой, `Point` в начале координат.

```quiz
id: struct-default-ctor
type: output
prompt: Что выведет код? `$"{a.Retries} {b.Retries}"` подставляет значения через пробел.
code: |
  var a = new Settings();
  Settings b = default;
  Console.WriteLine($"{a.Retries} {b.Retries}");

  struct Settings
  {
      public int Retries;
      public Settings() { Retries = 3; }
  }
answer: 3 0
explain: |
  `new Settings()` вызывает объявленный конструктор без параметров. `default` ничего не вызывает и просто обнуляет все поля.
variants:
  - code: |
      var items = new Settings[2];
      Console.WriteLine(items[1].Retries);

      struct Settings
      {
          public int Retries;
          public Settings() { Retries = 3; }
      }
    answer: "0"
    explain: |
      Элементы нового массива получают значение по умолчанию — обнулённые поля. Конструктор для них не вызывается.
```

::::

::::part Изменяемые структуры и копии

Структура копируется при каждом присваивании, передаче в метод и возврате из метода или свойства. Если структура изменяемая, легко поменять копию и потерять изменение.

**Передача в метод:**

```csharp
var p = new Point(1, 1);
MoveRight(p);
Console.WriteLine(p.X);  // 1: метод сдвинул свою копию

void MoveRight(Point point) => point.X++;
```

**Свойство-структура:**

```csharp
class Shape
{
    public Point Position { get; set; }
}

var shape = new Shape();
shape.Position.X = 5;  // ошибка компиляции CS1612: get вернул копию
```

Компилятор запрещает эту запись: изменилась бы временная копия, а `shape` остался бы прежним. Правильно — взять копию, изменить и записать обратно, или создать новое значение: `shape.Position = new Point(5, shape.Position.Y);`.

**Массив — исключение:** элемент массива — сама переменная, а не копия, поэтому `points[0].X = 5` меняет элемент.

**Вывод: структуры делают неизменяемыми.** Значения задаются в конструкторе, а «изменение» создаёт новую структуру:

```csharp
readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money Add(decimal value) => new(Amount + value, Currency);
}
```

`readonly struct` — компилятор проверяет, что все поля только для чтения. Для копии с изменённым свойством есть `with`: `var moved = point with { X = 10 };`.

```quiz
id: struct-method-copy
type: output
prompt: Что выведет код?
code: |
  var p = new Point { X = 1 };
  Shift(p);
  Console.WriteLine(p.X);

  void Shift(Point point) => point.X += 10;

  struct Point
  {
      public int X;
  }
answer: "1"
explain: |
  Структура передаётся копией. Метод изменил копию, а `p` снаружи остался прежним.
variants:
  - code: |
      var p = new Point { X = 1 };
      Shift(ref p);
      Console.WriteLine(p.X);

      void Shift(ref Point point) => point.X += 10;

      struct Point
      {
          public int X;
      }
    answer: "11"
    explain: |
      С `ref` метод работает с самой переменной `p`, а не с копией.
  - code: |
      var points = new Point[1];
      points[0].X = 7;
      Console.WriteLine(points[0].X);

      struct Point
      {
          public int X;
      }
    answer: "7"
    explain: |
      Элемент массива — сама переменная, не копия, поэтому его поле меняется.
```

```quiz
id: struct-property-copy
type: choice
prompt: Почему `shape.Position.X = 5` не компилируется, если `Position` — свойство типа `Point`, а `Point` — структура?
options:
  - "get свойства возвращает копию, изменение копии сразу пропало бы"
  - "Поля структуры нельзя менять никогда"
  - "Свойство Position приватное"
  - "Структуры нельзя использовать как свойства"
answer: 0
explain: |
  Компилятор защищает от бесполезного изменения временной копии. Для класса `Point` запись бы работала: копировалась бы ссылка на тот же объект.
```

::::

::::part Когда выбирать структуру

Структура уместна, если тип:

- **маленький** — до 16 байт, примерно 2–4 поля-числа: его дешевле копировать, чем ходить по ссылке;
- **представляет одно значение**, как число: координата, деньги, диапазон дат, цвет;
- **неизменяемый**;
- **создаётся часто и в больших количествах** — миллион структур в массиве лежит одним блоком без нагрузки на сборщик мусора, а миллион объектов — это миллион аллокаций.

Класс нужен, если у объекта есть **идентичность** (два пользователя с одинаковым именем — разные пользователи), он большой, изменяется или участвует в наследовании.

Если структура часто попадает в `object` или в переменную интерфейса, каждый раз происходит упаковка — и выгода пропадает.

```quiz
id: struct-or-class
type: choice
prompt: Какой тип лучше сделать структурой?
options:
  - "Координата на карте: широта и долгота, неизменяемая"
  - "Пользователь с именем, почтой, списком заказов и историей входов"
  - "Окно приложения, которое меняет размер и содержимое"
  - "Базовый тип для иерархии фигур"
answer: 0
explain: |
  Координата — маленькое неизменяемое значение без идентичности. У пользователя есть идентичность и много данных, окно изменяемое, а иерархию структура не поддерживает.
```

:::level middle

**Сравнение структур.** Если не переопределить `Equals`, вызывается `ValueType.Equals`, который сравнивает поля через рефлексию — медленно, и при вызове через `object` значения упаковываются. Поэтому в структурах реализуют `IEquatable<T>` или используют `record struct`, где сравнение генерирует компилятор. Подробнее — в теме про равенство.

**Защитные копии.** Если вызвать метод у структуры, переданной через `in`, или у поля `readonly`, а структура не `readonly`, компилятор делает копию перед вызовом: вдруг метод изменит поля. `readonly struct` или `readonly`-методы убирают эти копии.

**`ref struct`.** Структура, которая может жить только на стеке: её нельзя упаковать, положить в поле класса или захватить в лямбду. Так устроены `Span<T>` и `ReadOnlySpan<T>` — о них в разделе «Память».

```quiz
id: valuetype-equals
type: choice
level: middle
prompt: Почему в часто сравниваемой структуре переопределяют Equals или реализуют IEquatable<T>?
options:
  - "Стандартный ValueType.Equals сравнивает поля через рефлексию и упаковывает значения"
  - "Без этого структуры нельзя сравнивать вообще"
  - "Иначе == сравнивает ссылки"
  - "Это требование компилятора для readonly struct"
answer: 0
explain: |
  Своя реализация сравнивает поля напрямую, без рефлексии и упаковки. `record struct` генерирует её автоматически.
```

:::

::::

::::practice

```quiz
id: move-point-task
type: code
lang: csharp
prompt: |
  Допишите метод `Moved`: он возвращает **новую** точку, сдвинутую на `dx` и `dy`. Исходная точка неизменяемая и меняться не должна.
starter: |
  public readonly struct Point
  {
      public int X { get; }
      public int Y { get; }

      public Point(int x, int y)
      {
          X = x;
          Y = y;
      }

      public Point Moved(int dx, int dy)
      {
      }
  }
tests: |
  var start = new Point(1, 2);
  var moved = start.Moved(3, -1);
  Assert.Equal(4, moved.X);
  Assert.Equal(1, moved.Y);
  Assert.Equal(1, start.X);
  Assert.Equal(2, start.Y);
solution: |
  public readonly struct Point
  {
      public int X { get; }
      public int Y { get; }

      public Point(int x, int y)
      {
          X = x;
          Y = y;
      }

      public Point Moved(int dx, int dy) => new(X + dx, Y + dy);
  }
explain: |
  У `readonly struct` нельзя менять свойства, поэтому «изменение» — это новое значение. Так устроены `DateTime.AddDays` и `string.Replace`.
variants:
  - prompt: |
      Допишите метод `Add`: он возвращает новую сумму денег в той же валюте. Исходная сумма меняться не должна.
    starter: |
      public readonly struct Money
      {
          public decimal Amount { get; }
          public string Currency { get; }

          public Money(decimal amount, string currency)
          {
              Amount = amount;
              Currency = currency;
          }

          public Money Add(decimal value)
          {
          }
      }
    tests: |
      var price = new Money(100m, "RUB");
      var total = price.Add(50m);
      Assert.Equal(150m, total.Amount);
      Assert.Equal("RUB", total.Currency);
      Assert.Equal(100m, price.Amount);
    solution: |
      public readonly struct Money
      {
          public decimal Amount { get; }
          public string Currency { get; }

          public Money(decimal amount, string currency)
          {
              Amount = amount;
              Currency = currency;
          }

          public Money Add(decimal value) => new(Amount + value, Currency);
      }
    explain: |
      Неизменяемая структура возвращает новое значение вместо изменения своих полей. Её можно спокойно копировать: копии не разойдутся.
```

```quiz
id: with-struct
type: fill
prompt: Вставьте ключевое слово, чтобы получить копию структуры с другим `X`.
code: |
  var moved = point ___ { X = 10 };
answer: with
explain: |
  `with` создаёт копию структуры (или записи) и меняет в ней перечисленные свойства. Исходное значение не трогается.
```

::::

::::traps

- **Присваивание структуры копирует все поля.** Изменение копии не видно в оригинале.
- **Метод, получивший структуру, меняет свою копию**, если нет `ref`.
- **`obj.Position.X = 5` для свойства-структуры не компилируется.**
- **`default` и элементы массива не вызывают конструктор без параметров.**
- **Структура не бывает `null`** и не участвует в наследовании.
- **Структуру делают маленькой и неизменяемой**, иначе копии разъезжаются.

::::

::::summary

```quiz
id: struct-recall
type: recall
prompt: Чем структура отличается от класса и когда её выбирают?
answer: |
  Структура — **значимый тип**: присваивание, передача в метод и возврат из свойства копируют все поля. Она не бывает `null` (кроме `T?`), не наследуется, но может реализовывать интерфейсы, лежит там, где объявлена.
  `default` и элементы массива — обнулённые поля; объявленный конструктор без параметров (C# 10) при этом не вызывается.
  Изменяемая структура опасна: метод или свойство меняет копию, а `obj.Prop.X = 5` не компилируется. Поэтому структуры делают неизменяемыми (`readonly struct`), а «изменение» возвращает новое значение или делается через `with`.
  Структуру выбирают для маленьких неизменяемых значений без идентичности, которые создаются массово: координаты, деньги, даты. Иначе — класс.
```

::::

::::test

```quiz
id: test-struct-inherit
type: choice
part: "Объявление и отличия от класса"
prompt: Что из этого структура НЕ умеет?
options:
  - "Наследоваться от другой структуры или класса"
  - "Реализовывать интерфейсы"
  - "Иметь методы и свойства"
  - "Иметь конструкторы с параметрами"
answer: 0
explain: |
  Структуры не участвуют в наследовании. Всё остальное у них есть, как у классов.
```

```quiz
id: test-builtin-struct
type: multi
part: "Объявление и отличия от класса"
prompt: Какие из этих типов — структуры?
options:
  - "DateTime"
  - "string"
  - "int"
  - "Guid"
  - "int[]"
answer: [0, 2, 3]
explain: |
  `DateTime`, `int` и `Guid` — структуры. `string` и массивы — ссылочные типы.
```

```quiz
id: test-struct-param
type: output
part: "Объявление и отличия от класса"
prompt: Что выведет код?
code: |
  var size = new Size { Width = 10 };
  var copy = size;
  copy.Width = 20;
  Console.WriteLine(size.Width + copy.Width);

  struct Size
  {
      public int Width;
  }
answer: "30"
explain: |
  `copy` — отдельная копия: `size.Width` остался 10, `copy.Width` стал 20.
```

```quiz
id: test-struct-default
type: choice
part: "Конструкторы и default"
prompt: Чем заполнена структура `Point p = default;`?
options:
  - "Все поля имеют значения по умолчанию: нули, false, null"
  - "Вызывается конструктор без параметров"
  - "p равна null"
  - "Ошибка компиляции"
answer: 0
explain: |
  `default` для структуры — обнулённая память, никакой конструктор не вызывается.
```

```quiz
id: test-struct-array-ctor
type: choice
part: "Конструкторы и default"
prompt: Вызывается ли объявленный конструктор без параметров для элементов массива `new Settings[10]`, где `Settings` — структура?
options:
  - "Нет, элементы просто обнулены"
  - "Да, для каждого из десяти элементов"
  - "Только для первого элемента"
  - "Массив структур создать нельзя"
answer: 0
explain: |
  Массив выделяется обнулённым блоком памяти. Конструктор вызывается только при явном `new Settings()`.
```

```quiz
id: test-struct-new-zero
type: output
part: "Конструкторы и default"
prompt: Что выведет код?
code: |
  var p = new Point();
  Console.WriteLine(p.X + p.Y);

  struct Point
  {
      public int X;
      public int Y;
      public Point(int x, int y) { X = x; Y = y; }
  }
answer: "0"
explain: |
  Своего конструктора без параметров нет, поэтому `new Point()` даёт обнулённую структуру, даже если есть конструктор с параметрами.
```

```quiz
id: test-struct-mutate-return
type: output
part: "Изменяемые структуры и копии"
prompt: Что выведет код?
code: |
  var p = new Counter();
  p.Add();
  var q = p;
  q.Add();
  Console.WriteLine(p.Value);

  struct Counter
  {
      public int Value;
      public void Add() => Value++;
  }
answer: "1"
explain: |
  `p.Add()` увеличил `p` до 1. `q` — копия, её `Add` меняет только `q`.
```

```quiz
id: test-readonly-struct
type: choice
part: "Изменяемые структуры и копии"
prompt: Что гарантирует `readonly struct`?
options:
  - "Все поля структуры только для чтения, компилятор это проверяет"
  - "Структура хранится в куче"
  - "Структуру нельзя копировать"
  - "Структура не может иметь методов"
answer: 0
explain: |
  Неизменяемость проверяется при компиляции: присвоить поле вне конструктора нельзя.
```

```quiz
id: test-struct-array-element
type: output
part: "Изменяемые структуры и копии"
prompt: Что выведет код?
code: |
  var points = new Point[2];
  var first = points[0];
  first.X = 9;
  Console.WriteLine(points[0].X);

  struct Point
  {
      public int X;
  }
answer: "0"
explain: |
  `first` — копия элемента. Изменение копии не трогает массив. Напрямую — `points[0].X = 9`.
```

```quiz
id: test-struct-size
type: choice
part: "Когда выбирать структуру"
prompt: Какое ориентировочное ограничение по размеру советуют для структур?
options:
  - "До 16 байт"
  - "До 1 КБ"
  - "До 85 000 байт"
  - "Размер не важен"
answer: 0
explain: |
  Структура копируется целиком. Пока она размером с пару чисел, копировать её дешевле, чем работать через ссылку.
```

```quiz
id: test-struct-identity
type: choice
part: "Когда выбирать структуру"
prompt: Почему «пользователя» делают классом, а не структурой?
options:
  - "У пользователя есть идентичность, он большой и изменяется"
  - "Структуры не могут хранить строки"
  - "Классы быстрее структур всегда"
  - "Структуры нельзя передавать в методы"
answer: 0
explain: |
  Два пользователя с одинаковыми полями — разные люди. Копировать такие данные по значению бессмысленно.
```

```quiz
id: test-struct-boxing
type: choice
part: "Когда выбирать структуру"
prompt: Почему структура теряет выгоду, если её часто присваивают переменной типа `object`?
options:
  - "Каждый раз происходит упаковка: выделение объекта в куче"
  - "object не может хранить структуры"
  - "Структура становится классом навсегда"
  - "Выгода не теряется"
answer: 0
explain: |
  Упаковка — та самая аллокация в куче, которой структура должна была избежать.
```

```quiz
id: test-ref-struct
type: choice
part: "Когда выбирать структуру"
level: middle
prompt: Что нельзя сделать с ref struct, например со Span<T>?
options:
  - "Положить в поле класса или упаковать"
  - "Передать в метод"
  - "Создать локальную переменную"
  - "Прочитать элементы"
answer: 0
explain: |
  `ref struct` живёт только на стеке. Любое место, откуда он мог бы попасть в кучу, запрещено компилятором.
```

::::
