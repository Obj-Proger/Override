---
id: csharp.oop.properties
title: Свойства и индексаторы
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 2
requires: [csharp.oop.classes]
quickCheck: [computed-property, private-set, init-only, validation-setter]
testMinutes: 6
---

::::warmup

```quiz
id: computed-property
type: output
prompt: Что выведет код?
code: |
  var box = new Box { Width = 3, Height = 4 };
  box.Width = 5;
  Console.WriteLine(box.Area);

  class Box
  {
      public int Width { get; set; }
      public int Height { get; set; }
      public int Area => Width * Height;
  }
answer: "20"
explain: |
  `Area` не хранит значение, а вычисляет его при каждом чтении из текущих `Width` и `Height`: 5 × 4 = 20.
variants:
  - code: |
      var box = new Box { Width = 3, Height = 4 };
      int area = box.Area;
      box.Width = 5;
      Console.WriteLine(area);

      class Box
      {
          public int Width { get; set; }
          public int Height { get; set; }
          public int Area => Width * Height;
      }
    answer: "12"
    explain: |
      Значение `Area` прочитали в переменную до изменения ширины: 3 × 4 = 12. Переменная хранит число, а не формулу.
```

::::

::::part Поля и свойства

**Свойство** выглядит снаружи как поле, но внутри это пара методов: `get` возвращает значение, `set` его принимает. Внутри `set` новое значение доступно как `value`.

```csharp
class User
{
    private int _age;  // поле, где лежит значение

    public int Age  // свойство
    {
        get { return _age; }
        set { _age = value; }
    }
}

var user = new User();
user.Age = 30;                // вызывается set, value = 30
Console.WriteLine(user.Age);  // вызывается get
```

**Автосвойство** — то же самое без ручного поля: компилятор создаёт скрытое поле сам.

```csharp
public int Age { get; set; }
```

**Вычисляемое свойство** ничего не хранит, а считает значение при каждом чтении:

```csharp
public string FullName => $"{FirstName} {LastName}";
```

**Зачем свойства, если есть поля.** Публичные поля в C# почти не используют, наружу открывают свойства:

- в `set` можно добавить проверку, не меняя код, который пользуется свойством;
- доступ на чтение и запись можно разделить: читать всем, менять — только самому классу;
- свойство может быть вычисляемым, а для вызывающего кода ничего не меняется;
- свойства поддерживают привязка данных, сериализация в JSON, ORM и интерфейсы — поля нет.

Имена свойств пишут в PascalCase, приватных полей — с подчёркиванием: `_age`.

```quiz
id: property-is-method
type: choice
prompt: Что на самом деле происходит при записи `user.Age = 30`, если `Age` — свойство?
options:
  - "Вызывается метод set, параметр value равен 30"
  - "Значение записывается напрямую в память объекта, как в поле"
  - "Создаётся новый объект User"
  - "Вызывается конструктор"
answer: 0
explain: |
  Свойство компилируется в методы `get_Age` и `set_Age`. Запись вызывает `set`, чтение — `get`.
```

::::

::::part Доступ к свойству: private set, init, required

У `get` и `set` можно задать разный доступ, а `set` заменить на более строгий вариант:

```csharp
class Order
{
    public int Id { get; }                         // только чтение: присвоить можно лишь в конструкторе
    public string Status { get; private set; }     // читают все, меняет только сам класс
    public DateTime Created { get; init; }         // присвоить можно в конструкторе или инициализаторе объекта
    public required string Customer { get; set; }  // обязано быть задано при создании

    public Order(int id)
    {
        Id = id;
        Status = "новый";
    }

    public void Pay() => Status = "оплачен";
}

var order = new Order(1) { Customer = "Анна", Created = DateTime.Now };
order.Status = "x";            // ошибка: set приватный
order.Created = DateTime.Now;  // ошибка: init только при создании
order.Pay();                   // ок: класс сам меняет статус
```

