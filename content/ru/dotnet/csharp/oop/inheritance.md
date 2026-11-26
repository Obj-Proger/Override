---
id: csharp.oop.inheritance
title: Наследование
level: junior
tracks: [backend, fullstack]
minutes: 30
order: 6
requires: [csharp.oop.static]
quickCheck: [ctor-order, base-ctor-missing, init-order-full, upcast-access]
testMinutes: 6
---

::::warmup

```quiz
id: ctor-order
type: output
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  var dog = new Dog();

  class Animal
  {
      public Animal() => Console.Write("A");
  }

  class Dog : Animal
  {
      public Dog() => Console.Write("D");
  }
answer: AD
explain: |
  Конструктор базового класса всегда выполняется раньше конструктора наследника: сначала строится «животное», потом достраивается «собака».
variants:
  - code: |
      var puppy = new Puppy();

      class Animal
      {
          public Animal() => Console.Write("A");
      }

      class Dog : Animal
      {
          public Dog() => Console.Write("D");
      }

      class Puppy : Dog
      {
          public Puppy() => Console.Write("P");
      }
    answer: ADP
    explain: |
      Конструкторы выполняются от самого базового класса к самому производному: `Animal`, `Dog`, `Puppy`.
```

::::

::::part Наследование и base

**Наследование** позволяет создать класс на основе другого: наследник получает все члены базового класса и добавляет свои.

```csharp
class Employee
{
    public string Name { get; set; } = "";
    public decimal Salary { get; set; }

    public string Describe() => $"{Name}: {Salary}";
}

class Manager : Employee  // Manager наследует Employee
{
    public decimal Bonus { get; set; }

    public decimal TotalPay() => Salary + Bonus;  // Salary унаследован
}

var boss = new Manager { Name = "Ольга", Salary = 200_000, Bonus = 50_000 };
Console.WriteLine(boss.Describe());  // метод базового класса
Console.WriteLine(boss.TotalPay());  // 250000
```

- В C# у класса **один** базовый класс. Множественное наследование классов запрещено (несколько интерфейсов — можно, о них отдельная тема).
- Если базовый класс не указан, это **`object`**. Поэтому у любого объекта есть `ToString()`, `Equals()`, `GetHashCode()`, `GetType()`.
- Наследник видит `public`, `protected` и `internal` члены базового класса, но **не видит `private`**: они есть в объекте, но доступны только коду базового класса.
- **`base`** обращается к члену базового класса из наследника: `base.Describe()`.

Наследование выражает отношение «является»: менеджер **является** сотрудником. Если отношение «имеет» (у сотрудника есть адрес), это поле, а не наследование.

```quiz
id: private-in-derived
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  class Account
  {
      private decimal _balance;
      protected string Owner = "";
  }

  class SavingsAccount : Account
  {
      public void AddInterest()
      {
          Console.WriteLine(Owner);
          _balance *= 1.05m;
      }
  }
answer: 12
explain: |
  `private`-поле есть в объекте наследника, но доступно только коду класса `Account`. `protected`-поле `Owner` наследник видит. Чтобы дать наследникам менять баланс, поле делают `protected` или добавляют `protected`-метод.
```

::::

::::part Конструкторы в иерархии

Конструкторы **не наследуются**: у наследника свои. Но при создании объекта наследника **всегда** сначала выполняется конструктор базового класса — объект строится от основания.

Какой конструктор базового класса вызвать, указывают через **`: base(...)`**:

```csharp
class Employee
{
    public string Name { get; }

    public Employee(string name)
    {
        Name = name;
    }
}

class Manager : Employee
{
    public decimal Bonus { get; }

    public Manager(string name, decimal bonus) : base(name)  // сначала Employee(name)
    {
        Bonus = bonus;
    }
}
```

Если `: base(...)` не написан, вызывается конструктор базового класса **без параметров**. А если его нет — например, потому что в базовом классе объявлен только конструктор с параметрами, — это ошибка компиляции. Тогда наследник обязан явно вызвать подходящий конструктор через `base`.

