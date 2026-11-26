---
id: csharp.oop.access
title: Модификаторы доступа, const и readonly
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 4
requires: [csharp.oop.struct]
quickCheck: [default-member-access, protected-access, readonly-array, const-vs-readonly]
testMinutes: 6
---

::::warmup

```quiz
id: default-member-access
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var account = new Account();
  account.Deposit(100);
  Console.WriteLine(account.balance);

  class Account
  {
      decimal balance;

      public void Deposit(decimal amount) => balance += amount;
  }
answer: 3
explain: |
  У поля `balance` модификатор не указан, а по умолчанию члены класса `private`: доступны только внутри `Account`. Метод `Deposit` открыт, поэтому строка 2 компилируется.
variants:
  - code: |
      var account = new Account();
      account.Deposit(100);
      Console.WriteLine(account.Balance);

      class Account
      {
          public decimal Balance { get; private set; }

          void Deposit(decimal amount) => Balance += amount;
      }
    answer: 2
    explain: |
      Модификатор у `Deposit` не указан, значит он `private`. Вызвать его снаружи класса нельзя.
```

::::

::::part Модификаторы доступа

Модификатор доступа решает, **какой код** может обращаться к типу или члену:

| Модификатор | Кто имеет доступ |
|---|---|
| `public` | любой код |
| `private` | только этот тип |
| `protected` | этот тип и его наследники |
| `internal` | любой код в той же сборке (проекте) |
| `protected internal` | та же сборка **или** наследники в других сборках |
| `private protected` | наследники **внутри** той же сборки |
| `file` | только этот файл (для типов, C# 11) |

**По умолчанию**, если модификатор не указан:

- члены класса и структуры — `private`;
- типы верхнего уровня (класс не внутри другого класса) — `internal`;
- члены интерфейса — `public`.

`internal` удобен для библиотек: класс нужен внутри проекта, но не должен быть частью публичного API. Проекты с тестами получают доступ к `internal` через атрибут `InternalsVisibleTo`.

Пары `protected internal` и `private protected` часто путают: первое расширяет доступ («или»), второе сужает («и»). На собеседованиях их спрашивают именно ради этой разницы.

```quiz
id: protected-access
type: choice
prompt: Кто может обратиться к члену с модификатором `protected`?
options:
  - "Сам класс и его наследники"
  - "Любой код в той же сборке"
  - "Только сам класс"
  - "Любой код"
answer: 0
explain: |
  `protected` открывает член для наследников, где бы они ни были, но не для остального кода. Наследование — следующие темы.
variants:
  - prompt: Кто может обратиться к члену с модификатором `internal`?
    answer: 1
    explain: |
      `internal` — доступ в пределах сборки, то есть проекта. Для кода из других проектов член закрыт.
  - prompt: Кто может обратиться к члену класса, у которого модификатор не указан?
    answer: 2
    explain: |
      Члены класса по умолчанию `private` — видны только внутри самого класса.
```

::::

::::part Инкапсуляция

**Инкапсуляция** — скрыть внутреннее устройство объекта и дать наружу только безопасные операции. Объект сам следит, чтобы его состояние оставалось правильным:

```csharp
class Account
{
    private decimal _balance;  // снаружи не изменить

    public decimal Balance => _balance;  // читать можно

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
        _balance += amount;
    }

    public bool Withdraw(decimal amount)
    {
        if (amount > _balance)
            return false;  // правило «не уходить в минус» — в одном месте
        _balance -= amount;
        return true;
    }
}
```

Если бы `_balance` был публичным, любой код мог бы записать туда -1000, и правило пришлось бы проверять везде. Закрытое поле и открытые методы делают ошибочное состояние невозможным.

Правило по умолчанию: всё `private`, а открывают только то, что действительно нужно снаружи. Закрыть открытое потом трудно: на него уже опирается чужой код.

```quiz
id: encapsulation-why
type: choice
prompt: Зачем поле баланса делают приватным, а изменение — через методы?
options:
  - "Чтобы правила изменения проверялись в одном месте и объект не попал в неверное состояние"
  - "Чтобы код работал быстрее"
  - "Потому что публичные поля не компилируются"
  - "Чтобы баланс нельзя было прочитать"
answer: 0
explain: |
  Это инкапсуляция: объект защищает свои данные и сам отвечает за их правильность. Читать баланс при этом можно — через свойство.
```

::::

::::part const, readonly и static readonly

Три способа объявить значение, которое не меняется:

```csharp
class Settings
{
    public const int MaxUsers = 100;                     // известно при компиляции
    public readonly DateTime Created = DateTime.Now;     // своё у каждого объекта, задаётся при создании
    public static readonly Guid AppId = Guid.NewGuid();  // одно на весь тип, вычисляется при запуске
}
```

| | `const` | `readonly` | `static readonly` |
|---|---|---|---|
| Когда известно значение | при компиляции | при создании объекта | при первом обращении к типу |
| Где присвоить | только в объявлении | в объявлении или конструкторе | в объявлении или статическом конструкторе |
| Типы | числа, `bool`, `char`, `string`, `enum` | любые | любые |
| Принадлежит | типу | объекту | типу |

**Ловушка `const` между сборками.** Компилятор подставляет значение `const` прямо в код, который его использует. Если библиотека поменяет константу с 100 на 200, а программу не пересобрать, программа продолжит видеть 100. Поэтому в публичных API для значений, которые могут измениться, используют `static readonly`.

**Ловушка `readonly` со ссылкой.** `readonly` запрещает заменить ссылку, но не изменять объект по этой ссылке:

```csharp
class Basket
{
    public readonly int[] Items = new int[3];
}

var basket = new Basket();
basket.Items[0] = 42;       // можно: меняется массив, а не ссылка
basket.Items = new int[5];  // ошибка: ссылку заменить нельзя
```

`readonly` — неизменяемое поле, а не неизменяемый объект.

```quiz
id: readonly-array
type: output
prompt: Что выведет код?
code: |
  var box = new Box();
  box.Values[0] = 7;
  Console.WriteLine(box.Values[0]);

  class Box
  {
      public readonly int[] Values = new int[2];
  }
answer: "7"
explain: |
  `readonly` защищает только саму ссылку: нельзя присвоить `Values` другой массив. Элементы массива менять можно.
```

```quiz
id: const-vs-readonly
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  class Config
  {
      public const int Limit = 10;
      public const DateTime Started = DateTime.Now;
      public readonly DateTime Created = DateTime.Now;
  }
answer: 4
explain: |
  Значение `const` должно быть известно при компиляции, а `DateTime.Now` вычисляется при выполнении. Да и `DateTime` не может быть константой. Здесь подходит `static readonly`.
variants:
  - code: |
      class Config
      {
          public readonly int Limit = 10;

          public void Change()
          {
              Limit = 20;
          }
      }
    answer: 7
    explain: |
      `readonly` поле присваивается только в объявлении или конструкторе. В обычном методе — ошибка.
```

::::

::::part sealed и partial

**`sealed`** запрещает наследоваться от класса. Так закрывают классы, которые не проектировались для расширения: наследник мог бы нарушить их правила. Например, `string` — `sealed`.

```csharp
sealed class Token { }
class Special : Token { }  // ошибка: от sealed-класса наследоваться нельзя
```

**`partial`** разрешает разбить один класс на несколько файлов. Компилятор собирает части в один тип. Главное применение — сгенерированный код: генератор пишет свою часть, программист — свою, и они не мешают друг другу. Так устроены формы WinForms, Razor-компоненты и генераторы исходного кода.

```csharp
// Order.cs
partial class Order { public int Id { get; set; } }

// Order.Generated.cs
partial class Order { public string Describe() => $"Заказ {Id}"; }
```

Бывают и `partial`-методы: объявление в одной части, реализация в другой.

```quiz
id: sealed-meaning
type: choice
prompt: Что делает модификатор `sealed` у класса?
options:
  - "Запрещает от него наследоваться"
  - "Запрещает создавать его объекты"
  - "Делает все поля только для чтения"
  - "Разбивает класс на несколько файлов"
answer: 0
explain: |
  От `sealed`-класса нельзя наследоваться. Объекты создавать можно. Разбиение на файлы — это `partial`.
```

:::level middle

**`sealed` и производительность.** Вызов виртуального метода у `sealed`-класса JIT может сделать прямым (девиртуализация), потому что наследников точно нет. Поэтому многие команды делают классы `sealed` по умолчанию, а открывают для наследования осознанно. В .NET 6+ анализатор CA1852 предлагает запечатать внутренние классы без наследников.

**Модификатор `file`** (C# 11) делает тип видимым только в одном файле. Его используют генераторы кода, чтобы их вспомогательные типы не конфликтовали с вашими.

:::

::::

::::practice

```quiz
id: account-task
type: code
lang: csharp
prompt: |
  Допишите класс: баланс хранится в приватном поле и наружу только читается. `Deposit` игнорирует неположительные суммы, `Withdraw` возвращает `false` и ничего не меняет, если денег не хватает.
starter: |
  public class Account
  {
      private decimal _balance;

      public decimal Balance => _balance;

      public void Deposit(decimal amount)
      {
      }

      public bool Withdraw(decimal amount)
      {
      }
  }
tests: |
  var account = new Account();
  account.Deposit(200m);
  account.Deposit(0m);
  Assert.Equal(200m, account.Balance);
  Assert.False(account.Withdraw(250m));
  Assert.Equal(200m, account.Balance);
  Assert.True(account.Withdraw(50m));
  Assert.Equal(150m, account.Balance);
solution: |
  public class Account
  {
      private decimal _balance;

      public decimal Balance => _balance;

      public void Deposit(decimal amount)
      {
          if (amount > 0)
              _balance += amount;
      }

      public bool Withdraw(decimal amount)
      {
          if (amount > _balance)
              return false;

          _balance -= amount;
          return true;
      }
  }
explain: |
  Поле закрыто, поэтому изменить баланс можно только через методы с проверками. Снаружи `Balance` только читается.
variants:
  - prompt: |
      Допишите класс: лимит задаётся в конструкторе и потом не меняется (`readonly`). `TryAdd` добавляет к `Used` количество и возвращает `true`, только если не превышен лимит; иначе ничего не меняет.
    starter: |
      public class Quota
      {
          private readonly int _limit;

          public int Used { get; private set; }

          public Quota(int limit)
          {
          }

          public bool TryAdd(int amount)
          {
          }
      }
    tests: |
      var quota = new Quota(10);
      Assert.True(quota.TryAdd(7));
      Assert.False(quota.TryAdd(4));
      Assert.Equal(7, quota.Used);
      Assert.True(quota.TryAdd(3));
      Assert.Equal(10, quota.Used);
    solution: |
      public class Quota
      {
          private readonly int _limit;

          public int Used { get; private set; }

          public Quota(int limit)
          {
              _limit = limit;
          }

          public bool TryAdd(int amount)
          {
              if (Used + amount > _limit)
                  return false;

              Used += amount;
              return true;
          }
      }
    explain: |
      `readonly`-поле присваивается в конструкторе и дальше защищено от изменений. `Used` читают все, а меняет только класс.
```

```quiz
id: static-readonly-fill
type: fill
prompt: Вставьте модификаторы для поля, которое одно на весь тип, вычисляется при запуске и не меняется.
code: |
  public ___ Guid InstanceId = Guid.NewGuid();
answer: static readonly
explain: |
  `static` — одно значение на тип, `readonly` — присвоить можно только при инициализации. `const` не подходит: значение не известно при компиляции.
```

::::

::::traps

- **Члены класса по умолчанию `private`, типы верхнего уровня — `internal`.**
- **`protected internal` — «или», `private protected` — «и».**
- **`const` вписывается в вызывающий код** — смена значения не видна без пересборки.
- **`const` — только для значений, известных при компиляции:** числа, строки, `bool`, `char`, `enum`.
- **`readonly` не делает объект неизменяемым** — только ссылку.
- **`sealed` запрещает наследование**, а не создание объектов.

::::

::::summary

```quiz
id: access-recall
type: recall
prompt: Перечислите модификаторы доступа C# и объясните разницу между const, readonly и static readonly.
answer: |
  `public` — всем; `private` — только типу; `protected` — типу и наследникам; `internal` — сборке; `protected internal` — сборке или наследникам; `private protected` — наследникам внутри сборки; `file` — файлу. По умолчанию члены класса `private`, типы верхнего уровня `internal`.
  Инкапсуляция: данные закрыты, наружу — методы и свойства, которые сохраняют объект в правильном состоянии.
  `const` — значение при компиляции, только простые типы, вписывается в вызывающий код. `readonly` — поле объекта, присваивается в объявлении или конструкторе; ссылка неизменна, а объект по ней — нет. `static readonly` — одно значение на тип, вычисляется при запуске; им заменяют `const` в публичных API.
  `sealed` запрещает наследование, `partial` делит класс на файлы, что нужно для сгенерированного кода.
```

::::

::::test

```quiz
id: test-top-level-default
type: choice
part: "Модификаторы доступа"
prompt: Какой доступ у класса верхнего уровня, если модификатор не указан?
options:
  - "internal"
  - "public"
  - "private"
  - "protected"
answer: 0
explain: |
  Типы верхнего уровня по умолчанию видны только внутри своей сборки.
```

```quiz
id: test-protected-internal
type: choice
part: "Модификаторы доступа"
prompt: Чем `protected internal` отличается от `private protected`?
options:
  - "protected internal — сборка ИЛИ наследники; private protected — наследники И только в этой сборке"
  - "Ничем, это синонимы"
  - "protected internal — уже, чем private protected"
  - "private protected разрешает доступ всем в сборке"
answer: 0
explain: |
  Первое расширяет доступ, объединяя два правила. Второе сужает, требуя оба условия.
```

```quiz
id: test-interface-members
type: choice
part: "Модификаторы доступа"
prompt: Какой доступ по умолчанию у членов интерфейса?
options:
  - "public"
  - "private"
  - "internal"
  - "protected"
answer: 0
explain: |
  Интерфейс — контракт для внешнего кода, поэтому его члены по умолчанию открыты.
```

```quiz
id: test-private-other-instance
type: choice
part: "Модификаторы доступа"
prompt: Может ли метод класса `Account` прочитать приватное поле другого объекта `Account`?
options:
  - "Да: private ограничивает доступ по типу, а не по объекту"
  - "Нет: private — только своё поле"
  - "Только через рефлексию"
  - "Только если поле readonly"
answer: 0
explain: |
  Доступ в C# проверяется по типу. Внутри `Account` видны приватные члены любого объекта `Account` — так часто пишут `Equals`.
```

```quiz
id: test-encapsulation-public-field
type: choice
part: "Инкапсуляция"
prompt: Чем плохо публичное поле `public decimal Balance;` в классе счёта?
options:
  - "Любой код может записать туда что угодно, и правила счёта не проверить"
  - "Публичные поля медленнее"
  - "Такое поле нельзя прочитать"
  - "Ничем"
answer: 0
explain: |
  Состоянием объекта начинает управлять внешний код. Закрытое поле с методами оставляет правила в одном месте.
```

```quiz
id: test-encapsulation-default
type: choice
part: "Инкапсуляция"
prompt: Какой доступ стоит давать членам класса по умолчанию?
options:
  - "Самый узкий, private, и открывать только нужное"
  - "public, чтобы потом не менять"
  - "internal всегда"
  - "protected, на случай наследования"
answer: 0
explain: |
  Открыть закрытое потом легко, а закрыть открытое трудно: на него уже опирается чужой код.
```

```quiz
id: test-const-baked
type: choice
part: "const, readonly и static readonly"
prompt: Библиотека поменяла `public const int Max = 100` на 200. Программа, собранная со старой версией, не пересобрана. Какое значение она увидит?
options:
  - "100: константа вписана в её код при компиляции"
  - "200: значение читается из библиотеки"
  - "0"
  - "Программа упадёт"
answer: 0
explain: |
  `const` подставляется в вызывающий код. С `static readonly` программа прочитала бы новое значение из библиотеки.
```

```quiz
id: test-readonly-where
type: choice
part: "const, readonly и static readonly"
prompt: Где можно присвоить значение `readonly`-полю?
options:
  - "В объявлении поля или в конструкторе"
  - "В любом методе класса"
  - "Только в объявлении"
  - "Нигде, оно всегда равно значению по умолчанию"
answer: 0
explain: |
  После завершения конструктора `readonly`-поле больше не меняется.
```

```quiz
id: test-const-types
type: multi
part: "const, readonly и static readonly"
prompt: Какие объявления допустимы?
options:
  - "`const int Max = 10;`"
  - "`const string Name = \"app\";`"
  - "`const int[] Values = { 1, 2 };`"
  - "`const double Pi = 3.14;`"
  - "`const DateTime Start = DateTime.Today;`"
answer: [0, 1, 3]
explain: |
  Константой могут быть числа, `bool`, `char`, `string` и перечисления со значением, известным при компиляции. Массив и `DateTime` — нет.
```

```quiz
id: test-sealed-error
type: find-bug
part: "sealed и partial"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  sealed class Logger
  {
      public void Write(string text) => Console.WriteLine(text);
  }

  class FileLogger : Logger
  {
  }
answer: 6
explain: |
  От `sealed`-класса наследоваться нельзя. Создавать объекты `Logger` и вызывать его методы — можно.
```

```quiz
id: test-partial-use
type: choice
part: "sealed и partial"
prompt: Для чего чаще всего используют partial-классы?
options:
  - "Чтобы сгенерированный код и код программиста лежали в разных файлах одного класса"
  - "Чтобы скрыть класс от других сборок"
  - "Чтобы запретить наследование"
  - "Чтобы класс загружался по частям во время выполнения"
answer: 0
explain: |
  Части собирает компилятор в один тип. Во время выполнения это обычный класс.
```

```quiz
id: test-sealed-devirtualize
type: choice
part: "sealed и partial"
level: middle
prompt: Почему sealed-классы могут работать быстрее?
options:
  - "JIT знает, что наследников нет, и заменяет виртуальные вызовы прямыми"
  - "Их объекты меньше по размеру"
  - "Они хранятся в стеке"
  - "Сборщик мусора их не проверяет"
answer: 0
explain: |
  Девиртуализация убирает поиск метода в таблице и открывает путь к встраиванию (inlining).
```

::::
