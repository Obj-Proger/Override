---
id: csharp.oop.static
title: static и методы расширения
level: junior
tracks: [backend, fullstack]
minutes: 25
order: 5
requires: [csharp.oop.access]
quickCheck: [static-counter, static-no-this, static-ctor-once, extension-call]
testMinutes: 6
---

::::warmup

```quiz
id: static-counter
type: output
prompt: Что выведет код?
code: |
  new User();
  new User();
  new User();
  Console.WriteLine(User.Created);

  class User
  {
      public static int Created;

      public User()
      {
          Created++;
      }
  }
answer: "3"
explain: |
  Статическое поле одно на весь класс, а не своё у каждого объекта. Все три конструктора увеличивают один и тот же счётчик.
variants:
  - code: |
      var a = new User();
      var b = new User();
      Console.WriteLine(a.Visits + b.Visits);

      class User
      {
          public int Visits;

          public User()
          {
              Visits++;
          }
      }
    answer: "2"
    explain: |
      Здесь поле обычное: у каждого объекта своё, и каждое равно 1.
```

::::

::::part Статические члены

**Статический член** принадлежит самому типу, а не объекту. Обращаются к нему через имя типа:

```csharp
Console.WriteLine(Math.Max(3, 7));  // Max — статический метод
Console.WriteLine(int.MaxValue);    // MaxValue — статическая константа
Console.WriteLine(DateTime.Now);    // Now — статическое свойство
```

- **Статическое поле** существует в одном экземпляре на весь тип. Все объекты видят одно значение.
- **Статический метод** вызывается без объекта, поэтому в нём нет `this` и нельзя обратиться к полям и методам экземпляра напрямую — непонятно, какого именно объекта.
- Обычный метод, наоборот, может пользоваться статическими членами своего класса.

```csharp
class Temperature
{
    public double Celsius { get; set; }

    public static double ToFahrenheit(double celsius) => celsius * 9 / 5 + 32;  // объект не нужен

    public double Fahrenheit => ToFahrenheit(Celsius);  // обычное свойство вызывает статический метод
}
```

Делать метод статическим имеет смысл, если ему не нужно состояние объекта — он работает только с параметрами.

```quiz
id: static-no-this
type: find-bug
prompt: Код не компилируется. В какой строке ошибка?
code: |
  class Circle
  {
      public double Radius { get; set; }

      public static double Area()
      {
          return Math.PI * Radius * Radius;
      }
  }
answer: 7
explain: |
  Статический метод вызывается без объекта, и у него нет `this`. Чей `Radius` брать — неизвестно. Нужно либо сделать метод обычным, либо передавать радиус параметром.
```

::::

::::part Статические классы

**Статический класс** содержит только статические члены. Создать его объект и наследоваться от него нельзя. Это просто контейнер для функций, которым не нужно состояние:

```csharp
static class Geometry
{
    public static double CircleArea(double radius) => Math.PI * radius * radius;
    public static double Hypotenuse(double a, double b) => Math.Sqrt(a * a + b * b);
}

double area = Geometry.CircleArea(2);
var g = new Geometry();  // ошибка: объект статического класса создать нельзя
```

Из стандартной библиотеки статические классы — `Math`, `Console`, `File`, `Path`, `Convert`.

**Осторожно со статическим состоянием.** Статическое поле живёт всё время работы программы и общее для всех: для всех пользователей веб-приложения и для всех потоков. Поэтому:

- статическое изменяемое поле легко превращается в глобальную переменную, которую меняют из разных мест;
- в многопоточном коде одновременная запись в него приводит к гонкам (тема многопоточности);
- объекты, на которые ссылается статическое поле, никогда не освобождаются сборщиком мусора;
- код, завязанный на статическое состояние, трудно тестировать.

Статическими обычно делают константы, кэши «только для чтения» и вспомогательные функции без состояния.

```quiz
id: static-class-new
type: choice
prompt: Что верно для статического класса?
options:
  - "Все его члены статические, создать объект и наследоваться от него нельзя"
  - "Он создаётся один раз при первом new"
  - "В нём могут быть обычные поля, если они приватные"
  - "От него можно наследоваться, но нельзя создавать объекты"
answer: 0
explain: |
  Статический класс — набор статических членов. Компилятор запрещает в нём члены экземпляра, `new` и наследование.
```

::::

::::part Статический конструктор

**Статический конструктор** инициализирует статические поля. Он:

- объявляется со словом `static`, без модификатора доступа и параметров;
- вызывается средой автоматически, **один раз**, перед первым обращением к типу: к статическому члену или при создании первого объекта;
- вызвать его вручную нельзя.

```csharp
class Config
{
    public static readonly string Environment;

    static Config()
    {
        Console.WriteLine("загрузка настроек");
        Environment = "Production";
    }
}

Console.WriteLine(Config.Environment);  // "загрузка настроек", затем Production
Console.WriteLine(Config.Environment);  // только Production: конструктор уже отработал
```

