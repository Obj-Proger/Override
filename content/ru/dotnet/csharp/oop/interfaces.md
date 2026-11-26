---
id: csharp.oop.interfaces
title: Интерфейсы и абстрактные классы
level: junior
tracks: [backend, fullstack]
minutes: 30
order: 8
requires: [csharp.oop.polymorphism]
quickCheck: [interface-variable, interface-new, explicit-impl, struct-interface-copy]
testMinutes: 6
---

::::warmup

```quiz
id: interface-variable
type: output
prompt: Что выведет код?
code: |
  INotifier notifier = new SmsNotifier();
  Console.WriteLine(notifier.Send("привет"));

  interface INotifier
  {
      string Send(string text);
  }

  class SmsNotifier : INotifier
  {
      public string Send(string text) => $"SMS: {text}";
  }
answer: "SMS: привет"
explain: |
  Переменная интерфейсного типа может хранить любой объект, который реализует интерфейс. Вызов уходит в реализацию настоящего объекта.
variants:
  - code: |
      INotifier notifier = new EmailNotifier();
      Console.WriteLine(notifier.Send("привет"));

      interface INotifier
      {
          string Send(string text);
      }

      class SmsNotifier : INotifier
      {
          public string Send(string text) => $"SMS: {text}";
      }

      class EmailNotifier : INotifier
      {
          public string Send(string text) => $"Email: {text}";
      }
    answer: "Email: привет"
    explain: |
      Код работает с `INotifier` и не знает конкретный класс. Подставили другую реализацию — поведение сменилось без изменения вызывающего кода.
```

::::

::::part Интерфейс — это контракт

**Интерфейс** описывает, **что** объект умеет, но не **как**. В нём перечислены методы, свойства, индексаторы и события без реализации. Класс, реализующий интерфейс, обязан дать реализацию всех его членов.

```csharp
interface IPaymentGateway
{
    bool Charge(decimal amount);
    string Name { get; }
}

class CardGateway : IPaymentGateway
{
    public string Name => "Карта";
    public bool Charge(decimal amount) => amount <= 10_000;
}

class SbpGateway : IPaymentGateway
{
    public string Name => "СБП";
    public bool Charge(decimal amount) => true;
}
```

- Имена интерфейсов по соглашению начинаются с `I`: `IDisposable`, `IComparable`, `IEnumerable`.
- Члены интерфейса публичные; реализация в классе тоже должна быть `public`.
- Класс может реализовать **сколько угодно** интерфейсов: `class Order : IComparable, IDisposable`. Базовый класс при этом пишется первым.
- Объект интерфейса создать нельзя, но переменная интерфейсного типа может ссылаться на любой реализующий объект.

**Зачем.** Код, который зависит от интерфейса, не зависит от конкретной реализации:

```csharp
class Checkout(IPaymentGateway gateway)
{
    public string Pay(decimal sum) => gateway.Charge(sum) ? $"Оплачено: {gateway.Name}" : "Отказ";
}
```

`Checkout` работает с картой, СБП и тестовой заглушкой одинаково. Подменить реализацию можно, не трогая `Checkout`, — на этом построены тестирование и внедрение зависимостей (DI) в ASP.NET Core.

```quiz
id: interface-new
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  IShape shape = new IShape();
  Console.WriteLine(shape.Area());

  interface IShape
  {
      double Area();
  }
answer: 1
explain: |
  Интерфейс — только контракт без реализации, его объект создать нельзя. Создают объект класса, который реализует интерфейс.
variants:
  - code: |
      IShape shape = new Square();
      Console.WriteLine(shape.Area());

      interface IShape
      {
          double Area();
          string Name { get; }
      }

      class Square : IShape
      {
          public double Area() => 4;
      }
    answer: 10
    explain: |
      `Square` объявил, что реализует `IShape`, но не реализовал свойство `Name`. Класс обязан реализовать все члены интерфейса.
```

::::

::::part Интерфейс или абстрактный класс

Оба описывают общее поведение для разных типов. Различия:

