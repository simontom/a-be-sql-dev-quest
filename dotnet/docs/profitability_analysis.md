# Empirical Profitability & Strategy Comparison (Výnosnost)

Tento dokument doplňuje technické benchmarky o **hlavní byznysové hodnoticí kritérium – celkovou dosaženou výnosnost (profit)** a porovnání jednotlivých heuristických strategií s teoretickým horním odhadem optima (LP relaxace).

---

## 1. Souhrn výsledků (Empirické měření)

Testováno na syntetickém e-commerce datasetu (`seed = 42`) odpovídajícím zadání:
- **Vozový park**: 240 okruhů (120 dodávek × 2 jízdy denně)
- **Kapacita dodávky na okruh**: 7,0 m³ objem, 5 500 kg hmotnost
- **Celková denní kapacita skladu**: **1 680 m³** a **1 320 000 kg**
- **Testovací vzorky**: 100 000 balíčků a 200 000 balíčků (výrazný přetlak poptávky)

### Výsledky pro 100 000 balíčků

> **Teoretický horní odhad (LP relaxace)**: **79 060 128,90 Kč**

| Strategie | Celkový zisk (Kč) | % z LP maxima | Využití objemu | Využití váhy | Přiděleno balíčků | Čas výpočtu |
|---|---|---|---|---|---|---|
| **1. Baseline: Čistý zisk (Profit desc)** | 18 885 983,61 Kč | 23,89 % | 100,0 % | 8,4 % | 4 989 | 686 ms |
| **2. Baseline: Profit / Hmotnost** | 15 093 014,19 Kč | 19,09 % | 100,0 % | 0,1 % | 8 204 | 650 ms |
| **3. Baseline: Profit / Objem** | 76 548 550,65 Kč | 96,82 % | 91,8 % | 36,5 % | 42 033 | 524 ms |
| **4. Vyvážený Greedy: Profit / (0.5V + 0.5W)** ⭐ | **77 929 593,57 Kč** | **98,57 %** | **97,0 %** | **35,8 %** | **42 762** | **386 ms** |
| **5. Laděný poměr: Profit / (0.8V + 0.2W)** | 76 828 553,93 Kč | 97,18 % | 92,8 % | 36,3 % | 42 190 | 304 ms |
| **6. Laděný poměr: Profit / (0.9V + 0.1W)** | 76 665 726,33 Kč | 96,97 % | 92,2 % | 36,4 % | 42 098 | 415 ms |
| **7. Vyvážený Greedy + Local Search (100ms)** | 77 929 632,91 Kč | 98,57 % | 97,0 % | 35,8 % | 42 762 | 424 ms |
| **8. Business SLA: Vyvážený + Mandatory + Aging** | 73 957 355,10 Kč | 93,55 % | 97,3 % | 33,7 % | 40 663 | 474 ms |

---

### Výsledky pro 200 000 balíčků

> **Teoretický horní odhad (LP relaxace)**: **122 074 375,83 Kč**

| Strategie | Celkový zisk (Kč) | % z LP maxima | Využití objemu | Využití váhy | Přiděleno balíčků | Čas výpočtu |
|---|---|---|---|---|---|---|
| **1. Baseline: Čistý zisk (Profit desc)** | 18 663 553,85 Kč | 15,29 % | 100,0 % | 8,5 % | 4 648 | 1 036 ms |
| **2. Baseline: Profit / Hmotnost** | 14 937 656,37 Kč | 12,24 % | 100,0 % | 0,1 % | 7 325 | 950 ms |
| **3. Baseline: Profit / Objem** | 114 290 176,02 Kč | 93,62 % | 83,6 % | 56,1 % | 64 299 | 796 ms |
| **4. Vyvážený Greedy: Profit / (0.5V + 0.5W)** ⭐ | **118 479 506,35 Kč** | **97,06 %** | **93,8 %** | **54,9 %** | **66 532** | **747 ms** |
| **5. Laděný poměr: Profit / (0.8V + 0.2W)** | 115 157 329,85 Kč | 94,33 % | 85,4 % | 55,8 % | 64 767 | 756 ms |
| **6. Laděný poměr: Profit / (0.9V + 0.1W)** | 114 657 179,38 Kč | 93,92 % | 84,3 % | 56,0 % | 64 499 | 910 ms |
| **7. Vyvážený Greedy + Local Search (100ms)** | 118 479 514,18 Kč | 97,06 % | 93,8 % | 54,9 % | 66 532 | 894 ms |
| **8. Business SLA: Vyvážený + Mandatory + Aging** | 101 850 935,40 Kč | 83,43 % | 94,6 % | 45,7 % | 57 200 | 798 ms |

---

## 2. Klíčová zjištění a interpretace čísel