```quiz
id: base-ctor-missing
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  class Animal
  {
      public string Name { get; }
      public Animal(string name) => Name = name;
  }

  class Cat : Animal
  {
      public Cat()
      {
      }
  }
answer: 9
explain: |
  У `Animal` нет конструктора без параметров, а конструктор `Cat` неявно пытается его вызвать. Нужно `public Cat() : base("Кот")`.
variants:
  - code: |
      class Animal
      {
          public string Name { get; }
          public Animal(string name) => Name = name;
      }

      class Cat : Animal
      {
      }
    answer: 7
    explain: |
      Компилятор добавляет классу `Cat` конструктор без параметров, который вызывает `Animal()`. Такого конструктора нет — ошибка на объявлении класса.
```

::::

::::part Порядок инициализации

Полный порядок при `new Derived()` — классический вопрос собеседований:

1. инициализаторы **статических** полей наследника, затем его статический конструктор;
2. инициализаторы статических полей базового класса, затем его статический конструктор;
3. инициализаторы **полей экземпляра** наследника;
4. инициализаторы полей экземпляра базового класса;
5. **тело конструктора** базового класса;
6. тело конструктора наследника.

Статика выполняется только при первом обращении к типу; при втором `new` шаги 1–2 пропускаются.

Неочевидное здесь — шаги 3–4: **инициализаторы полей наследника выполняются раньше, чем инициализаторы и конструктор базового класса**. Компилятор вставляет инициализаторы полей в начало конструктора, перед вызовом `base(...)`.

```csharp
class Base
{
    static Base() => Console.Write("1");
    public Base() => Console.Write("5");
    private int _b = Log("4");
    protected static int Log(string s) { Console.Write(s); return 0; }
}

class Derived : Base
{
    static Derived() => Console.Write("0");
    public Derived() => Console.Write("6");
    private int _d = Log("3");
}

new Derived();  // 0 1 3 4 5 6 — подробности ниже
```

Вызов «0» — статический конструктор `Derived`. Затем инициализатор `_d` вызывает `Log` из `Base`: это первое обращение к `Base`, срабатывает его статический конструктор — «1», потом сам `Log` — «3». Дальше инициализатор `Base` — «4», тело `Base` — «5», тело `Derived` — «6».

**Виртуальный вызов в конструкторе.** Если конструктор базового класса вызывает метод, переопределённый в наследнике (переопределение — следующая тема), выполнится версия наследника — **до** тела его конструктора. Поля, которые задаются в теле конструктора наследника, будут ещё пустыми. Поэтому виртуальные методы из конструкторов не вызывают.

```quiz
id: init-order-full
type: output
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  new Derived();

  class Base
  {
      public Base() => Console.Write("B");
  }

  class Derived : Base
  {
      private int _x = Note();

      public Derived() => Console.Write("D");

      private static int Note()
      {
          Console.Write("F");
          return 1;
      }
  }
answer: FBD
explain: |
  Инициализаторы полей наследника выполняются первыми — до конструктора базового класса. Затем тело `Base`, затем тело `Derived`.
variants:
  - code: |
      new Derived();
      new Derived();

      class Base
      {
          static Base() => Console.Write("s");
          public Base() => Console.Write("B");
      }

      class Derived : Base
      {
          public Derived() => Console.Write("D");
      }
    answer: sBDBD
    explain: |
      Статический конструктор `Base` выполняется один раз, при первом создании. Обычные конструкторы — при каждом `new`, базовый первым.
```

::::

::::part Приведение в иерархии

Объект наследника можно положить в переменную базового типа — это **восходящее приведение**, оно неявное и безопасное: менеджер — тоже сотрудник.

```csharp
Employee person = new Manager("Ольга", 50_000);
Console.WriteLine(person.Name);   // ок: Name есть у Employee
Console.WriteLine(person.Bonus);  // ошибка компиляции: у типа Employee нет Bonus
```

Через переменную базового типа доступны только члены базового типа, хотя объект остаётся `Manager`. Чтобы добраться до `Bonus`, нужно **нисходящее приведение** с проверкой — те же `is` и `as` из темы про приведение:

```csharp
if (person is Manager manager)
    Console.WriteLine(manager.Bonus);
```

Это основа полиморфизма: код работает со списком `Employee`, а в нём лежат и менеджеры, и разработчики. Как сделать, чтобы общий метод вёл себя по-разному для разных наследников, — следующая тема.