Если статический конструктор бросает исключение, CLR заворачивает его в **`TypeInitializationException`**, и **тип становится непригодным до конца работы программы**: каждое следующее обращение к нему бросает то же исключение. Поэтому в статических конструкторах избегают кода, который может упасть: чтения файлов, сети.

Инициализаторы статических полей выполняются до тела статического конструктора, в порядке объявления.

```quiz
id: static-ctor-once
type: output
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  var a = new Service();
  var b = new Service();

  class Service
  {
      static Service()
      {
          Console.Write("S");
      }

      public Service()
      {
          Console.Write("I");
      }
  }
answer: SII
explain: |
  Статический конструктор выполняется один раз — перед созданием первого объекта. Обычный конструктор — при каждом `new`.
variants:
  - code: |
      Console.Write(Service.Name);
      Console.Write(Service.Name);

      class Service
      {
          public static string Name = "N";

          static Service()
          {
              Console.Write("S");
          }
      }
    answer: SNN
    explain: |
      Первое обращение к статическому полю запускает статический конструктор, второе — уже нет.
```

```quiz
id: type-init-exception
type: choice
prompt: Что произойдёт, если статический конструктор класса бросит исключение?
options:
  - "Будет брошено TypeInitializationException, и класс останется непригодным до конца работы программы"
  - "Статический конструктор вызовется снова при следующем обращении"
  - "Исключение будет проигнорировано"
  - "Программа не скомпилируется"
answer: 0
explain: |
  Среда запоминает, что инициализация типа провалилась, и при каждом обращении бросает то же исключение. Повторной попытки не будет.
```

::::

::::part Методы расширения

**Метод расширения** добавляет к существующему типу метод, который вызывается как обычный метод объекта, хотя тип вы не меняли и даже не имеете к нему доступа:

```csharp
static class StringExtensions
{
    public static bool IsBlank(this string text) => string.IsNullOrWhiteSpace(text);

    public static string Shout(this string text) => text.ToUpper() + "!";
}

Console.WriteLine("  ".IsBlank());    // True
Console.WriteLine("привет".Shout());  // ПРИВЕТ!
```

Правила:

- метод статический и объявлен в **статическом** классе;
- первый параметр помечен **`this`** — это объект, у которого метод «вызывают»;
- компилятор превращает `"привет".Shout()` в обычный вызов `StringExtensions.Shout("привет")`;
- метод виден там, где подключено его пространство имён через `using`.

Метод расширения не получает доступа к приватным членам типа: это обычный внешний статический метод. Если у типа есть собственный метод с той же сигнатурой, побеждает собственный.

На методах расширения построен LINQ: `Where`, `Select`, `OrderBy` — методы расширения для `IEnumerable<T>`.

С C# 14 есть и новый синтаксис — блоки `extension`, где можно объявлять ещё и свойства расширения. Классическая форма с `this` работает по-прежнему и встречается в большинстве кода.

```quiz
id: extension-call
type: output
prompt: Что выведет код?
code: |
  Console.WriteLine(7.IsEven());

  static class IntExtensions
  {
      public static bool IsEven(this int n) => n % 2 == 0;
  }
answer: "False"
explain: |
  `7.IsEven()` компилируется в `IntExtensions.IsEven(7)`. 7 нечётное.
variants:
  - code: |
      Console.WriteLine("кот".Repeat(3));

      static class StringExtensions
      {
          public static string Repeat(this string text, int times) => string.Concat(Enumerable.Repeat(text, times));
      }
    answer: коткоткот
    explain: |
      Первый параметр с `this` — сама строка, остальные передаются в скобках: `times` равно 3.
```

:::level middle

**`beforefieldinit`.** Если у класса нет явного статического конструктора, а есть только инициализаторы статических полей, компилятор помечает тип флагом `beforefieldinit`. Тогда среда может инициализировать поля раньше первого обращения или позже, но не позднее первого чтения поля. С явным статическим конструктором момент строго определён. На собеседованиях это иногда спрашивают в связи с порядком вывода в консоль.

**Статика в обобщённых типах.** У `Cache<int>` и `Cache<string>` — разные статические поля: каждый закрытый обобщённый тип — отдельный тип. Подробнее — в теме про generics.

**Почему статику не любят в больших приложениях.** Статический вызов `DateTime.Now` или `File.ReadAllText` нельзя подменить в тесте. Поэтому зависимости передают через интерфейсы и внедрение зависимостей (DI), а для времени в .NET 8 появился `TimeProvider`.

