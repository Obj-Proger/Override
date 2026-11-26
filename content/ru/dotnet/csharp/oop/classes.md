---
id: csharp.oop.classes
title: Классы и объекты
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 1
requires: [csharp.types.enum]
quickCheck: [two-objects, default-ctor-gone, ctor-chain, init-order]
testMinutes: 6
---

::::warmup

```quiz
id: two-objects
type: output
prompt: Что выведет код? `$"{a.Count} {b.Count}"` подставляет значения через пробел.
code: |
  var a = new Counter();
  var b = new Counter();
  a.Count++;
  a.Count++;
  b.Count++;
  Console.WriteLine($"{a.Count} {b.Count}");

  class Counter
  {
      public int Count;
  }
answer: 2 1
explain: |
  Каждый `new` создаёт отдельный объект со своим полем `Count`. Изменения одного объекта не касаются другого.
variants:
  - code: |
      var a = new Counter();
      var b = a;
      a.Count++;
      b.Count++;
      Console.WriteLine($"{a.Count} {b.Count}");

      class Counter
      {
          public int Count;
      }
    answer: 2 2
    explain: |
      Объект один: `b = a` копирует ссылку. Оба увеличения меняют одно поле.
```

::::

::::part Класс и объект

**Класс** — описание: какие у объекта есть данные и что он умеет. **Объект** (экземпляр) — конкретный экземпляр, созданный по этому описанию через `new`.

```csharp
var account = new Account();         // объект в куче, ссылка в переменной
account.Owner = "Анна";
account.Deposit(500);
Console.WriteLine(account.Balance);  // 500

class Account
{
    public string Owner = "";  // поле: данные объекта
    public decimal Balance;

    public void Deposit(decimal amount)  // метод: действие объекта
    {
        Balance += amount;
    }
}
```

- **Поля** хранят состояние. У каждого объекта свой набор полей. Если полю не присвоили значение, оно получает значение по умолчанию: `0`, `false`, `null`. В отличие от локальных переменных, поля можно читать без явной инициализации.
- **Методы** работают с полями своего объекта. Внутри метода к полям обращаются по имени или через **`this`** — ссылку на текущий объект: `this.Balance`.
- Класс — **ссылочный тип**: переменная хранит ссылку, присваивание копирует ссылку, `==` по умолчанию сравнивает ссылки. Всё это из темы про значимые и ссылочные типы относится к вашим классам.

Слово `public` открывает член для кода снаружи. Без него член закрыт (`private`) — подробно в теме про модификаторы доступа.

```quiz
id: field-default
type: output
prompt: Что выведет код?
code: |
  var user = new User();
  Console.WriteLine(user.Age);

  class User
  {
      public string? Name;
      public int Age;
  }
answer: "0"
explain: |
  Поля объекта сразу получают значения по умолчанию: `0` для `int`, `null` для `string`. Читать их можно без присваивания.
variants:
  - code: |
      var user = new User();
      Console.WriteLine(user.Name is null);

      class User
      {
          public string? Name;
          public int Age;
      }
    answer: "True"
    explain: |
      Поле ссылочного типа по умолчанию равно `null`.
```

::::

::::part Конструкторы

**Конструктор** — особый метод, который выполняется при `new`. Его имя совпадает с именем класса, тип результата не пишется. Конструктор задаёт начальное состояние объекта:

```csharp
var account = new Account("Анна", 100);

class Account
{
    public string Owner;
    public decimal Balance;

    public Account(string owner, decimal balance)
    {
        Owner = owner;
        Balance = balance;
    }
}
```

**Конструктор по умолчанию.** Если в классе нет ни одного конструктора, компилятор добавляет пустой конструктор без параметров — поэтому работал `new Counter()`. Но **как только вы объявили любой конструктор, конструктор по умолчанию больше не создаётся**. После этого `new Account()` — ошибка компиляции, пока не объявить его явно.

**Несколько конструкторов** — обычная перегрузка. Чтобы не повторять код, один конструктор вызывает другой через **`: this(...)`**:

```csharp
class Account
{
    public string Owner;
    public decimal Balance;

    public Account(string owner, decimal balance)
    {
        Owner = owner;
        Balance = balance;
    }

    public Account(string owner) : this(owner, 0)  // сначала выполнится конструктор выше
    {
    }
}
```

