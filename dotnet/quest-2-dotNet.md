# Specifikace zadání: Doručení teleportem (Optimální plánování kapacity dodávek)

## 1. Přehled a logistický kontext
Cílem úlohy je návrh a implementace C# modulu pro **efektivní plánování přepravy zásilek do AlzaBoxů** z centrálního skladu.
Proces musí v omezeném časovém okně rozřadit balíčky do vozového parku tak, aby při přetlaku poptávky dosáhl **co nejvyšší celkové výnosnosti (profitu)**.

---

## 2. Vstupní parametry a business pravidla

### Sklad a vozový park
- **Počet dodávek v centrálním skladu**: **120 dodávek**.
- **Frekvence jízd**: Každá dodávka jezdí po okruhu **2x denně** (celkem **240 okruhů/jízd denně**).
- **Kapacita dodávky na 1 okruh**:
  - Maximální objem: **7 m³**
  - Maximální hmotnost nákladu: **5,5 tuny** (**5 500 kg**)
- **Využití kapacity dle dnů**:
  - **Úterý a Čtvrtek**: Kapacita dostačuje pro všechny balíčky.
  - **Ostatní dny (Po, St, Pá, So, Ne)**: Kapacita **nestačí** na všechny balíčky (dochází k přetlaku). Při každém okruhu je nutné dosáhnout **co nejvyšší výnosnosti**.

### Parametry balíčků / zásilek
- Množství pro jedno plánování: **řádově statisíce balíčků** (100 000+).
- Vlastnosti balíčku:
  - **Hmotnost**: v kg
  - **Objem**: v m³
  - **Výnosnost**: v Kč (předpokládáme kladné hodnoty)

### Omezení na výpočet
- **Přísné časové okno**: Výpočet musí být extrémně rychlý (řádově sekundy / stovky milisekund pro statisíce balíčků).

---

## 3. Požadavky na řešení a architekturu

### Kód v C#
- Implementace v **C# (.NET 8+)**.
- Zaměření na **výkon a paměťovou efektivitu** (využití `readonly struct`, zamezení zbytečným alokacím na heapu, efektivní řazení / paralelismus).
- Vhodně zvolený heuristický algoritmus pro **NP-těžký problém batohu pro více kontejnerů** (Multi-dimensional Knapsack Problem):
  - Např. **Greedy algoritmus** hodnocení poměru `Výnos / (Vážená kombinace Objemu a Hmotnosti)`.
  - Případně kombinace s **Local Search** / **Simulated Annealing** pro dooptimování.

### Textový popis (Součást výstupu)
- Vysvětlení pochopení problému a zvoleného přístupu.
- Popis stanovených omezení a zjednodušení.
- Stručné srovnání a diskuze dalších možných algoritmických přístupů.

---

## 4. Hodnocená kritéria
- Přístup k řešení a způsob uvažování.
- Čitelnost a struktura C# kódu.
- Rychlost výpočtu (časová složitost).
- Celková profitabilita (výnosnost) zvoleného postupu.