```quiz
id: upcast-access
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  Shape shape = new Circle { Radius = 2 };
  Console.WriteLine(shape.Name);
  Console.WriteLine(shape.Radius);

  class Shape
  {
      public string Name { get; set; } = "фигура";
  }

  class Circle : Shape
  {
      public double Radius { get; set; }
  }
answer: 3
explain: |
  Переменная имеет тип `Shape`, а у `Shape` нет `Radius`. Объект — круг, но компилятор знает только тип переменной. Нужно `if (shape is Circle circle) ... circle.Radius`.
```

:::level middle

**Почему наследование используют осторожно.** Наследник жёстко зависит от устройства базового класса: изменение в базовом классе может сломать всех наследников («хрупкий базовый класс»). Глубокие иерархии трудно понимать. Поэтому часто советуют **композицию вместо наследования**: объект содержит другой объект и делегирует ему работу, а общее поведение описывают интерфейсами. Подробнее — в теме про принципы ООП.

**Принцип подстановки Лисков (LSP).** Наследник должен работать везде, где ожидается базовый класс, не нарушая ожиданий. Классический контрпример — `Square : Rectangle`: квадрат меняет обе стороны при изменении одной, и код, рассчитанный на прямоугольник, ломается. Это буква L из SOLID.

```quiz
id: lsp-square
type: choice
level: middle
prompt: |
  Почему наследование `Square : Rectangle` с изменяемыми Width и Height считают ошибкой проектирования?
options:
  - "Квадрат не может вести себя как прямоугольник: при смене одной стороны меняется и другая, код для прямоугольников ломается"
  - "Квадрат нельзя унаследовать, потому что у него меньше свойств"
  - "Компилятор запрещает такое наследование"
  - "Площадь квадрата вычисляется иначе"
answer: 0
explain: |
  Нарушен принцип подстановки Лисков: наследник не выполняет ожидания базового типа. Отношение «является» верно в геометрии, но не для поведения изменяемых объектов.
```

:::

::::

::::practice

```quiz
id: manager-task
type: code
lang: csharp
prompt: |
  Допишите классы: конструктор `Manager` передаёт имя и оклад в конструктор `Employee` и запоминает премию, а `TotalPay` возвращает оклад плюс премию.
starter: |
  public class Employee
  {
      public string Name { get; }
      public decimal Salary { get; }

      public Employee(string name, decimal salary)
      {
          Name = name;
          Salary = salary;
      }
  }

  public class Manager : Employee
  {
      public decimal Bonus { get; }

      public Manager(string name, decimal salary, decimal bonus) : base(name, salary)
      {
      }

      public decimal TotalPay()
      {
      }
  }
tests: |
  var boss = new Manager("Ольга", 200m, 50m);
  Assert.Equal("Ольга", boss.Name);
  Assert.Equal(50m, boss.Bonus);
  Assert.Equal(250m, boss.TotalPay());
  Employee asEmployee = boss;
  Assert.Equal(200m, asEmployee.Salary);
solution: |
  public class Employee
  {
      public string Name { get; }
      public decimal Salary { get; }

      public Employee(string name, decimal salary)
      {
          Name = name;
          Salary = salary;
      }
  }

  public class Manager : Employee
  {
      public decimal Bonus { get; }

      public Manager(string name, decimal salary, decimal bonus) : base(name, salary)
      {
          Bonus = bonus;
      }

      public decimal TotalPay() => Salary + Bonus;
  }
explain: |
  `: base(name, salary)` заполняет унаследованные свойства, тело конструктора — своё. `Salary` унаследован и доступен в методе наследника.
variants:
  - prompt: |
      Допишите класс `Cat`: его конструктор передаёт имя в `Animal`, а метод `Describe` возвращает строку вида «Кот Барсик» — слово «Кот», пробел и имя из базового класса.
    starter: |
      public class Animal
      {
          protected string Name { get; }

          public Animal(string name)
          {
              Name = name;
          }
      }

      public class Cat : Animal
      {
          public Cat(string name) : base(name)
          {
          }

          public string Describe()
          {
          }
      }
    tests: |
      Assert.Equal("Кот Барсик", new Cat("Барсик").Describe());
      Assert.Equal("Кот Мурка", new Cat("Мурка").Describe());
    solution: |
      public class Animal
      {
          protected string Name { get; }

          public Animal(string name)
          {
              Name = name;
          }
      }

      public class Cat : Animal
      {
          public Cat(string name) : base(name)
          {
          }

          public string Describe() => $"Кот {Name}";
      }
    explain: |
      `protected`-свойство `Name` доступно наследнику, хотя снаружи его не видно.
```

