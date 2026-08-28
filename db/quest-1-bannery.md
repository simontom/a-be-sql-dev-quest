# Specifikace zadání: Sledování účinnosti bannerových kampaní

## 1. Přehled a kontext
Cílem tohoto úkolu je navrhnout datový model a analytické SQL dotazy pro **sledování účinnosti a bilance bannerových kampaní** v prostředí e-shopu.

Bannery (reklamní proužky) jsou umísťovány na externí weby (např. **seznam.cz**, **heureka.cz**) do konkrétních **bannerových pozic**. 
- Každý web může mít několik bannerových pozic.
- Bannerové pozice i bannery mají definované rozlišení v pixelech (**rozměry**).
- Banner lze umístit **výhradně** do bannerové pozice stejné velikosti.
- Pozice mají dva typy zpoplatnění: **platba za den (FIX)** nebo **platba za proklik (PPC)** (je dáno na každé pozici).
- **Kampaň** definuje časově ohraničené rozmístění vybraných bannerů do pozic na různých webech.
- **Zakázka (objednávka)** v e-shopu uchovává informaci o datu vytvoření, celkové ceně, marži a primárně o **bannerech**, přes které zákazník přišel a nakoupil.

---

## 2. Detailní požadavky na řešení

### a) Datový model (ERD / Relační schéma)
Navrhněte kompletní relaci tabulek včetně primárních a cizích klíčů a datových typů.
Model musí pokrývat:
- **Weby** (`Web`) - id, název, URL.
- **Bannerové pozice** (`BannerPosition`) - id, web_id, width, height, pricing_type (DAY / CLICK), price.
- **Bannery** (`Banner`) - id, name, width, height, target_url.
- **Kampaně** (`Campaign`) - id, name, start_date, end_date.
- **Nasazení bannerů v kampani** (`CampaignPlacement`) - vazba mezi kampaní, bannerem a pozicí v daném časovém rozmezí.
- **Prokliky** (`BannerClick`) - evidence prokliků s časovým razítkem (nutné pro vyčíslení PPC nákladů).
- **Zakázky / Objednávky** (`Order`) - id, order_date, total_price, margin.
- **Vazba zakázky na bannery** (`OrderBanner`) - vazba M:N pro zachování atribuce (z jakých bannerů zákazník přišel).

### b) SQL Select 1: Bilance kampaní
Napište SQL dotaz, který vypíše **všechny kampaně** a jejich **bilanci** (`výnos - náklady`):
- **Výnos**: Suma marží ze zakázek přiřazených bannerům v dané kampani.
- **Náklady**: Součet fixních nákladů za dny trvání pozic placených za den + součet nákladů za prokliky na pozicích placených za proklik během trvání kampaně.
- **Řazení**: Od **nejvýhodnější** po **nejméně výhodnou** (sestupně podle bilance).

### c) SQL Select 2: Denní bilance denně placených bannerů
Napište SQL dotaz, který pro jednotlivé dny v týdnu vyčíslí celkovou bilanci všech bannerů **placených za den**:
- Výsledek musí mít přesně **7 řádků** (pro jednotlivé dny v týdnu).
- **Řazení**: Pevně od **pondělí do neděle** (1 až 7).

### d) Návrh indexů
Navrhněte a odůvodněte databázové indexy pro:
- Zrychlení analytických dotazů z bodů **b)** a **c)**.
- Doplnění indexů především na tabulkách s **vysokým počtem záznamů** (`Order`, `OrderBanner`, `BannerClick`, možná ještě další, zatím nevím).
- Návrh dalších indexů potřebných pro podporu business logiky.

### e) Návrh integrity constraints
Navrhněte sadu databázových omezení (**Constraints**):
- **Foreign Keys** pro zajištění referenční integrity.
- **Check Constraints**:
  - Nezápustnost/kladnost cen, marží a rozměrů (`width > 0 AND height > 0`).
  - Konzistence datumu kampaně (`start_date <= end_date`).
  - Shoda rozměrů banneru a pozice při nasazení.
  - Omezení na povolené typy účtování (enum / check).

---

## 3. Pokyny pro implementaci
- Výstupní kód připravte ve standardním SQL pro MS SQL Server.
- Přidejte komentáře k logice výpočtu nákladů a atribuce marže.