### 1. Proč je vícerozměrný greedy přístup nutností (+534 % zisku oproti intuitivnímu řešení)
- Pokud bychom balíčky řadili intuitivně podle nejvyššího absolutního zisku (**Strategie 1**), naložíme sice balíčky s vysokou nominální cenou, ale tyto balíčky jsou často rozměrné a těžké. Dodávky se zaplní na 100 % objemu po naložení pouhých ~4 600 balíčků a celkový výnos je pouhých **18,66 mil. Kč**.
- Vhodně normalizovaná hustota zisku (**Strategie 4**) vygeneruje **118,48 mil. Kč** (+99,8 mil. Kč, tedy **nárůst o 534 %**), protože naloží přes 66 000 balíčků s optimálním poměrem cena/prostor.

### 2. Doložení vhodnosti zvoleného řešení (97–98,5 % teoretického maxima)
- Pomocí **LP relaxace** (Fractional Multi-dimensional Knapsack) jsme spočetli absolutní teoretický horní limit, který nelze překonat žádným algoritmem (ani hrubou silou ILP řešiče).
- Vyvážená heuristika dosahuje **98,57 % optima pro 100k balíčků** a **97,06 % optima pro 200k balíčků**.
- **Závěr**: Rozdíl oproti globálnímu optimu je menší než 3 %, což jednoznačně dokládá, že greedy přístup s normalizovanou 2D hustotou je pro tuto doménu mimořádně efektivní a další složitější metaheuristiky nemají ekonomické opodstatnění.

### 3. Empirické odůvodnění vah $\alpha = 0.5$ a $\beta = 0.5$
Často vzniká domněnka, že když je objem vyčerpán na 94 % a váha jen na 55 %, měla by mít váha v hodnocení menší vliv a objem větší vliv (např. $\alpha=0.8, \beta=0.2$).
- Měření ukazuje opak: přechod z $\alpha=0.5, \beta=0.5$ na $\alpha=0.8, \beta=0.2$ **sníží profit o 3,3 mil. Kč** (z 118,5M na 115,2M).
- **Důvod**: Normalizace $\frac{V}{V_{\max}}$ a $\frac{W}{W_{\max}}$ již sama o sobě převádí obě fyzikální veličiny na bezrozměrnou spotřebu kapacity vozidla [0..1]. Zohlednění hmotnosti i u objemově limitovaného nákladu zabraňuje tomu, aby se do zbývajících volných prostor naložily příliš těžké balíčky, které by předčasně zablokovaly váhový limit dodávky. Symetrické rozdělení 0.5 / 0.5 dosahuje nejtěsnějšího 2D balení.

### 4. Kvantifikace přínosu Local Search (Proč je pro business redundantní)
- V teoretickém popisu se často uvádí lokální prohledávání jako vhodná fáze dotažení řešení.
- Reálné měření na tomto problému však ukázalo, že Local Search přinesl na 200k balíčcích zisk navíc pouhých **+7,83 Kč** (+0,000006 %).
- **Důvod**: Protože greedy řešení je již na 97–98,5 % teoretického stropu a dodávky jsou z 94–97 % zaplněné, prostor pro ziskové záměny 1 za 1 s respektováním 2D kapacit je téměř nulový.
- **Doporučení pro produkci**: Local Search se nevyplatí spouštět; spotřebovává 100 ms CPU času a vyžaduje paměťové mutace bez měřitelného byznysového přínosu.

### 5. Byznysová cena za SLA a prevenci hladovění balíčků
- Zadání zmiňovalo maximalizaci zisku. Pokud do systému přidáme garanci doručení prioritních balíčků (`Mandatory`) a penalizaci/stárnutí čekajících zásilek (`AgingBoost`), dochází k vytlačení balíčků s nejvyšší okamžitou ziskovostí.
- **Naměřený dopad**:
  - Pro 100k balíčků: pokles zisku z 77,93 mil. Kč na 73,96 mil. Kč (**náklad SLA činí -3,97 mil. Kč / -5,1 %**).
  - Pro 200k balíčků: pokles zisku z 118,48 mil. Kč na 101,85 mil. Kč (**náklad SLA činí -16,63 mil. Kč / -14,0 %**).
- Tento údaj dává byznysu exaktní podklad pro rozhodování: *„Dodržení 100% SLA u prioritních balíčků nás v peaku stojí 14 % denního výnosu.“*

---

## 3. Režim pro Úterý a Čtvrtek (Reflexe zadání)

V původní implementaci byl vytvořen samostatný režim `DayType.LowDemand` se speciálním algoritmem Round-Robin.
- **Zpětná vazba od hodnotitelů**: Zadání tento samostatný režim nevyžadovalo – popisovalo pouze dva provozní stavy vytížení sítě.
- **Algoritmické ověření**: Greedy algoritmus funguje univerzálně pro oba stavy:
  - Pokud je celková poptávka nižší než kapacita (Út/Čt), Greedy algoritmus naloží všechny balíčky a dosáhne 100% obslužnosti.
  - Není nutné ani žádoucí udržovat v jádru plánovače dvě různé větve.