```quiz
id: generic-static
type: choice
level: middle
prompt: Сколько копий статического поля `Count` будет у `Cache<int>` и `Cache<string>`?
options:
  - "Две: каждый закрытый обобщённый тип имеет свои статические поля"
  - "Одна на все варианты Cache<T>"
  - "Ни одной: в обобщённых типах статика запрещена"
  - "Зависит от числа объектов"
answer: 0
explain: |
  `Cache<int>` и `Cache<string>` — разные типы во время выполнения, у каждого свои статические поля.
```

:::

::::

::::practice

```quiz
id: extension-task
type: code
lang: csharp
prompt: |
  Допишите метод расширения: `Truncate` возвращает строку не длиннее `max` символов. Если строка длиннее, она обрезается и в конце ставится «…» так, чтобы общая длина была ровно `max`.
starter: |
  public static class StringExtensions
  {
      public static string Truncate(this string text, int max)
      {
      }
  }
tests: |
  Assert.Equal("кот", "кот".Truncate(5));
  Assert.Equal("прив…", "привет мир".Truncate(5));
  Assert.Equal("ровно", "ровно".Truncate(5));
solution: |
  public static class StringExtensions
  {
      public static string Truncate(this string text, int max)
      {
          if (text.Length <= max)
              return text;

          return text.Substring(0, max - 1) + "…";
      }
  }
explain: |
  Метод расширения объявлен в статическом классе, первый параметр помечен `this`. Вызывается как метод строки: `"текст".Truncate(5)`.
variants:
  - prompt: |
      Допишите метод расширения: `Clamp` возвращает число, ограниченное диапазоном от `min` до `max` включительно.
    starter: |
      public static class IntExtensions
      {
          public static int Clamp(this int value, int min, int max)
          {
          }
      }
    tests: |
      Assert.Equal(5, 5.Clamp(0, 10));
      Assert.Equal(0, (-3).Clamp(0, 10));
      Assert.Equal(10, 42.Clamp(0, 10));
    solution: |
      public static class IntExtensions
      {
          public static int Clamp(this int value, int min, int max)
          {
              if (value < min)
                  return min;
              if (value > max)
                  return max;
              return value;
          }
      }
    explain: |
      Для `int` тоже можно писать методы расширения. Отрицательный литерал берут в скобки: `(-3).Clamp(...)`, иначе минус применился бы к результату.
```

```quiz
id: extension-this
type: fill
prompt: Вставьте ключевое слово, чтобы метод стал методом расширения для `string`.
code: |
  public static int WordCount(___ string text) => text.Split(' ').Length;
answer: this
explain: |
  `this` перед первым параметром превращает статический метод статического класса в метод расширения.
```

::::

::::traps

- **Статическое поле одно на весь тип**, а не своё у каждого объекта.
- **В статическом методе нет `this`:** поля экземпляра напрямую недоступны.
- **Статический конструктор выполняется один раз** — перед первым обращением к типу.
- **Исключение в статическом конструкторе ломает тип навсегда:** `TypeInitializationException` при каждом обращении.
- **Статическое изменяемое состояние** — глобальная переменная: гонки, утечки, трудные тесты.
- **Метод расширения не видит приватных членов**, а собственный метод типа важнее расширения.

::::

::::summary

```quiz
id: static-recall
type: recall
prompt: Расскажите о статических членах, статических классах, статическом конструкторе и методах расширения.
answer: |
  **Статический член** принадлежит типу: поле одно на всех, метод вызывается без объекта и не имеет `this`. **Статический класс** содержит только статические члены, его нельзя создать и унаследовать: `Math`, `Console`.
  **Статический конструктор** без модификаторов и параметров вызывается средой один раз перед первым обращением к типу. Исключение в нём превращается в `TypeInitializationException`, и тип становится непригодным до конца работы программы.
  Статическое изменяемое состояние опасно: общее для всех потоков и пользователей, не освобождается, мешает тестам.
  **Метод расширения** — статический метод статического класса с `this` у первого параметра; вызывается как метод объекта, но компилируется в обычный статический вызов, приватных членов не видит. На них построен LINQ.
```

::::

::::test

```quiz
id: test-static-access
type: choice
part: "Статические члены"
prompt: Как обратиться к статическому методу `Parse` класса `Version`?
options:
  - "Version.Parse(...)"
  - "new Version().Parse(...)"
  - "this.Parse(...) из любого места"
  - "Parse(...) из любого места"
answer: 0
explain: |
  Статические члены вызываются через имя типа. Без имени — только изнутри самого класса.
```

```quiz
id: test-static-shared
type: output
part: "Статические члены"
prompt: Что выведет код?
code: |
  var a = new Player();
  var b = new Player();
  Player.Level = 5;
  Console.WriteLine(Player.Level);

  class Player
  {
      public static int Level = 1;
  }
answer: "5"
explain: |
  Статическое поле одно на весь тип. Объекты `a` и `b` тут ни при чём.
```

