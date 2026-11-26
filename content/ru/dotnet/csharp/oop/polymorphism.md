---
id: csharp.oop.polymorphism
title: "Полиморфизм: virtual, override, new"
level: junior
tracks: [backend, fullstack]
minutes: 30
order: 7
requires: [csharp.oop.inheritance]
quickCheck: [override-call, new-hides, abstract-new, overload-vs-override]
testMinutes: 6
---

::::warmup

```quiz
id: override-call
type: output
prompt: Что выведет код?
code: |
  Animal pet = new Dog();
  Console.WriteLine(pet.Speak());

  class Animal
  {
      public virtual string Speak() => "...";
  }

  class Dog : Animal
  {
      public override string Speak() => "Гав";
  }
answer: Гав
explain: |
  Переменная имеет тип `Animal`, но объект — `Dog`. Для виртуального метода выбирается версия по настоящему типу объекта, а не по типу переменной.
variants:
  - code: |
      Animal pet = new Animal();
      Console.WriteLine(pet.Speak());

      class Animal
      {
          public virtual string Speak() => "...";
      }

      class Dog : Animal
      {
          public override string Speak() => "Гав";
      }
    answer: "..."
    explain: |
      Объект — обычный `Animal`, поэтому вызывается его версия метода.
```

::::

::::part virtual и override

**Полиморфизм** — один и тот же вызов ведёт себя по-разному в зависимости от настоящего типа объекта. Код работает с базовым типом и не знает, какой именно наследник перед ним.

Базовый класс помечает метод **`virtual`** — «можно переопределить». Наследник пишет свою версию с **`override`**:

```csharp
class Shape
{
    public virtual double Area() => 0;
}

class Circle : Shape
{
    public double Radius { get; init; }
    public override double Area() => Math.PI * Radius * Radius;
}

class Square : Shape
{
    public double Side { get; init; }
    public override double Area() => Side * Side;
}

Shape[] shapes = { new Circle { Radius = 1 }, new Square { Side = 2 } };
foreach (Shape shape in shapes)
    Console.WriteLine(shape.Area());  // 3.14..., затем 4
```

Цикл ничего не знает о кругах и квадратах — он вызывает `Area()` у `Shape`. Какая версия выполнится, решается **во время выполнения** по настоящему типу объекта. Это **позднее связывание**. Чтобы добавить треугольник, не нужно трогать цикл — достаточно нового наследника.

Внутри переопределения можно вызвать версию базового класса: `base.Area()`. Переопределить можно только `virtual`, `abstract` или уже `override` член. Свойства тоже бывают виртуальными.

Так же устроены методы `object`: `ToString()`, `Equals()`, `GetHashCode()` — виртуальные, и классы их переопределяют.

```quiz
id: base-in-override
type: output
prompt: Что выведет код?
code: |
  Report report = new DetailedReport();
  Console.WriteLine(report.Title());

  class Report
  {
      public virtual string Title() => "Отчёт";
  }

  class DetailedReport : Report
  {
      public override string Title() => base.Title() + " подробный";
  }
answer: Отчёт подробный
explain: |
  Вызов уходит в переопределение `DetailedReport`, а оно через `base.Title()` берёт текст базовой версии и дописывает своё.
```

::::

::::part new: сокрытие метода

Если наследник объявляет метод с тем же именем **без `override`**, он не переопределяет, а **скрывает** метод базового класса. Чтобы показать, что это намеренно, пишут **`new`** (без него компилятор выдаст предупреждение, но поведение то же).

Разница с `override` видна, когда объект лежит в переменной базового типа:

```csharp
class Base
{
    public virtual string Who() => "Base";
}

class Derived : Base
{
    public new string Who() => "Derived";  // скрытие, а не переопределение
}

Derived d = new Derived();
Base b = d;  // тот же объект

Console.WriteLine(d.Who());  // Derived: выбрано по типу переменной Derived
Console.WriteLine(b.Who());  // Base!   выбрано по типу переменной Base
```

| | `override` | `new` |
|---|---|---|
| Что делает | заменяет реализацию виртуального метода | объявляет новый метод с тем же именем |
| Через переменную базового типа | вызывается версия наследника | вызывается версия базового класса |
| Решается | во время выполнения, по типу объекта | при компиляции, по типу переменной |

`new` используют редко — например, когда базовый класс из чужой библиотеки добавил метод с именем, которое у вас уже есть. В обычном коде нужен `override`.