```quiz
id: base-call-fill
type: fill
prompt: Вставьте вызов конструктора базового класса с параметром `name`.
code: |
  public Dog(string name) ___
  {
  }
answer: ": base(name)"
explain: |
  `: base(name)` выбирает конструктор базового класса и выполняется до тела конструктора наследника.
```

::::

::::traps

- **Конструктор базового класса выполняется первым.**
- **Нет конструктора без параметров в базовом классе — наследник обязан вызвать `base(...)`.**
- **Инициализаторы полей наследника выполняются раньше конструктора базового класса.**
- **Статические конструкторы: сначала наследника, потом базового** — при первом обращении.
- **`private` члены базового класса наследнику недоступны**, `protected` — доступны.
- **Через переменную базового типа видны только члены базового типа.**
- **Виртуальный вызов в конструкторе** видит недостроенный объект наследника.

::::

::::summary

```quiz
id: inheritance-recall
type: recall
prompt: |
  Расскажите о наследовании в C#: что наследуется, как работают конструкторы и в каком порядке инициализируется объект.
answer: |
  Класс наследует один базовый класс (по умолчанию `object`) и получает его члены; `private` есть в объекте, но наследнику недоступен, `protected` доступен. `base` обращается к членам базового класса. Наследование выражает «является».
  Конструкторы не наследуются. Сначала выполняется конструктор базового класса, выбранный через `: base(...)`; без него — конструктор без параметров, и если его нет, это ошибка компиляции.
  Порядок при `new Derived()`: статика наследника, статика базового (только при первом обращении), инициализаторы полей наследника, инициализаторы полей базового, тело конструктора базового, тело конструктора наследника. Виртуальный метод, вызванный из конструктора базового класса, видит недостроенный объект наследника.
  Восходящее приведение неявное, через переменную базового типа доступны только его члены; вниз — через `is`/`as`.
```

::::

::::test

```quiz
id: test-single-base
type: choice
part: "Наследование и base"
prompt: Сколько базовых классов может быть у класса в C#?
options:
  - "Один"
  - "Сколько угодно"
  - "Два"
  - "Ни одного: наследование только от интерфейсов"
answer: 0
explain: |
  Множественное наследование классов в C# запрещено. Реализовать можно сколько угодно интерфейсов.
```

```quiz
id: test-object-base
type: choice
part: "Наследование и base"
prompt: От какого класса наследуется класс, если базовый не указан?
options:
  - "System.Object"
  - "Ни от какого"
  - "System.ValueType"
  - "System.Class"
answer: 0
explain: |
  Все классы в итоге наследуют `object`. Поэтому у любого объекта есть `ToString`, `Equals`, `GetHashCode` и `GetType`.
```

```quiz
id: test-base-method
type: output
part: "Наследование и base"
prompt: Что выведет код?
code: |
  var m = new Manager { Name = "Ольга" };
  Console.WriteLine(m.Title());

  class Employee
  {
      public string Name { get; set; } = "";
      public string Title() => $"сотрудник {Name}";
  }

  class Manager : Employee
  {
  }
answer: сотрудник Ольга
explain: |
  `Manager` ничего не добавил и не изменил, метод `Title` унаследован от `Employee`.
```

```quiz
id: test-ctor-not-inherited
type: choice
part: "Конструкторы в иерархии"
prompt: Наследуются ли конструкторы базового класса?
options:
  - "Нет, у наследника свои конструкторы, а базовый вызывается через base"
  - "Да, все"
  - "Только публичные"
  - "Только конструктор без параметров"
answer: 0
explain: |
  Если наследнику нужен конструктор с теми же параметрами, его объявляют и передают аргументы в `base(...)`.
```

