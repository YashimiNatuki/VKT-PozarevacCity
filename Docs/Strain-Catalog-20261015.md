# VKT cannabis strain catalog — 2026-10-15

The catalog now represents cannabis strains, replacing the previous ornamental-flower theme. The eight existing inventory/save IDs remain in place. Existing seeds, harvested stock, plots, costs, reputation gates and fictional game timers are preserved at those IDs; an existing item takes the new displayed strain name. No new live trade IDs were appended, so no garden-array migration is needed.

All currency, growth durations, watering intervals, inventory yields and sale modifiers are **fictional game balance**. They are not cultivation instructions, real production measurements or real market quotations. The cards provide only general provenance/history attributed to public breeder sources. They contain no real potency/dose recommendations or cultivation optimization.

## Trade and illustration mapping

| Stable ID / atlas slot | Strain | Primary source |
| --- | --- | --- |
| 0 | Amnesia Haze | [Royal Queen Seeds](https://www.royalqueenseeds.com/feminized-cannabis-seeds/115-amnesia-haze.html) |
| 1 | OG Kush | [Royal Queen Seeds](https://www.royalqueenseeds.com/feminized-cannabis-seeds/149-og-kush.html) |
| 2 | Wedding Cake | [Barney's Farm](https://www.barneysfarm.com/wedding-cake-511) |
| 3 | Purple Haze | [White Label / Sensi Seeds](https://sensiseeds.com/en/feminized-seeds/whitelabel/purple-haze-feminised) |
| 4 | AK-47 | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/ak-47) |
| 5 | White Widow | [Dutch Passion](https://dutch-passion.com/en/cannabis-seeds/white-widow) |
| 6 | Sour Diesel | [Royal Queen Seeds](https://www.royalqueenseeds.com/us/feminized-cannabis-seeds/185-sour-diesel.html) |
| 7 | Green Gelato | [Royal Queen Seeds](https://www.royalqueenseeds.com/feminized-cannabis-seeds/416-green-gelato.html) |

First five remain available initially. IDs 5/6/7 retain reputation gates 30/60/100. Atlas UV indices 0–7 remain in the existing two-column/four-row layout. Any replacement atlas must use this order. Art is illustrative; it is not an authenticated photograph or proof of a phenotype. No breeder photographs have been copied.

Green Gelato is the exact RQS catalog expression chosen for the Gelato family; it is not presented as interchangeable with every Gelato cut.

## Eight additional informational cards

These entries have no trade/save ID and do not silently extend inventories:

| Card | Strain | Primary source |
| --- | --- | --- |
| 8 | Chronic | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/chronic) |
| 9 | White Russian | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/white-russian) |
| 10 | Bubble Gum | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/bubble-gum) |
| 11 | Kali Mist | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/kali-mist) |
| 12 | Warlock | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/warlock) |
| 13 | Serious Kush | [Serious Seeds](https://www.seriousseeds.com/cannabis-seeds/serious-kush) |
| 14 | Blueberry | [Dutch Passion](https://dutch-passion.com/en/cannabis-seeds/blueberry) |
| 15 | Orange Bud | [Dutch Passion](https://dutch-passion.com/en/cannabis-seeds/orange-bud) |

## Attribution and uncertainty

Sources were checked publicly on 2026-10-15; no API keys, account login, model download or purchase was needed. Their pages are commercial breeder publications, used here only as attributed primary accounts of their own lines. They do not establish universal historical truth about every strain sharing a name. Cannabis strain names are not sixteen separate botanical species.

OG Kush's RQS page itself acknowledges uncertainty about the original genealogy. Wedding Cake has conflicting lineage descriptions even within Barney's Farm: the product page lists Cherry Pie/Girl Scout Cookies/OG Kush, while its [history article](https://www.barneysfarm.com/us/blog/about-wedding-cake-strain) discusses the original Triangle Mints account. The card preserves that distinction instead of combining the claims into a fabricated pedigree. Generic marketing superlatives, potency numbers, practical grow specifications and customer cultivation reviews were not imported into the cards.

The user spellings `uctan` and `muywuay` remain **unresolved**. No strain, lineage or geography has been invented for them; exact intended spelling/source is required before adding them.

## Implementation and verification boundary

- `CityPlantCatalog.cs`: changes display names only; numeric balancing is unchanged.
- `CityPlantEncyclopedia.cs`: full HTTPS primary-source URLs and sixteen cannabis strain cards; no ornamental analogues.
- Catalog tests protect the eight names, unchanged balancing and restored per-ID inventories. Encyclopedia tests require HTTPS breeder sources and cannabis taxon labels.
- This document does not claim a fresh Unity/player build. The coordinator runs integrated tests, replaces incompatible old art/UI labels and validates the final player.