```quiz
id: new-hides
type: output
prompt: Что выведет код?
code: |
  Animal pet = new Cat();
  Console.WriteLine(pet.Speak());

  class Animal
  {
      public virtual string Speak() => "...";
  }

  class Cat : Animal
  {
      public new string Speak() => "Мяу";
  }
answer: "..."
explain: |
  `new` не переопределяет, а скрывает метод. Вызов через переменную типа `Animal` выбирает метод `Animal`, и переопределения у объекта нет.
variants:
  - code: |
      Cat pet = new Cat();
      Console.WriteLine(pet.Speak());

      class Animal
      {
          public virtual string Speak() => "...";
      }

      class Cat : Animal
      {
          public new string Speak() => "Мяу";
      }
    answer: Мяу
    explain: |
      Переменная имеет тип `Cat`, поэтому компилятор выбирает метод `Cat`, который скрыл базовый.
  - code: |
      Animal pet = new Cat();
      Console.WriteLine(pet.Speak());

      class Animal
      {
          public string Speak() => "...";
      }

      class Cat : Animal
      {
          public new string Speak() => "Мяу";
      }
    answer: "..."
    explain: |
      Метод `Animal.Speak` не виртуальный. Невиртуальный вызов определяется типом переменной — `Animal`.
```

::::

::::part Абстрактные классы и методы

Иногда у базового класса нет разумной реализации: какая площадь у «фигуры вообще»? Тогда метод делают **`abstract`** — без тела. Наследник **обязан** его переопределить.

```csharp
abstract class Shape
{
    public abstract double Area();  // нет тела: каждая фигура считает по-своему

    public string Describe() => $"Площадь {Area():F2}";  // обычный метод может вызывать абстрактный
}

class Circle : Shape
{
    public double Radius { get; init; }
    public override double Area() => Math.PI * Radius * Radius;
}

var s = new Shape();  // ошибка: объект абстрактного класса создать нельзя
```

- Класс с хотя бы одним абстрактным членом должен быть объявлен `abstract`.
- Объект абстрактного класса создать нельзя — только объекты наследников.
- Абстрактный класс может содержать обычные методы, поля и конструкторы: это общий каркас для наследников.
- Наследник, который не реализовал все абстрактные члены, сам должен быть абстрактным.

`Describe` выше — пример «шаблонного метода»: общий алгоритм в базовом классе, конкретный шаг — в наследниках.

```quiz
id: abstract-new
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  Shape shape = new Shape();
  Console.WriteLine(shape.Area());

  abstract class Shape
  {
      public abstract double Area();
  }
answer: 1
explain: |
  Объект абстрактного класса создать нельзя: у `Area` нет реализации. Создают наследника, который её дал, и кладут его в переменную типа `Shape`.
variants:
  - code: |
      Shape shape = new Square();
      Console.WriteLine(shape.Area());

      abstract class Shape
      {
          public abstract double Area();
      }

      class Square : Shape
      {
      }
    answer: 9
    explain: |
      `Square` не абстрактный, но не реализовал абстрактный метод `Area`. Нужно `public override double Area() => ...` или объявить `Square` абстрактным.
```

::::

::::part Перегрузка и переопределение

Два похожих слова, которые постоянно путают на собеседованиях:

| | Перегрузка (overload) | Переопределение (override) |
|---|---|---|
| Что это | методы с одним именем и **разными параметрами** | новая реализация **того же** метода в наследнике |
| Где | в одном классе (или в иерархии) | только в наследнике |
| Когда выбирается | при компиляции, по типам аргументов | во время выполнения, по типу объекта |
| Ключевые слова | не нужны | `virtual` / `abstract` и `override` |

Следствие: перегрузка выбирается по **объявленным** типам, а не по настоящим.

```csharp
void Feed(Animal a) => Console.WriteLine("животное");
void Feed(Dog d) => Console.WriteLine("собака");

Animal pet = new Dog();
Feed(pet);  // «животное»: переменная типа Animal, перегрузка выбрана при компиляции
```

**`sealed override`** запрещает переопределять метод дальше по иерархии: наследники наследника получат эту версию.

```quiz
id: overload-vs-override
type: output
prompt: Что выведет код?
code: |
  Animal pet = new Dog();
  Feed(pet);

  void Feed(Animal a) => Console.WriteLine("животное");
  void Feed(Dog d) => Console.WriteLine("собака");

  class Animal { }
  class Dog : Animal { }
answer: животное
explain: |
  Перегрузка выбирается при компиляции по типу переменной. `pet` объявлена как `Animal`, поэтому вызывается `Feed(Animal)`, хотя объект — собака.
variants:
  - code: |
      var pet = new Dog();
      Feed(pet);

      void Feed(Animal a) => Console.WriteLine("животное");
      void Feed(Dog d) => Console.WriteLine("собака");

      class Animal { }
      class Dog : Animal { }
    answer: собака
    explain: |
      `var` вывел тип `Dog`. Для аргумента типа `Dog` точное совпадение — `Feed(Dog)`.
```