```quiz
id: test-instance-uses-static
type: choice
part: "Статические члены"
prompt: Может ли обычный метод класса вызвать статический метод того же класса?
options:
  - "Да"
  - "Нет, статический метод вызывается только из статического"
  - "Только через this"
  - "Только если класс статический"
answer: 0
explain: |
  Обычный метод может пользоваться и своими полями, и статическими членами. Ограничение действует только в обратную сторону.
```

```quiz
id: test-static-class-members
type: find-bug
part: "Статические классы"
prompt: Код не компилируется. В какой строке ошибка?
code: |
  static class Utils
  {
      public static int Twice(int x) => x * 2;

      public int Calls;
  }
answer: 5
explain: |
  В статическом классе разрешены только статические члены. Поле экземпляра `Calls` объявить нельзя.
```

```quiz
id: test-static-state-risk
type: choice
part: "Статические классы"
prompt: Чем опасно изменяемое статическое поле в веб-приложении?
options:
  - "Оно общее для всех пользователей и потоков: возможны гонки и утечки данных между запросами"
  - "Оно сбрасывается после каждого запроса"
  - "Его нельзя прочитать из контроллера"
  - "Оно хранится в браузере пользователя"
answer: 0
explain: |
  Статическое поле живёт весь срок работы процесса. Данные одного пользователя могут оказаться видны другому, а одновременная запись — испортиться.
```

```quiz
id: test-static-ctor-params
type: choice
part: "Статический конструктор"
prompt: Что из этого верно для статического конструктора?
options:
  - "У него нет параметров и модификатора доступа"
  - "Его можно вызвать явно: MyClass.MyClass()"
  - "Он вызывается при каждом создании объекта"
  - "У класса их может быть несколько"
answer: 0
explain: |
  Статический конструктор один, без параметров и модификаторов. Вызывает его среда, один раз.
```

```quiz
id: test-static-ctor-trigger
type: output
part: "Статический конструктор"
prompt: Что выведет код? `Console.Write` печатает без перевода строки.
code: |
  Console.Write("1");
  Console.Write(Box.Size);
  Console.Write("2");

  class Box
  {
      public static int Size;

      static Box()
      {
          Console.Write("S");
          Size = 9;
      }
  }
answer: 1S92
explain: |
  Статический конструктор с явным объявлением запускается непосредственно перед первым обращением к типу — между выводом «1» и чтением `Size`.
```

```quiz
id: test-type-init-repeat
type: choice
part: "Статический конструктор"
prompt: Статический конструктор упал с исключением. Что будет при следующем обращении к классу?
options:
  - "Снова TypeInitializationException, без повторной попытки"
  - "Статический конструктор выполнится заново"
  - "Поля будут иметь значения по умолчанию, ошибки не будет"
  - "Класс выгрузится из памяти"
answer: 0
explain: |
  Неудачная инициализация типа запоминается до конца работы процесса.
```

```quiz
id: test-extension-compiles
type: choice
part: "Методы расширения"
prompt: Во что компилятор превращает вызов метода расширения `"text".Shout()`?
options:
  - "В обычный статический вызов StringExtensions.Shout(\"text\")"
  - "В виртуальный вызов метода строки"
  - "В добавление метода к классу string во время выполнения"
  - "В вызов через рефлексию"
answer: 0
explain: |
  Методы расширения — удобная запись. Класс `string` не меняется, вызов статический.
```

```quiz
id: test-extension-private
type: choice
part: "Методы расширения"
prompt: Может ли метод расширения обратиться к приватному полю расширяемого класса?
options:
  - "Нет: это внешний статический метод"
  - "Да, всегда"
  - "Да, если класс в той же сборке"
  - "Да, если метод расширения объявлен как private"
answer: 0
explain: |
  Метод расширения видит только то, что доступно любому внешнему коду.
```

```quiz
id: test-extension-priority
type: output
part: "Методы расширения"
prompt: Что выведет код?
code: |
  Console.WriteLine(new Greeter().Hello());

  class Greeter
  {
      public string Hello() => "свой";
  }

  static class GreeterExtensions
  {
      public static string Hello(this Greeter greeter) => "расширение";
  }
answer: свой
explain: |
  Собственный метод типа с подходящей сигнатурой всегда важнее метода расширения. Расширение вызывается, только если подходящего метода у типа нет.
```

```quiz
id: test-before-field-init
type: choice
part: "Методы расширения"
level: middle
prompt: Что меняет наличие явного статического конструктора в момент инициализации статических полей?
options:
  - "Инициализация строго перед первым обращением к типу; без него (beforefieldinit) среда может сделать это раньше"
  - "Ничего"
  - "Поля инициализируются при запуске программы"
  - "Поля инициализируются только при создании объекта"
answer: 0
explain: |
  Без явного статического конструктора тип помечается `beforefieldinit`, и момент инициализации полей не фиксирован.
```

::::