**Первичный конструктор** (C# 12) записывает параметры прямо после имени класса. Они доступны во всём теле класса:

```csharp
class Account(string owner, decimal balance)
{
    public string Owner = owner;
    public decimal Balance = balance;
}
```

```quiz
id: default-ctor-gone
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  var guest = new User();
  Console.WriteLine(guest.Name);

  class User
  {
      public string Name;

      public User(string name)
      {
          Name = name;
      }
  }
answer: 1
explain: |
  В классе объявлен конструктор с параметром, поэтому компилятор не добавляет конструктор без параметров. `new User()` вызвать нечего. Нужно передать имя или объявить `public User() { }`.
```

```quiz
id: ctor-chain
type: output
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  var point = new Point(5);

  class Point
  {
      public Point(int x) : this(x, 0)
      {
          Console.Write("B");
      }

      public Point(int x, int y)
      {
          Console.Write("A");
      }
  }
answer: AB
explain: |
  `: this(x, 0)` вызывает другой конструктор до тела текущего. Сначала выполняется он и печатает `A`, потом тело первого — `B`.
variants:
  - code: |
      var point = new Point(5, 7);

      class Point
      {
          public Point(int x) : this(x, 0)
          {
              Console.Write("B");
          }

          public Point(int x, int y)
          {
              Console.Write("A");
          }
      }
    answer: A
    explain: |
      Вызван конструктор с двумя параметрами. Он ни к какому другому не обращается, поэтому печатается только `A`.
```

::::

::::part Инициализаторы и порядок

**Инициализатор поля** задаёт значение прямо в объявлении: `public int Limit = 10;`. Инициализаторы выполняются **до тела конструктора**, в порядке объявления полей:

```csharp
class Order
{
    public string Status = "новый";  // 1. инициализаторы полей
    public int Items;

    public Order()
    {
        Status = "черновик";  // 2. тело конструктора — может перезаписать
    }
}
```

**Инициализатор объекта** присваивает поля сразу после конструктора, в фигурных скобках:

```csharp
var order = new Order { Items = 3 };  // сначала конструктор, потом Items = 3
```

Порядок целиком: инициализаторы полей → тело конструктора → инициализатор объекта. Поэтому значение из фигурных скобок «побеждает» всё остальное.

**Короткий `new`.** Если тип уже указан слева, справа его можно не повторять:

```csharp
Order order = new();  // то же, что new Order()
Order other = new() { Items = 2 };
```

```quiz
id: init-order
type: output
prompt: Что выведет код?
code: |
  var order = new Order { Status = "оплачен" };
  Console.WriteLine(order.Status);

  class Order
  {
      public string Status = "новый";

      public Order()
      {
          Status = "черновик";
      }
  }
answer: оплачен
explain: |
  Порядок: инициализатор поля («новый»), тело конструктора («черновик»), инициализатор объекта («оплачен»). Последнее присваивание и остаётся.
variants:
  - code: |
      var order = new Order();
      Console.WriteLine(order.Status);

      class Order
      {
          public string Status = "новый";

          public Order()
          {
              Status = "черновик";
          }
      }
    answer: черновик
    explain: |
      Инициализатор поля выполняется до тела конструктора, и конструктор перезаписывает значение.
```

::::

::::part Анонимные типы

Иногда нужен объект на один раз — например, сгруппировать пару значений. Объявлять ради этого класс не обязательно: есть **анонимные типы**.

```csharp
var item = new { Name = "Чай", Price = 120m };
Console.WriteLine(item.Name);  // Чай
item.Price = 100;              // ошибка компиляции: свойства только для чтения
```

- Тип создаёт компилятор, имени у него нет, поэтому переменную объявляют через `var`.
- Члены только для чтения.
- Два анонимных объекта с одинаковыми значениями равны по `Equals`: компилятор генерирует сравнение по значениям.
- Используются внутри одного метода, чаще всего в LINQ. Вернуть такой объект из метода с понятным типом нельзя — для этого объявляют класс или запись.

```quiz
id: anonymous-readonly
type: choice
prompt: Что верно для анонимного типа `var p = new { X = 1, Y = 2 };`?
options:
  - "Его члены только для чтения, а тип создаёт компилятор"
  - "Его можно вернуть из метода как p"
  - "Значения X и Y можно менять"
  - "Это структура"
answer: 0
explain: |
  Анонимный тип — класс без имени, сгенерированный компилятором, с членами только для чтения. Имени нет, поэтому за пределы метода его передают разве что как `object`.
```

:::level middle

**Первичный конструктор — не поля.** Параметры первичного конструктора класса не становятся публичными полями. Если параметр используется в методах, компилятор сохраняет его в скрытом поле, и его можно случайно изменить внутри класса. Если параметр присвоен полю и ещё используется в методе, получатся две копии. Поэтому значения из первичного конструктора обычно сразу кладут в свойства или поля.

**Почему класс, а не набор переменных.** Класс связывает данные и действия, которые должны меняться согласованно. Если баланс может измениться только через `Deposit` и `Withdraw`, правила (например, «нельзя уйти в минус») проверяются в одном месте. Это инкапсуляция, к ней ведут следующие темы: свойства и модификаторы доступа.

:::

::::

::::practice

```quiz
id: counter-task
type: code
lang: csharp
prompt: |
  Допишите класс счётчика: `Increment` увеличивает `Count` на шаг, заданный в конструкторе, а `Reset` сбрасывает его в 0.
starter: |
  public class Counter
  {
      public int Count;
      public int Step;

      public Counter(int step)
      {
      }

      public void Increment()
      {
      }

      public void Reset()
      {
      }
  }
tests: |
  var counter = new Counter(5);
  counter.Increment();
  counter.Increment();
  Assert.Equal(10, counter.Count);
  counter.Reset();
  Assert.Equal(0, counter.Count);
  var other = new Counter(1);
  other.Increment();
  Assert.Equal(1, other.Count);
  Assert.Equal(0, counter.Count);
solution: |
  public class Counter
  {
      public int Count;
      public int Step;

      public Counter(int step)
      {
          Step = step;
      }

      public void Increment()
      {
          Count += Step;
      }

      public void Reset()
      {
          Count = 0;
      }
  }
explain: |
  Конструктор запоминает шаг в поле, методы работают с полями своего объекта. У второго счётчика свои поля, поэтому первый не меняется.
variants:
  - prompt: |
      Допишите класс банковского счёта: `Deposit` добавляет положительную сумму (неположительную игнорирует), `Withdraw` снимает сумму и возвращает `true`, если денег хватает, иначе ничего не меняет и возвращает `false`.
    starter: |
      public class Account
      {
          public decimal Balance;

          public void Deposit(decimal amount)
          {
          }

          public bool Withdraw(decimal amount)
          {
          }
      }
    tests: |
      var account = new Account();
      account.Deposit(100m);
      account.Deposit(-50m);
      Assert.Equal(100m, account.Balance);
      Assert.True(account.Withdraw(30m));
      Assert.Equal(70m, account.Balance);
      Assert.False(account.Withdraw(500m));
      Assert.Equal(70m, account.Balance);
    solution: |
      public class Account
      {
          public decimal Balance;

          public void Deposit(decimal amount)
          {
              if (amount > 0)
                  Balance += amount;
          }

          public bool Withdraw(decimal amount)
          {
              if (amount > Balance)
                  return false;

              Balance -= amount;
              return true;
          }
      }
    explain: |
      Правила изменения баланса собраны в методах класса. Если бы код снаружи менял `Balance` напрямую, проверки можно было бы обойти — как это запретить, расскажут следующие темы.
```

```quiz
id: target-new
type: fill
prompt: Вставьте выражение, чтобы создать объект, не повторяя имя типа.
code: |
  Account account = ___;
answer: new()
explain: |
  Когда тип указан слева, `new()` создаёт объект этого типа конструктором без параметров.
```

::::

::::traps

- **Объявили конструктор — конструктор без параметров исчез.** Его нужно объявить явно, если он нужен.
- **Порядок: инициализаторы полей → конструктор → инициализатор объекта.**
- **`: this(...)` выполняется до тела конструктора.**
- **Класс — ссылочный тип:** `b = a` даёт два имени одного объекта.
- **Поля получают значения по умолчанию**, локальные переменные — нет.
- **Анонимный тип только для чтения** и живёт внутри метода.

::::

::::summary

```quiz
id: classes-recall
type: recall
prompt: Расскажите, что такое класс и объект, как работают конструкторы и в каком порядке инициализируется объект.
answer: |
  **Класс** описывает данные (поля) и поведение (методы); **объект** — экземпляр, созданный `new`, со своими полями. Класс — ссылочный тип. `this` — ссылка на текущий объект.
  **Конструктор** выполняется при `new` и задаёт начальное состояние. Без объявленных конструкторов компилятор добавляет пустой без параметров; после объявления любого — не добавляет. Конструкторы перегружаются и вызывают друг друга через `: this(...)` — до своего тела. Первичный конструктор (C# 12) пишется после имени класса.
  Порядок: инициализаторы полей в порядке объявления → тело конструктора → инициализатор объекта `{ X = ... }`.
  Поля получают значения по умолчанию. Анонимные типы — классы без имени с членами только для чтения, для использования внутри метода.
```

::::

::::test

```quiz
id: test-this
type: choice
part: "Класс и объект"
prompt: Что такое `this` внутри метода класса?
options:
  - "Ссылка на объект, у которого вызван метод"
  - "Ссылка на класс"
  - "Первый параметр метода"
  - "Ссылка на объект, созданный последним"
answer: 0
explain: |
  `this` указывает на текущий экземпляр. Обычно его пишут, когда имя поля совпадает с именем параметра: `this.name = name`.
```

```quiz
id: test-class-reference
type: output
part: "Класс и объект"
prompt: Что выведет код?
code: |
  var first = new Box();
  var second = new Box();
  Console.WriteLine(first == second);

  class Box
  {
      public int Size;
  }
answer: "False"
explain: |
  Класс — ссылочный тип, `==` по умолчанию сравнивает ссылки. Объекты разные, хоть и с одинаковыми полями.
```

```quiz
id: test-method-changes
type: output
part: "Класс и объект"
prompt: Что выведет код?
code: |
  var wallet = new Wallet();
  Fill(wallet);
  Console.WriteLine(wallet.Money);

  void Fill(Wallet w) => w.Money = 100;

  class Wallet
  {
      public int Money;
  }
answer: "100"
explain: |
  Метод получил копию ссылки на тот же объект и изменил его поле. Изменение видно снаружи.
```

```quiz
id: test-default-ctor
type: choice
part: "Конструкторы"
prompt: Когда компилятор сам добавляет конструктор без параметров?
options:
  - "Только когда в классе не объявлено ни одного конструктора"
  - "Всегда"
  - "Никогда"
  - "Когда все поля публичные"
answer: 0
explain: |
  Как только появляется свой конструктор, компилятор считает, что вы управляете созданием объекта сами, и пустой не добавляет.
```

```quiz
id: test-ctor-return
type: choice
part: "Конструкторы"
prompt: Чем конструктор отличается от обычного метода?
options:
  - "Имя совпадает с именем класса, тип результата не указывается, вызывается через new"
  - "Конструктор всегда статический"
  - "Конструктор возвращает void"
  - "Ничем, это обычный метод"
answer: 0
explain: |
  Конструктор вызывается при создании объекта. Даже `void` у него не пишут.
```

```quiz
id: test-ctor-chain-order
type: output
part: "Конструкторы"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  var item = new Item();

  class Item
  {
      public Item() : this("x")
      {
          Console.Write("1");
      }

      public Item(string name)
      {
          Console.Write("2");
      }
  }
answer: "21"
explain: |
  Конструктор, указанный в `: this(...)`, выполняется первым. Потом тело вызванного.
```

```quiz
id: test-field-init-order
type: output
part: "Инициализаторы и порядок"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  var log = new Log();

  class Log
  {
      public int A = Write(1);

      public Log()
      {
          Write(3);
      }

      public int B = Write(2);

      static int Write(int n)
      {
          Console.Write(n);
          return n;
      }
  }
answer: "123"
explain: |
  Инициализаторы полей выполняются в порядке объявления, все — до тела конструктора, даже если поле объявлено ниже конструктора.
```

```quiz
id: test-object-initializer
type: output
part: "Инициализаторы и порядок"
prompt: Что выведет код?
code: |
  var task = new TodoItem { Title = "Купить хлеб" };
  Console.WriteLine(task.Done);

  class TodoItem
  {
      public string Title = "";
      public bool Done;
  }
answer: "False"
explain: |
  Инициализатор объекта задал только `Title`. `Done` осталось со значением по умолчанию.
```

```quiz
id: test-initializer-after
type: choice
part: "Инициализаторы и порядок"
prompt: Когда выполняется инициализатор объекта `new Order { Items = 3 }`?
options:
  - "После конструктора"
  - "До конструктора"
  - "Вместо конструктора"
  - "Только если конструктора нет"
answer: 0
explain: |
  Сначала объект создаётся конструктором, потом выполняются присваивания из фигурных скобок.
```

```quiz
id: test-anonymous-equals
type: output
part: "Анонимные типы"
prompt: Что выведет код?
code: |
  var a = new { X = 1, Y = 2 };
  var b = new { X = 1, Y = 2 };
  Console.WriteLine(a.Equals(b));
answer: "True"
explain: |
  Компилятор генерирует для анонимного типа `Equals`, сравнивающий значения членов. При этом `a == b` дал бы `False`: оператор не переопределён и сравнивает ссылки.
```

```quiz
id: test-anonymous-var
type: choice
part: "Анонимные типы"
prompt: Почему переменную анонимного типа объявляют через `var`?
options:
  - "У типа нет имени, которое можно написать"
  - "Так быстрее"
  - "Анонимный тип — это dynamic"
  - "Иначе члены станут изменяемыми"
answer: 0
explain: |
  Имя типа генерирует компилятор, и в коде его не написать. `var` выводит этот тип.
```

```quiz
id: test-primary-ctor-capture
type: choice
part: "Анонимные типы"
level: middle
prompt: Что происходит с параметром первичного конструктора класса, который используется в методе?
options:
  - "Компилятор сохраняет его в скрытом поле, доступном внутри класса"
  - "Он становится публичным свойством"
  - "Он доступен только в инициализаторах полей"
  - "Это ошибка компиляции"
answer: 0
explain: |
  У класса параметры первичного конструктора не превращаются в свойства, как у записей. Если они нужны в методах, компилятор хранит их в скрытых полях.
```

::::