| Запись | Кто может присвоить |
|---|---|
| `{ get; set; }` | кто угодно, когда угодно |
| `{ get; private set; }` | только код этого класса |
| `{ get; }` | только конструктор (и инициализатор свойства) |
| `{ get; init; }` | конструктор и инициализатор объекта, потом — никто |
| `required` | обязательно задать в инициализаторе объекта при создании |

`init` (C# 9) удобен для неизменяемых объектов: значения задаются при создании через `{ ... }`, а дальше их не поменять. `required` (C# 11) заставляет вызывающий код не забыть важное свойство: без него `new Order(1)` не скомпилируется.

```quiz
id: private-set
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var order = new Order();
  order.Pay();
  order.Status = "отменён";
  Console.WriteLine(order.Status);

  class Order
  {
      public string Status { get; private set; } = "новый";
      public void Pay() => Status = "оплачен";
  }
answer: 3
explain: |
  `set` у `Status` приватный: изменить свойство может только код класса `Order`, например метод `Pay`. Снаружи — только читать.
variants:
  - code: |
      var order = new Order(7);
      Console.WriteLine(order.Id);
      order.Id = 8;

      class Order
      {
          public int Id { get; }
          public Order(int id) => Id = id;
      }
    answer: 3
    explain: |
      У `Id` есть только `get`: присвоить значение можно лишь в конструкторе.
```

```quiz
id: init-only
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var point = new Point { X = 1, Y = 2 };
  Console.WriteLine(point.X);
  point.X = 10;

  class Point
  {
      public int X { get; init; }
      public int Y { get; init; }
  }
answer: 3
explain: |
  `init` разрешает присвоить значение только при создании объекта — в конструкторе или в фигурных скобках инициализатора. Потом свойство только для чтения.
```

::::

::::part Проверка значений и ключевое слово field

Главная причина писать свойство «вручную» — проверка в `set`:

```csharp
class Product
{
    private decimal _price;

    public decimal Price
    {
        get => _price;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Цена не может быть отрицательной.");
            _price = value;
        }
    }
}
```

`throw` бросает исключение — подробно о них в отдельной теме. `nameof(value)` возвращает имя `"value"` строкой, чтобы его не писать вручную.

**`field`** (C# 14) убирает необходимость объявлять поле: внутри аксессоров `field` — скрытое поле, которое создаёт компилятор, как у автосвойства.

```csharp
public decimal Price
{
    get;
    set => field = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
```

Ещё один частый приём — нормализация: `set => field = value.Trim();` хранит строку уже без пробелов по краям.

```quiz
id: validation-setter
type: output
prompt: Что выведет код?
code: |
  var t = new Thermostat();
  t.Target = 35;
  Console.WriteLine(t.Target);

  class Thermostat
  {
      private int _target = 20;

      public int Target
      {
          get => _target;
          set => _target = Math.Clamp(value, 10, 30);
      }
  }
answer: "30"
explain: |
  `set` не хранит значение как есть: `Math.Clamp` ограничивает его диапазоном от 10 до 30. 35 превращается в 30.
variants:
  - code: |
      var t = new Thermostat();
      t.Target = 5;
      Console.WriteLine(t.Target);

      class Thermostat
      {
          private int _target = 20;

          public int Target
          {
              get => _target;
              set => _target = Math.Clamp(value, 10, 30);
          }
      }
    answer: "10"
    explain: |
      5 меньше нижней границы, и `Math.Clamp` поднимает его до 10.
  - code: |
      var t = new Thermostat();
      Console.WriteLine(t.Target);

      class Thermostat
      {
          private int _target = 20;

          public int Target
          {
              get => _target;
              set => _target = Math.Clamp(value, 10, 30);
          }
      }
    answer: "20"
    explain: |
      Свойство ещё не меняли, и `get` возвращает начальное значение поля.
```

::::

::::part Индексаторы

**Индексатор** позволяет обращаться к объекту по индексу, как к массиву: `obj[i]`. Объявляется как свойство с именем `this` и параметром:

```csharp
class Week
{
    private readonly string[] _days = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };

    public string this[int index]
    {
        get => _days[index];
        set => _days[index] = value;
    }
}

var week = new Week();
Console.WriteLine(week[0]);  // Пн
```

Параметр не обязан быть числом: у словаря индексатор принимает ключ, `dictionary["ключ"]`. Индексаторов с разными параметрами в классе может быть несколько. Индексатор может быть только для чтения — без `set`, как у строки: `text[0]` читать можно, писать нельзя.

```quiz
id: indexer-output
type: output
prompt: Что выведет код?
code: |
  var grid = new Grid();
  grid[1, 2] = 7;
  Console.WriteLine(grid[1, 2] + grid[0, 0]);

  class Grid
  {
      private readonly int[,] _cells = new int[3, 3];

      public int this[int row, int column]
      {
          get => _cells[row, column];
          set => _cells[row, column] = value;
      }
  }
answer: "7"
explain: |
  Индексатор может принимать несколько параметров. Ячейка `[1, 2]` стала 7, а `[0, 0]` осталась 0.
```

:::level middle

**Свойство — это методы.** В IL `Age` превращается в `get_Age()` и `set_Age(int)`. Поэтому свойство можно объявить в интерфейсе, сделать виртуальным и переопределить, а поле — нельзя. По той же причине замена публичного поля на свойство ломает совместимость уже собранных сборок: меняется способ доступа.

**Свойство, возвращающее структуру.** Если свойство возвращает значимый тип, `get` отдаёт копию. Запись `shape.Position.X = 5`, где `Position` — свойство-структура, не компилируется (CS1612): изменилась бы копия, которая сразу пропадёт. Подробнее — в теме про структуры.

**Дорогой get.** От свойства ждут, что чтение быстрое и без побочных эффектов. Если значение долго вычисляется или идёт в базу, это должен быть метод (`GetTotal()`), а не свойство.

```quiz
id: property-in-interface
type: choice
level: middle
prompt: Почему в интерфейсе можно объявить свойство, но нельзя объявить поле?
options:
  - "Свойство — это пара методов, а интерфейс описывает методы, а не хранение данных"
  - "Поля запрещены в C# вообще"
  - "Свойства хранятся в стеке"
  - "Интерфейсы не поддерживают свойства"
answer: 0
explain: |
  Интерфейс — контракт поведения. Свойство компилируется в методы `get_`/`set_`, поэтому его можно требовать от класса. Как класс хранит значение — его дело.
```

:::

::::

::::practice

```quiz
id: temperature-task
type: code
lang: csharp
prompt: |
  Допишите класс: `Celsius` — обычное автосвойство, `Fahrenheit` — вычисляемое свойство только для чтения по формуле F = C × 9 / 5 + 32.
starter: |
  public class Temperature
  {
      public double Celsius { get; set; }

      public double Fahrenheit
      {
          get
          {
          }
      }
  }
tests: |
  var t = new Temperature { Celsius = 100 };
  Assert.Equal(212.0, t.Fahrenheit);
  t.Celsius = 0;
  Assert.Equal(32.0, t.Fahrenheit);
  t.Celsius = -40;
  Assert.Equal(-40.0, t.Fahrenheit);
solution: |
  public class Temperature
  {
      public double Celsius { get; set; }

      public double Fahrenheit => Celsius * 9 / 5 + 32;
  }
explain: |
  Вычисляемое свойство пересчитывается при каждом чтении, поэтому после смены `Celsius` `Fahrenheit` сразу верный. Хранить два значения отдельно не нужно.
variants:
  - prompt: |
      Допишите класс: свойство `Name` при записи убирает пробелы по краям, а если получилась пустая строка — сохраняет «Без имени».
    starter: |
      public class Profile
      {
          private string _name = "Без имени";

          public string Name
          {
              get => _name;
              set
              {
              }
          }
      }
    tests: |
      var p = new Profile();
      p.Name = "  Анна ";
      Assert.Equal("Анна", p.Name);
      p.Name = "   ";
      Assert.Equal("Без имени", p.Name);
    solution: |
      public class Profile
      {
          private string _name = "Без имени";

          public string Name
          {
              get => _name;
              set
              {
                  var trimmed = value.Trim();
                  _name = trimmed.Length == 0 ? "Без имени" : trimmed;
              }
          }
      }
    explain: |
      `set` — место, где значение приводят к нужному виду до сохранения. Код, который пользуется свойством, об этом ничего не знает.
```

```quiz
id: auto-property-fill
type: fill
prompt: Вставьте аксессоры, чтобы свойство читали все, а меняли только внутри класса.
code: |
  public decimal Balance { ___ }
answer: get; private set;
explain: |
  `get` открыт, `private set` разрешает запись только коду класса.
```

::::

::::traps

- **Свойство — это методы**, хоть и выглядит как поле. В `set` может быть любая логика.
- **Вычисляемое свойство не хранит значение:** оно пересчитывается при каждом чтении.
- **`{ get; }` можно присвоить только в конструкторе**, `init` — ещё и в инициализаторе объекта.
- **`required` без значения не скомпилируется.**
- **Свойство-структура отдаёт копию:** `obj.Position.X = 5` не скомпилируется.
- **Наружу открывают свойства, а не поля.**

::::

::::summary

```quiz
id: properties-recall
type: recall
prompt: Чем свойство отличается от поля и какие виды свойств бывают?
answer: |
  **Свойство** — пара методов `get`/`set`, которая снаружи выглядит как поле; в `set` новое значение — `value`. Это позволяет проверять и нормализовать значения, разделять доступ на чтение и запись, вычислять значение на лету и объявлять свойства в интерфейсах. Наружу открывают свойства, поля делают приватными.
  **Автосвойство** `{ get; set; }` создаёт скрытое поле само. **Вычисляемое** `=> выражение` ничего не хранит. `{ get; private set; }` — меняет только класс, `{ get; }` — только конструктор, `{ get; init; }` — ещё и инициализатор объекта, `required` — обязательно при создании. `field` (C# 14) — скрытое поле внутри аксессоров.
  **Индексатор** `this[...]` даёт доступ по индексу или ключу, как у массива и словаря.
```

::::

::::test

```quiz
id: test-auto-property
type: choice
part: "Поля и свойства"
prompt: Что создаёт компилятор для автосвойства `public int Age { get; set; }`?
options:
  - "Скрытое поле и методы get и set для него"
  - "Только публичное поле Age"
  - "Только метод get"
  - "Ничего, это просто поле"
answer: 0
explain: |
  Автосвойство — сокращение: поле создаётся автоматически, а аксессоры просто читают и пишут его.
```

```quiz
id: test-computed
type: output
part: "Поля и свойства"
prompt: Что выведет код?
code: |
  var person = new Person { First = "Анна", Last = "Иванова" };
  person.Last = "Петрова";
  Console.WriteLine(person.Full);

  class Person
  {
      public string First { get; set; } = "";
      public string Last { get; set; } = "";
      public string Full => $"{First} {Last}";
  }
answer: Анна Петрова
explain: |
  Вычисляемое свойство собирает строку при каждом чтении, поэтому видит новую фамилию.
```

```quiz
id: test-why-properties
type: choice
part: "Поля и свойства"
prompt: Почему в публичном API класса используют свойства, а не публичные поля?
options:
  - "В свойство можно добавить проверку и разграничить доступ, не меняя код, который им пользуется"
  - "Свойства работают быстрее полей"
  - "Поля нельзя читать снаружи"
  - "Поля не могут хранить строки"
answer: 0
explain: |
  Свойство — точка контроля: можно проверить значение, сделать запись приватной, вычислять значение. С полем этого не сделать без изменения контракта.
```

```quiz
id: test-get-only
type: choice
part: "Доступ к свойству: private set, init, required"
prompt: Где можно присвоить значение свойству `public int Id { get; }`?
options:
  - "Только в конструкторе или в инициализаторе самого свойства"
  - "Где угодно внутри класса"
  - "В инициализаторе объекта new X { Id = 1 }"
  - "Нигде"
answer: 0
explain: |
  Свойство только для чтения задаётся при создании объекта, внутри конструктора. Для инициализатора объекта нужен `init`.
```

```quiz
id: test-init
type: choice
part: "Доступ к свойству: private set, init, required"
prompt: Чем `{ get; init; }` отличается от `{ get; set; }`?
options:
  - "init разрешает присвоить значение только при создании объекта"
  - "init делает свойство приватным"
  - "init разрешает только одно чтение"
  - "Ничем"
answer: 0
explain: |
  После создания объекта свойство с `init` больше не меняется. Удобно для неизменяемых объектов.
```

```quiz
id: test-required
type: find-bug
part: "Доступ к свойству: private set, init, required"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var user = new User();
  Console.WriteLine(user.Email);

  class User
  {
      public required string Email { get; init; }
  }
answer: 1
explain: |
  `required` требует задать свойство при создании: `new User { Email = "..." }`. Без этого объект не создать.
```

```quiz
id: test-private-set-inside
type: output
part: "Доступ к свойству: private set, init, required"
prompt: Что выведет код?
code: |
  var c = new Counter();
  c.Add();
  c.Add();
  Console.WriteLine(c.Value);

  class Counter
  {
      public int Value { get; private set; }
      public void Add() => Value++;
  }
answer: "2"
explain: |
  Приватный `set` доступен методам самого класса, поэтому `Add` может менять `Value`.
```

```quiz
id: test-setter-value
type: choice
part: "Проверка значений и ключевое слово field"
prompt: Как внутри `set` получить присваиваемое значение?
options:
  - "Через ключевое слово value"
  - "Через параметр newValue"
  - "Через this"
  - "Через field"
answer: 0
explain: |
  `value` — неявный параметр аксессора `set`. `field` — скрытое поле свойства, а не новое значение.
```

```quiz
id: test-field-keyword
type: choice
part: "Проверка значений и ключевое слово field"
prompt: Что означает `field` внутри аксессора свойства в C# 14?
options:
  - "Скрытое поле, которое компилятор создаёт для свойства"
  - "Новое присваиваемое значение"
  - "Любое поле класса"
  - "Ссылку на объект"
answer: 0
explain: |
  `field` позволяет добавить логику в `set`, не объявляя отдельное поле вручную.
```

```quiz
id: test-trim-setter
type: output
part: "Проверка значений и ключевое слово field"
prompt: Что выведет код?
code: |
  var tag = new Tag();
  tag.Name = "  csharp  ";
  Console.WriteLine(tag.Name.Length);

  class Tag
  {
      private string _name = "";

      public string Name
      {
          get => _name;
          set => _name = value.Trim();
      }
  }
answer: "6"
explain: |
  `set` сохраняет строку без пробелов: `csharp` — 6 символов.
```

```quiz
id: test-indexer-declare
type: choice
part: "Индексаторы"
prompt: Как объявляется индексатор?
options:
  - "Как свойство с именем this и параметром в квадратных скобках"
  - "Как метод Index"
  - "Как массив внутри класса"
  - "Через атрибут [Indexer]"
answer: 0
explain: |
  `public string this[int index] { get; set; }` — индексатор. Параметров может быть несколько, и они не обязаны быть числами.
```

```quiz
id: test-indexer-key
type: output
part: "Индексаторы"
prompt: Что выведет код?
code: |
  var colors = new Palette();
  Console.WriteLine(colors["red"]);

  class Palette
  {
      public string this[string name] => name == "red" ? "#FF0000" : "#000000";
  }
answer: "#FF0000"
explain: |
  Индексатор принимает строку-ключ. Без `set` он только для чтения.
```

```quiz
id: test-struct-property
type: choice
part: "Индексаторы"
level: middle
prompt: Почему `shape.Position.X = 5` не компилируется, если `Position` — свойство структурного типа?
options:
  - "get возвращает копию структуры, и изменение копии сразу пропало бы"
  - "У структур нельзя менять поля"
  - "Свойства нельзя использовать в выражениях"
  - "X — приватное поле"
answer: 0
explain: |
  Компилятор запрещает изменять временную копию (CS1612). Нужно взять копию, изменить и присвоить обратно: `var p = shape.Position; p.X = 5; shape.Position = p;`.
```

::::