:::level middle

**Как работает виртуальный вызов.** У каждого типа есть таблица виртуальных методов (vtable): по слоту на каждый виртуальный метод с адресом реализации. `override` записывает в слот наследника свой адрес, `new` создаёт новый слот. Вызов виртуального метода — инструкция IL `callvirt`: среда берёт тип объекта, находит его таблицу и вызывает метод из слота. Поэтому решение принимается во время выполнения.

Это немного дороже прямого вызова и мешает встраиванию. Если класс `sealed` или JIT видит точный тип, он может заменить вызов прямым — девиртуализация.

**Ковариантный тип результата** (C# 9). Переопределение может возвращать более конкретный тип, чем базовый метод: `public override Dog Clone()` при `public virtual Animal Clone()` в базовом классе.

```quiz
id: vtable
type: choice
level: middle
prompt: Почему версия виртуального метода выбирается во время выполнения, а не при компиляции?
options:
  - "Вызов идёт через таблицу виртуальных методов настоящего типа объекта, а он известен только при выполнении"
  - "Компилятор не умеет выбирать методы"
  - "Все методы в C# вызываются через рефлексию"
  - "Это делается при компиляции, а не при выполнении"
answer: 0
explain: |
  Компилятор знает только тип переменной. Настоящий тип объекта, а значит и нужный слот в его таблице, известен только когда код выполняется.
```

:::

::::

::::practice

```quiz
id: shapes-task
type: code
lang: csharp
prompt: |
  Допишите классы: `Rectangle` и `Circle` переопределяют абстрактный метод `Area`. Метод `Total` уже написан и считает сумму площадей, ничего не зная о конкретных фигурах.
starter: |
  public abstract class Shape
  {
      public abstract double Area();

      public static double Total(Shape[] shapes)
      {
          double sum = 0;
          foreach (var shape in shapes)
              sum += shape.Area();
          return sum;
      }
  }

  public class Rectangle : Shape
  {
      public double Width { get; init; }
      public double Height { get; init; }

      public override double Area()
      {
      }
  }

  public class Circle : Shape
  {
      public double Radius { get; init; }

      public override double Area()
      {
      }
  }
tests: |
  Assert.Equal(6.0, new Rectangle { Width = 2, Height = 3 }.Area());
  Assert.Equal(Math.PI * 4, new Circle { Radius = 2 }.Area());
  Shape[] shapes = { new Rectangle { Width = 1, Height = 1 }, new Rectangle { Width = 2, Height = 2 } };
  Assert.Equal(5.0, Shape.Total(shapes));
solution: |
  public abstract class Shape
  {
      public abstract double Area();

      public static double Total(Shape[] shapes)
      {
          double sum = 0;
          foreach (var shape in shapes)
              sum += shape.Area();
          return sum;
      }
  }

  public class Rectangle : Shape
  {
      public double Width { get; init; }
      public double Height { get; init; }

      public override double Area() => Width * Height;
  }

  public class Circle : Shape
  {
      public double Radius { get; init; }

      public override double Area() => Math.PI * Radius * Radius;
  }
explain: |
  `Total` вызывает `Area()` через базовый тип, а выполняется версия конкретной фигуры. Новая фигура не потребует менять `Total`.
variants:
  - prompt: |
      Допишите классы: `Dog` и `Cat` переопределяют виртуальный метод `Speak` («Гав» и «Мяу»), а `Chorus` склеивает голоса всех животных через пробел — он уже написан.
    starter: |
      public class Animal
      {
          public virtual string Speak() => "...";

          public static string Chorus(Animal[] animals)
          {
              var voices = new string[animals.Length];
              for (int i = 0; i < animals.Length; i++)
                  voices[i] = animals[i].Speak();
              return string.Join(" ", voices);
          }
      }

      public class Dog : Animal
      {
          public override string Speak()
          {
          }
      }

      public class Cat : Animal
      {
          public override string Speak()
          {
          }
      }
    tests: |
      Assert.Equal("Гав", new Dog().Speak());
      Animal[] animals = { new Cat(), new Dog(), new Animal() };
      Assert.Equal("Мяу Гав ...", Animal.Chorus(animals));
    solution: |
      public class Animal
      {
          public virtual string Speak() => "...";

          public static string Chorus(Animal[] animals)
          {
              var voices = new string[animals.Length];
              for (int i = 0; i < animals.Length; i++)
                  voices[i] = animals[i].Speak();
              return string.Join(" ", voices);
          }
      }

      public class Dog : Animal
      {
          public override string Speak() => "Гав";
      }

      public class Cat : Animal
      {
          public override string Speak() => "Мяу";
      }
    explain: |
      Через массив `Animal[]` вызывается версия настоящего типа каждого объекта, а для простого `Animal` — базовая.
```

```quiz
id: override-fill
type: fill
prompt: Вставьте ключевое слово, чтобы метод наследника переопределял виртуальный метод базового класса.
code: |
  public ___ string ToString() => $"Заказ {Id}";
answer: override
explain: |
  `ToString` виртуальный в `object`. `override` заменяет реализацию, и её увидит любой код, даже работающий с объектом как с `object`.
```

::::

::::traps

- **Виртуальный метод выбирается по типу объекта**, невиртуальный и скрытый через `new` — по типу переменной.
- **`new` не переопределяет:** через базовую переменную вызовется базовая версия.
- **Перегрузка выбирается при компиляции по объявленным типам**, переопределение — при выполнении.
- **Объект абстрактного класса создать нельзя**, а наследник обязан реализовать все абстрактные члены.
- **Переопределить можно только `virtual`, `abstract` или `override` член.**
- **`base.Method()` внутри override** вызывает базовую реализацию.

::::

::::summary

```quiz
id: polymorphism-recall
type: recall
prompt: Расскажите, как работает полиморфизм в C#, чем override отличается от new и перегрузка от переопределения.
answer: |
  **Полиморфизм** — один вызов ведёт себя по-разному в зависимости от настоящего типа объекта. Базовый класс объявляет `virtual` (или `abstract`) метод, наследник даёт `override`. Вызов через переменную базового типа выполняет версию настоящего типа — позднее связывание через таблицу виртуальных методов.
  **`new`** не переопределяет, а скрывает метод: через переменную базового типа вызовется базовая версия, выбор делается по типу переменной при компиляции.
  **`abstract`** метод не имеет тела; класс с ним абстрактный, его объект создать нельзя, наследник обязан реализовать. Абстрактный класс может содержать обычные члены.
  **Перегрузка** — одно имя, разные параметры, выбор при компиляции по объявленным типам аргументов. **Переопределение** — новая реализация того же метода в наследнике, выбор при выполнении по типу объекта.
```

::::

::::test

```quiz
id: test-late-binding
type: choice
part: "virtual и override"
prompt: По чему выбирается реализация виртуального метода при вызове?
options:
  - "По настоящему типу объекта во время выполнения"
  - "По типу переменной при компиляции"
  - "По имени файла, где объявлен класс"
  - "Всегда базовая реализация"
answer: 0
explain: |
  Это и есть позднее связывание: вызов уходит в версию того типа, которым объект является на самом деле.
```

```quiz
id: test-override-only-virtual
type: find-bug
part: "virtual и override"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  class Base
  {
      public string Name() => "base";
  }

  class Derived : Base
  {
      public override string Name() => "derived";
  }
answer: 8
explain: |
  Переопределить можно только `virtual`, `abstract` или `override` метод. `Base.Name` — обычный метод.
```

```quiz
id: test-tostring-override
type: output
part: "virtual и override"
prompt: Что выведет код?
code: |
  object item = new Product { Name = "Чай" };
  Console.WriteLine(item.ToString());

  class Product
  {
      public string Name { get; set; } = "";
      public override string ToString() => $"Товар: {Name}";
  }
answer: "Товар: Чай"
explain: |
  `ToString` виртуальный в `object`. Даже через переменную типа `object` вызывается переопределение `Product`.
```

```quiz
id: test-new-vs-override
type: output
part: "new: сокрытие метода"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  Base b = new Derived();
  Console.Write(b.A());
  Console.Write(b.B());

  class Base
  {
      public virtual string A() => "1";
      public virtual string B() => "1";
  }

  class Derived : Base
  {
      public override string A() => "2";
      public new string B() => "2";
  }
answer: "21"
explain: |
  `A` переопределён — вызывается версия `Derived`. `B` скрыт через `new` — через переменную `Base` вызывается версия `Base`.
```

```quiz
id: test-new-keyword-meaning
type: choice
part: "new: сокрытие метода"
prompt: Что означает `new` перед методом наследника?
options:
  - "Метод скрывает одноимённый метод базового класса, не переопределяя его"
  - "Метод создаёт новый объект"
  - "Метод переопределяет виртуальный метод"
  - "Метод можно вызвать только у новых объектов"
answer: 0
explain: |
  `new` подтверждает, что совпадение имени намеренное и это отдельный метод. Полиморфизма через базовый тип не будет.
```

```quiz
id: test-new-via-derived
type: output
part: "new: сокрытие метода"
prompt: Что выведет код?
code: |
  var d = new Derived();
  Base b = d;
  Console.WriteLine(d.Who() + b.Who());

  class Base { public string Who() => "B"; }
  class Derived : Base { public new string Who() => "D"; }
answer: DB
explain: |
  Один объект, но для невиртуального и скрытого метода выбор идёт по типу переменной: `d` — `Derived`, `b` — `Base`.
```

```quiz
id: test-abstract-body
type: choice
part: "Абстрактные классы и методы"
prompt: Что верно для абстрактного класса?
options:
  - "Нельзя создать его объект, но он может содержать обычные методы, поля и конструкторы"
  - "Все его методы абстрактные"
  - "У него не может быть конструктора"
  - "От него нельзя наследоваться"
answer: 0
explain: |
  Абстрактный класс — частично готовый каркас: общая логика плюс абстрактные шаги для наследников.
```

```quiz
id: test-abstract-must-override
type: choice
part: "Абстрактные классы и методы"
prompt: Что должен сделать неабстрактный наследник абстрактного класса?
options:
  - "Реализовать все абстрактные члены через override"
  - "Ничего, это необязательно"
  - "Объявить конструктор без параметров"
  - "Переопределить все методы базового класса"
answer: 0
explain: |
  Иначе у объекта остался бы метод без реализации. Альтернатива — сделать наследника тоже абстрактным.
```

```quiz
id: test-template-method
type: output
part: "Абстрактные классы и методы"
prompt: Что выведет код?
code: |
  Greeter g = new RussianGreeter();
  Console.WriteLine(g.Greet("Анна"));

  abstract class Greeter
  {
      protected abstract string Hello();
      public string Greet(string name) => $"{Hello()}, {name}!";
  }

  class RussianGreeter : Greeter
  {
      protected override string Hello() => "Привет";
  }
answer: Привет, Анна!
explain: |
  Общий метод `Greet` в базовом классе вызывает абстрактный шаг `Hello`, а реализация берётся из наследника.
```

```quiz
id: test-overload-compile-time
type: choice
part: "Перегрузка и переопределение"
prompt: Когда выбирается перегрузка метода?
options:
  - "При компиляции, по объявленным типам аргументов"
  - "Во время выполнения, по настоящим типам объектов"
  - "Случайно"
  - "По порядку объявления методов"
answer: 0
explain: |
  В отличие от переопределения, перегрузка — решение компилятора. Поэтому `Feed(Animal)` выберется для переменной `Animal`, даже если в ней собака.
```

```quiz
id: test-sealed-override
type: choice
part: "Перегрузка и переопределение"
prompt: Что делает `sealed override`?
options:
  - "Переопределяет метод и запрещает переопределять его дальше в наследниках"
  - "Запрещает создавать объекты класса"
  - "Делает метод статическим"
  - "Скрывает метод базового класса"
answer: 0
explain: |
  Наследники этого класса получат эту версию метода и не смогут её заменить.
```

```quiz
id: test-overload-mixed
type: output
part: "Перегрузка и переопределение"
prompt: Что выведет код?
code: |
  Shape s = new Circle();
  Console.WriteLine(Describe(s) + " " + s.Name());

  string Describe(Shape x) => "фигура";
  string Describe(Circle x) => "круг";

  class Shape { public virtual string Name() => "shape"; }
  class Circle : Shape { public override string Name() => "circle"; }
answer: фигура circle
explain: |
  Перегрузку `Describe` выбрал компилятор по типу переменной `Shape`. Виртуальный `Name` выбран при выполнении по типу объекта `Circle`.
```

```quiz
id: test-covariant-return
type: choice
part: "Перегрузка и переопределение"
level: middle
prompt: Что разрешают ковариантные типы результата (C# 9)?
options:
  - "Переопределению возвращать более конкретный тип, чем базовый метод"
  - "Перегружать методы по типу результата"
  - "Возвращать несколько значений"
  - "Переопределять невиртуальные методы"
answer: 0
explain: |
  Например, `virtual Animal Clone()` можно переопределить как `override Dog Clone()`, и вызывающему коду с `Dog` не нужно приведение.
```

::::
