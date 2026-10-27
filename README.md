# Paint++

Edytor wielokątów z ograniczeniami geometrycznymi, segmentami Béziera i łukami okręgów.
Projekt 1 z Grafiki Komputerowej. Wariant L–Z: ograniczenie prostopadłości, domyślna ciągłość C1.

## Uruchomienie

Wymagany .NET 10 SDK.

```
dotnet run --project src/Paint++.App
dotnet test
```

## Biblioteki

- **Raylib-cs** – binding C# do biblioteki raylib. Daje okno, obsługę myszy i klawiatury oraz rysowanie
  prymitywów. Wybrana, bo jest prosta, nie narzuca architektury i pozwala rysować pojedyncze piksele,
  co jest potrzebne do własnego algorytmu Bresenhama.
- **ImGui.NET + rlImgui-cs** – interfejs w trybie natychmiastowym (panel boczny, menu kontekstowe,
  okno edycji długości). Kod UI jest zwykłą funkcją wywoływaną co klatkę, bez osobnego modelu widoku.

Projekt `Paint++.Core` nie zależy od żadnej biblioteki graficznej – zawiera model, geometrię,
ograniczenia i algorytmy rasteryzacji i jest w całości pokryty testami (xUnit).

## Obsługa

| Akcja | Sterowanie |
|---|---|
| Przesunięcie wierzchołka lub punktu kontrolnego | LPM na punkcie + przeciągnięcie |
| Przesunięcie całego wielokąta | LPM na krawędzi + przeciągnięcie |
| Zaznaczenie krawędzi | LPM na krawędzi |
| Dodanie krawędzi do zaznaczenia | Cmd (macOS) / Win + LPM |
| Menu wierzchołka | PPM na wierzchołku |
| Menu krawędzi | PPM na krawędzi lub jej punkcie kontrolnym |
| Wybór algorytmu rysowania odcinków | przyciski w panelu bocznym |
| Pomoc (ta dokumentacja) | F1 lub przycisk w panelu bocznym |

Menu wierzchołka: usunięcie wierzchołka (wielokąt musi mieć więcej niż 3), wybór ciągłości G0/G1/C1
(tylko gdy sąsiaduje z krzywą).

Menu krawędzi: dodanie wierzchołka w środku, zmiana na odcinek / krzywą Béziera / łuk, ustawienie
długości, prostopadłość do zaznaczonej krawędzi, usunięcie istniejących ograniczeń.

Dodanie prostopadłości: zaznaczyć pierwszą krawędź LPM, następnie PPM na drugiej i wybrać
„Prostopadła do zaznaczonej”. Krawędzie mogą należeć do różnych wielokątów.

Ikony przy środkach krawędzi pokazują aktywne ograniczenia: długość z wartością oraz
prostopadłość z numerem pary.

## Założenia

- Długość i prostopadłość dotyczą tylko krawędzi będących odcinkami. Zmiana krawędzi na krzywą lub łuk
  usuwa jej ograniczenia.
- Dodanie lub usunięcie wierzchołka usuwa ograniczenia krawędzi, które przy tym znikają.
  Nowe krawędzie są odcinkami.
- Krawędź nie może być prostopadła do samej siebie. Prostopadłość krawędzi sąsiednich jest dozwolona.
- Ograniczenie, którego nie da się spełnić razem z istniejącymi, jest odrzucane z komunikatem.
- Jeśli przy przeciąganiu ograniczeń nie da się spełnić, cały wielokąt jest przesuwany za kursorem
  (pojawia się czerwony komunikat). Przesuwany punkt zawsze podąża za myszą.
- Ciągłość ma znaczenie tylko w wierzchołku sąsiadującym z krzywą Béziera lub łukiem.
  Przy zmianie krawędzi na krzywą Béziera jej końce dostają C1, o ile ciągłość nie była wcześniej
  ustawiona ręcznie.
- Przeciąganie wierzchołka nie przesuwa jego punktów kontrolnych; o ich ruchu decyduje tylko ciągłość.
- Łuk dopuszcza G0 i G1, przy czym G1 tylko na jednym końcu. Przy dwóch sąsiednich łukach z G1
  dopasowuje się łuk wychodzący.

## Model danych