```quiz
id: test-base-args
type: output
part: "Конструкторы в иерархии"
prompt: Что выведет код?
code: |
  var cat = new Cat();
  Console.WriteLine(cat.Name);

  class Animal
  {
      public string Name { get; }
      public Animal(string name) => Name = name;
  }

  class Cat : Animal
  {
      public Cat() : base("Мурка") { }
  }
answer: Мурка
explain: |
  Конструктор `Cat` передал имя в конструктор базового класса.
```

```quiz
id: test-three-levels
type: output
part: "Конструкторы в иерархии"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  new C();

  class A { public A() => Console.Write("a"); }
  class B : A { public B() => Console.Write("b"); }
  class C : B { public C() => Console.Write("c"); }
answer: abc
explain: |
  Конструкторы выполняются от корня иерархии к листу.
```

```quiz
id: test-field-before-base
type: choice
part: "Порядок инициализации"
prompt: |
  Что выполняется раньше при `new Derived()`: инициализаторы полей наследника или тело конструктора базового класса?
options:
  - "Инициализаторы полей наследника"
  - "Тело конструктора базового класса"
  - "Одновременно"
  - "Зависит от порядка объявления классов"
answer: 0
explain: |
  Компилятор вставляет инициализаторы полей в начало конструктора наследника, перед вызовом `base(...)`.
```

```quiz
id: test-static-order
type: output
part: "Порядок инициализации"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  new Derived();

  class Base
  {
      static Base() => Console.Write("1");
      public Base() => Console.Write("3");
  }

  class Derived : Base
  {
      static Derived() => Console.Write("0");
      public Derived() => Console.Write("4");
  }
answer: "0134"
explain: |
  Первым — статический конструктор `Derived`: обращение идёт к нему. Конструктор `Derived` вызывает `Base()` — первое обращение к `Base`, его статический конструктор. Затем тела конструкторов: базовый, потом наследник.
```

```quiz
id: test-virtual-in-ctor
type: choice
part: "Порядок инициализации"
prompt: Почему не стоит вызывать виртуальные методы в конструкторе базового класса?
options:
  - "Выполнится версия наследника, когда его конструктор ещё не отработал"
  - "Это ошибка компиляции"
  - "Всегда вызовется версия базового класса"
  - "Виртуальные методы в конструкторах работают медленно"
answer: 0
explain: |
  Объект уже имеет тип наследника, поэтому вызов уходит в переопределённый метод, а поля, задаваемые в теле конструктора наследника, ещё пустые.
```

```quiz
id: test-upcast-implicit
type: choice
part: "Приведение в иерархии"
prompt: Какое приведение неявное и всегда безопасное?
options:
  - "От наследника к базовому классу"
  - "От базового класса к наследнику"
  - "Между двумя наследниками одного класса"
  - "От object к любому классу"
answer: 0
explain: |
  Наследник — это частный случай базового класса. Обратное направление проверяется во время выполнения.
```

```quiz
id: test-downcast-is
type: output
part: "Приведение в иерархии"
prompt: Что выведет код?
code: |
  Employee person = new Developer { Language = "C#" };
  Console.WriteLine(person is Developer d ? d.Language : "нет");

  class Employee { }
  class Developer : Employee { public string Language { get; set; } = ""; }
answer: C#
explain: |
  Объект — `Developer`, проверка `is` проходит, и через `d` доступно его свойство.
variants:
  - code: |
      Employee person = new Employee();
      Console.WriteLine(person is Developer d ? d.Language : "нет");

      class Employee { }
      class Developer : Employee { public string Language { get; set; } = ""; }
    answer: нет
    explain: |
      Объект — просто `Employee`, а не `Developer`, проверка `is` не проходит.
```

```quiz
id: test-composition
type: choice
part: "Приведение в иерархии"
level: middle
prompt: Что означает совет «композиция вместо наследования»?
options:
  - "Объект содержит другой объект и делегирует ему работу, вместо того чтобы наследовать его"
  - "Никогда не использовать классы"
  - "Объединять несколько классов в один файл"
  - "Наследоваться сразу от нескольких классов"
answer: 0
explain: |
  Композиция даёт более слабую связь: внутренний объект можно заменить, а изменение его устройства не ломает внешний класс.
```

::::