| | Интерфейс | Абстрактный класс |
|---|---|---|
| Сколько можно унаследовать | сколько угодно | один |
| Поля (состояние) | нет | есть |
| Конструкторы | нет | есть |
| Реализация методов | только методы по умолчанию (C# 8) | любые обычные методы |
| Модификаторы членов | публичные | любые |
| Подходит структурам | да | нет |
| Отношение | «умеет» (`IComparable` — умеет сравниваться) | «является» (`Circle` — это `Shape`) |

Практическое правило:

- **интерфейс** — когда нужно описать возможность, которую получают несвязанные типы, или когда нужна подменяемая зависимость: `ILogger`, `IRepository`, `IDisposable`;
- **абстрактный класс** — когда у семейства типов есть общее состояние и общий код, а различается часть шагов: `Stream` в .NET — абстрактный класс с общей логикой буферизации.

Часто их сочетают: интерфейс — контракт, абстрактный класс — удобная базовая реализация этого контракта.

**Методы по умолчанию** (C# 8). Интерфейс может дать реализацию члена. Так библиотека добавляет метод в интерфейс, не ломая все существующие реализации:

```csharp
interface ILogger
{
    void Log(string message);
    void Warn(string message) => Log("ВНИМАНИЕ: " + message);  // реализация по умолчанию
}
```

Метод по умолчанию доступен только через переменную интерфейсного типа, а не через переменную класса.

```quiz
id: interface-or-abstract
type: choice
prompt: Нужно, чтобы и класс `Order`, и структура `Money` умели сравниваться между собой. Что подходит?
options:
  - "Интерфейс, например IComparable"
  - "Абстрактный класс Comparable"
  - "Общий базовый класс object"
  - "Статический класс с методом Compare"
answer: 0
explain: |
  Структура не может наследовать класс, а `Order` уже может иметь свой базовый класс. Интерфейс подходит обоим и описывает именно возможность «умеет сравниваться».
```

::::

::::part Явная реализация

Если два интерфейса требуют член с одним именем, или член не должен быть виден в публичном API класса, его реализуют **явно** — с именем интерфейса:

```csharp
interface IFile { string Save(); }
interface IDatabase { string Save(); }

class Document : IFile, IDatabase
{
    string IFile.Save() => "в файл";  // явная реализация: без модификатора доступа
    string IDatabase.Save() => "в базу";
}

var doc = new Document();
// doc.Save();                              // ошибка: у самого класса такого метода нет
Console.WriteLine(((IFile)doc).Save());  // в файл
IDatabase db = doc;
Console.WriteLine(db.Save());            // в базу
```

Явно реализованный член вызывается **только через переменную интерфейсного типа**. Так в .NET скрыты, например, необобщённые члены коллекций, которые устарели, но нужны для совместимости.

```quiz
id: explicit-impl
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var printer = new Printer();
  IPrintable printable = printer;
  Console.WriteLine(printable.Print());
  Console.WriteLine(printer.Print());

  interface IPrintable
  {
      string Print();
  }

  class Printer : IPrintable
  {
      string IPrintable.Print() => "печать";
  }
answer: 4
explain: |
  `Print` реализован явно, поэтому у класса `Printer` такого публичного метода нет. Вызвать его можно только через переменную типа `IPrintable`, как в строке 3.
```

::::

::::part Структуры и интерфейсы: скрытая упаковка

Структура может реализовывать интерфейсы. Но интерфейс — ссылочный тип, поэтому при присваивании структуры переменной интерфейсного типа происходит **упаковка**: создаётся копия в куче. Из-за этого изменение через интерфейс меняет **копию**:

```csharp
var counter = new Counter();
ICounter boxed = counter;  // упаковка: в куче копия counter
boxed.Increment();         // меняется копия

Console.WriteLine(counter.Value);           // 0
Console.WriteLine(((Counter)boxed).Value);  // 1

interface ICounter
{
    void Increment();
}

struct Counter : ICounter
{
    public int Value;
    public void Increment() => Value++;
}
```

Это ещё одна причина делать структуры неизменяемыми. А в коде, где важна скорость, структуры не передают через интерфейсный тип — каждая такая передача выделяет память. Обобщённые методы (`void Use<T>(T item) where T : ICounter`) работают со структурой без упаковки — о них в теме про generics.

```quiz
id: struct-interface-copy
type: output
prompt: Что выведет код?
code: |
  var counter = new Counter();
  ICounter boxed = counter;
  boxed.Increment();
  Console.WriteLine(counter.Value);

  interface ICounter { void Increment(); }

  struct Counter : ICounter
  {
      public int Value;
      public void Increment() => Value++;
  }
answer: "0"
explain: |
  `ICounter boxed = counter` упаковывает структуру: `boxed` ссылается на копию в куче. `Increment` меняет копию, `counter` остаётся прежним.
variants:
  - code: |
      var counter = new Counter();
      ICounter boxed = counter;
      boxed.Increment();
      Console.WriteLine(counter.Value);

      interface ICounter { void Increment(); }

      class Counter : ICounter
      {
          public int Value;
          public void Increment() => Value++;
      }
    answer: "1"
    explain: |
      Здесь `Counter` — класс. Присваивание копирует ссылку на тот же объект, упаковки нет, и изменение видно через `counter`.
  - code: |
      var counter = new Counter();
      ICounter boxed = counter;
      boxed.Increment();
      boxed.Increment();
      Console.WriteLine(((Counter)boxed).Value);

      interface ICounter { void Increment(); }

      struct Counter : ICounter
      {
          public int Value;
          public void Increment() => Value++;
      }
    answer: "2"
    explain: |
      Оба вызова меняют одну и ту же упакованную копию. Распаковка показывает её значение.
```

:::level middle

**Статические абстрактные члены** (C# 11). Интерфейс может требовать статический метод или оператор: `static abstract T operator +(T a, T b);`. На этом построена обобщённая математика .NET 7: `INumber<T>` позволяет написать `Sum<T>(T[] items) where T : INumber<T>`, который работает и с `int`, и с `decimal`.

**Стоимость вызова через интерфейс.** Вызов метода интерфейса чуть дороже виртуального: среде нужно найти реализацию интерфейса в типе. Современный JIT кэширует такие вызовы и девиртуализирует их, если видит точный тип. Для бизнес-кода разница незаметна.

**Интерфейс-маркер** — пустой интерфейс, который только помечает тип. Сейчас для этого чаще используют атрибуты.

```quiz
id: static-abstract
type: choice
level: middle
prompt: Что позволяют статические абстрактные члены интерфейсов (C# 11)?
options:
  - "Требовать от типа статические методы и операторы, например для обобщённой математики"
  - "Создавать объекты интерфейса"
  - "Объявлять поля в интерфейсе"
  - "Наследовать интерфейс от класса"
answer: 0
explain: |
  Благодаря им интерфейсы вроде `INumber<T>` требуют операторы `+`, `*` и т. п., и обобщённый код может считать над любым числовым типом.
```

:::

::::

::::practice

```quiz
id: interface-task
type: code
lang: csharp
prompt: |
  Реализуйте интерфейс `IDiscount` в двух классах: `PercentDiscount` уменьшает цену на заданный процент, `FixedDiscount` — на фиксированную сумму, но не ниже нуля.
starter: |
  public interface IDiscount
  {
      decimal Apply(decimal price);
  }

  public class PercentDiscount : IDiscount
  {
      private readonly decimal _percent;

      public PercentDiscount(decimal percent) => _percent = percent;

      public decimal Apply(decimal price)
      {
      }
  }

  public class FixedDiscount : IDiscount
  {
      private readonly decimal _amount;

      public FixedDiscount(decimal amount) => _amount = amount;

      public decimal Apply(decimal price)
      {
      }
  }
tests: |
  IDiscount percent = new PercentDiscount(10m);
  Assert.Equal(900m, percent.Apply(1000m));
  IDiscount fixedOne = new FixedDiscount(300m);
  Assert.Equal(700m, fixedOne.Apply(1000m));
  Assert.Equal(0m, fixedOne.Apply(200m));
solution: |
  public interface IDiscount
  {
      decimal Apply(decimal price);
  }

  public class PercentDiscount : IDiscount
  {
      private readonly decimal _percent;

      public PercentDiscount(decimal percent) => _percent = percent;

      public decimal Apply(decimal price) => price - price * _percent / 100;
  }

  public class FixedDiscount : IDiscount
  {
      private readonly decimal _amount;

      public FixedDiscount(decimal amount) => _amount = amount;

      public decimal Apply(decimal price) => Math.Max(0, price - _amount);
  }
explain: |
  Тесты работают с `IDiscount` и не знают, какая скидка внутри. Так в приложении можно хранить список скидок и применять их одинаково.
variants:
  - prompt: |
      Реализуйте интерфейс `IFormatter` в двух классах: `UpperFormatter` возвращает текст в верхнем регистре, `BracketFormatter` — текст в квадратных скобках.
    starter: |
      public interface IFormatter
      {
          string Format(string text);
      }

      public class UpperFormatter : IFormatter
      {
          public string Format(string text)
          {
          }
      }

      public class BracketFormatter : IFormatter
      {
          public string Format(string text)
          {
          }
      }
    tests: |
      IFormatter upper = new UpperFormatter();
      IFormatter bracket = new BracketFormatter();
      Assert.Equal("КОТ", upper.Format("кот"));
      Assert.Equal("[кот]", bracket.Format("кот"));
      Assert.Equal("[КОТ]", bracket.Format(upper.Format("кот")));
    solution: |
      public interface IFormatter
      {
          string Format(string text);
      }

      public class UpperFormatter : IFormatter
      {
          public string Format(string text) => text.ToUpper();
      }

      public class BracketFormatter : IFormatter
      {
          public string Format(string text) => $"[{text}]";
      }
    explain: |
      Обе реализации взаимозаменяемы для кода, который знает только `IFormatter`, и их можно комбинировать.
```

```quiz
id: implements-fill
type: fill
prompt: Вставьте объявление, чтобы класс `Report` наследовал `Document` и реализовывал `IPrintable`.
code: |
  class Report ___
  {
  }
answer: [": Document, IPrintable", ":Document, IPrintable", ": Document,IPrintable"]
explain: |
  После двоеточия первым идёт базовый класс, затем через запятую интерфейсы.
```

::::

::::traps

- **Объект интерфейса создать нельзя**, но переменная интерфейсного типа хранит любой реализующий объект.
- **Класс обязан реализовать все члены интерфейса**, кроме тех, у которых есть реализация по умолчанию.
- **Явно реализованный член вызывается только через интерфейс.**
- **Метод по умолчанию виден только через переменную интерфейсного типа.**
- **Структура в переменной интерфейса — это упаковка:** изменения уходят в копию.
- **Базовый класс в списке наследования идёт первым**, интерфейсы — после.

::::

::::summary

```quiz
id: interfaces-recall
type: recall
prompt: Что такое интерфейс, чем он отличается от абстрактного класса и что такое явная реализация?
answer: |
  **Интерфейс** — контракт: перечень методов, свойств, индексаторов и событий без реализации. Класс или структура реализует любое число интерфейсов и обязан дать реализацию всех членов. Переменная интерфейсного типа хранит любой реализующий объект — так код зависит от абстракции, а реализацию можно подменить (тесты, DI).
  **Отличия от абстрактного класса**: интерфейсов — сколько угодно, абстрактный класс — один; у интерфейса нет полей и конструкторов, члены публичные, подходит структурам. Интерфейс — «умеет», абстрактный класс — «является» с общим состоянием и кодом. С C# 8 интерфейсы могут иметь методы по умолчанию, видимые через интерфейсный тип.
  **Явная реализация** `string IFile.Save()` разводит одинаковые члены разных интерфейсов и скрывает член из публичного API класса: вызвать его можно только через интерфейс.
  Структура, присвоенная переменной интерфейса, упаковывается, и изменения через интерфейс меняют копию.
```

::::

::::test

```quiz
id: test-interface-many
type: choice
part: "Интерфейс — это контракт"
prompt: Сколько интерфейсов может реализовать класс?
options:
  - "Сколько угодно"
  - "Один"
  - "Два"
  - "Один, если у класса нет базового класса"
answer: 0
explain: |
  Ограничение «один» действует только для базового класса.
```

```quiz
id: test-interface-public
type: find-bug
part: "Интерфейс — это контракт"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  interface IGreeter
  {
      string Greet();
  }

  class Greeter : IGreeter
  {
      string Greet() => "Привет";
  }
answer: 8
explain: |
  Неявная реализация члена интерфейса должна быть `public`. Без модификатора метод `private`, и интерфейс остаётся нереализованным.
```

```quiz
id: test-interface-swap
type: choice
part: "Интерфейс — это контракт"
prompt: Почему сервис принимает в конструктор `IPaymentGateway`, а не `CardGateway`?
options:
  - "Чтобы подменять реализацию — другую платёжку или заглушку в тестах — не меняя сервис"
  - "Интерфейсы работают быстрее классов"
  - "Классы нельзя передавать в конструктор"
  - "Чтобы сервис мог создать объект интерфейса"
answer: 0
explain: |
  Зависимость от абстракции — основа внедрения зависимостей и модульных тестов.
```

```quiz
id: test-abstract-vs-interface-state
type: choice
part: "Интерфейс или абстрактный класс"
prompt: Что есть у абстрактного класса, но нет у интерфейса?
options:
  - "Поля и конструкторы"
  - "Методы"
  - "Свойства"
  - "Возможность иметь несколько реализаций"
answer: 0
explain: |
  Интерфейс не хранит состояние. Абстрактный класс может держать поля и инициализировать их в конструкторе.
```

```quiz
id: test-default-method-access
type: output
part: "Интерфейс или абстрактный класс"
prompt: Что выведет код?
code: |
  ILogger logger = new ConsoleLogger();
  logger.Warn("диск");

  interface ILogger
  {
      void Log(string message);
      void Warn(string message) => Log("ВНИМАНИЕ: " + message);
  }

  class ConsoleLogger : ILogger
  {
      public void Log(string message) => Console.WriteLine(message);
  }
answer: "ВНИМАНИЕ: диск"
explain: |
  `ConsoleLogger` не реализовал `Warn`, и вызывается реализация по умолчанию из интерфейса. Она в свою очередь вызывает `Log` класса.
```

```quiz
id: test-struct-interface-ok
type: choice
part: "Интерфейс или абстрактный класс"
prompt: Что может сделать структура?
options:
  - "Реализовать интерфейс"
  - "Унаследовать абстрактный класс"
  - "Унаследовать другую структуру"
  - "Быть базовым типом для класса"
answer: 0
explain: |
  Наследование для структур закрыто, а реализация интерфейсов — открыта.
```

```quiz
id: test-explicit-call
type: choice
part: "Явная реализация"
prompt: |
  Как вызвать метод, реализованный явно: `void IExport.Run()`?
options:
  - "Через переменную типа IExport или приведение ((IExport)obj).Run()"
  - "obj.Run()"
  - "obj.IExport.Run()"
  - "Никак, он недоступен"
answer: 0
explain: |
  Явная реализация не входит в публичные члены класса — только в контракт интерфейса.
```

```quiz
id: test-explicit-two
type: output
part: "Явная реализация"
prompt: Что выведет код?
code: |
  var doc = new Document();
  IDatabase db = doc;
  Console.WriteLine(db.Save());

  interface IFile { string Save(); }
  interface IDatabase { string Save(); }

  class Document : IFile, IDatabase
  {
      string IFile.Save() => "файл";
      string IDatabase.Save() => "база";
  }
answer: база
explain: |
  Для каждого интерфейса своя явная реализация. Через `IDatabase` вызывается его версия.
```

```quiz
id: test-explicit-why
type: choice
part: "Явная реализация"
prompt: Зачем используют явную реализацию интерфейса?
options:
  - "Чтобы развести одинаковые члены разных интерфейсов или скрыть член из публичного API класса"
  - "Чтобы метод работал быстрее"
  - "Чтобы метод стал статическим"
  - "Чтобы не реализовывать член вовсе"
answer: 0
explain: |
  Реализация всё равно нужна, просто доступна она только через интерфейсный тип.
```

```quiz
id: test-struct-boxing-interface
type: choice
part: "Структуры и интерфейсы: скрытая упаковка"
prompt: Что происходит при `IComparable c = 5;`?
options:
  - "Упаковка: int копируется в объект в куче"
  - "Ничего особенного: int и так ссылочный"
  - "Ошибка компиляции"
  - "Создаётся объект интерфейса IComparable"
answer: 0
explain: |
  Интерфейс — ссылочный тип, а `int` — значимый. Чтобы на значение можно было сослаться, оно упаковывается.
```

```quiz
id: test-struct-interface-modify
type: output
part: "Структуры и интерфейсы: скрытая упаковка"
prompt: Что выведет код?
code: |
  var light = new Light();
  ISwitch s = light;
  s.Toggle();
  Console.WriteLine(light.On);

  interface ISwitch { void Toggle(); }

  struct Light : ISwitch
  {
      public bool On;
      public void Toggle() => On = !On;
  }
answer: "False"
explain: |
  Присваивание структуры переменной интерфейса создаёт упакованную копию. `Toggle` меняет копию, исходная `light` выключена.
```

```quiz
id: test-generic-no-boxing
type: choice
part: "Структуры и интерфейсы: скрытая упаковка"
prompt: Как передать структуру в метод, который работает с её интерфейсом, без упаковки?
options:
  - "Через обобщённый метод с ограничением: void Use<T>(T item) where T : IMyInterface"
  - "Через параметр типа object"
  - "Через параметр интерфейсного типа"
  - "Никак, упаковка неизбежна"
answer: 0
explain: |
  JIT создаёт отдельную версию обобщённого метода для каждой структуры и вызывает методы напрямую, без обёртки в куче.
```

```quiz
id: test-static-abstract
type: choice
part: "Структуры и интерфейсы: скрытая упаковка"
level: middle
prompt: На чём построена обобщённая математика .NET 7, например INumber<T>?
options:
  - "На статических абстрактных членах интерфейсов"
  - "На методах расширения"
  - "На рефлексии"
  - "На наследовании от абстрактного класса Number"
answer: 0
explain: |
  Интерфейс требует от типа статические операторы, и обобщённый код может складывать и умножать значения любого числового `T`.
```

::::