Scena przechowuje listę wielokątów i jedną, globalną listę ograniczeń, dzięki czemu ograniczenie może
łączyć krawędzie różnych wielokątów. Wielokąt to cykliczna lista wierzchołków i krawędzi; krawędź
wskazuje swoje dwa wierzchołki i ma wymienny kształt (`LineShape`, `BezierShape` z dwoma punktami
kontrolnymi, `ArcShape`). Wierzchołki i punkty kontrolne są obiektami `ScenePoint`, a ograniczenia
trzymają referencje do krawędzi, więc solver przesuwa bezpośrednio te same punkty, które są rysowane.

## Algorytmy

### Ograniczenia

Każde ograniczenie implementuje interfejs `IConstraint`:

```csharp
bool IsSatisfied(float epsilon);
void Project(IReadOnlySet<ScenePoint> pinned);   // popraw swoje punkty, nie ruszając przypiętych
```

Solver (`ConstraintSolver`) wykonuje iteracyjną relaksację: dopóki nie wszystkie ograniczenia są
spełnione, wywołuje kolejno `Project` każdego z nich, maksymalnie 100 iteracji (ε = 0,01).

Przeciąganie punktu (`SceneEditor.MovePoint`):

1. zapamiętanie pozycji wszystkich punktów sceny,
2. ustawienie punktu pod kursorem i przypięcie go,
3. uruchomienie solvera,
4. jeśli solver nie zbiegł – przywrócenie pozycji i przesunięcie całego wielokąta o wektor ruchu myszy.
   Przesunięcie nie narusza żadnego ograniczenia, więc stan zawsze pozostaje poprawny.

Dodanie ograniczenia działa tak samo, ale bez przypiętych punktów; przy braku zbieżności
ograniczenie jest wycofywane.

Poszczególne ograniczenia:

- **Długość** – końce krawędzi przesuwane wzdłuż jej kierunku do zadanej długości: symetrycznie
  względem środka albo od końca przypiętego.
- **Prostopadłość** – obracana jest krawędź z mniejszą liczbą przypiętych końców (wokół środka lub
  przypiętego końca), z zachowaniem jej długości.
- **Ciągłość G1/C1** – w wierzchołku porównywane są wektory styczne krawędzi po obu jego stronach.
  C1 wymaga równych wektorów, G1 tylko równych kierunków. Poprawiany jest najpierw wolny punkt kontrolny, a dopiero gdy go nie ma – drugi koniec
  sąsiedniego odcinka. Dla C1 z odcinkiem punkt kontrolny leży w 1/3 długości odcinka.
  Ograniczenia ciągłości są odtwarzane z ustawień wierzchołków po każdej zmianie kształtu krawędzi.

### Łuki

Łuk nie jest ograniczeniem w solverze – jego geometria jest wyliczana przy rysowaniu na podstawie
sąsiadów. Przy G0 na obu końcach rysowany jest półokrąg o promieniu |AB|/2. Przy G1 na jednym końcu
środek okręgu leży na prostej prostopadłej do stycznej w tym końcu, w takim miejscu, żeby okrąg
przechodził przez oba końce krawędzi. Gdy styczna jest równoległa do cięciwy, rysowany jest odcinek.

### Rysowanie

- **Odcinki** – do wyboru linia biblioteczna raylib albo własny, całkowitoliczbowy algorytm Bresenhama
  (wszystkie oktanty).
- **Krzywa Béziera** – przejście do bazy potęgowej `P(t) = At³ + Bt² + Ct + D` i wyznaczanie kolejnych
  punktów metodą różnic w przód (trzy dodawania wektorów na punkt). Liczba kroków jest proporcjonalna
  do długości wieloboku kontrolnego, a punkty łączone są linią biblioteczną.
- **Łuk** – próbkowany do łamanej rysowanej linią biblioteczną.

## Scena początkowa

Scena jest budowana w `DemoScene.cs` przez `SceneBuilder`:

```csharp
var quad = builder.Polygon((100, 100), (350, 120), (300, 350), (120, 300));
builder.Length(quad.Edges[0], 100);
builder.Perpendicular(quad.Edges[1], other.Edges[0]);
builder.Bezier(quad.Edges[2], (320, 400), (150, 380));
builder.SetContinuity(quad.Vertices[2], Continuity.G1);
builder.Arc(quad.Edges[3]);
var scene = builder.Build();
```

`Build()` uruchamia solver, więc ograniczenia są spełnione od startu; sprzeczne ograniczenia w scenie
kończą się wyjątkiem.
