# Home Base System

Self-hosted household management: food inventory, physical assets and electronics,
shopping lists, purchases with price history, and the analytics that follow from it
price trends, store comparison, consumption, spoilage, personal inflation.

Receipts are not typed in by hand: an external [n8n](https://n8n.io) instance reads them
via OCR and delivers them to this application's ingest API. n8n itself is **not** part of
this project only the interfaces it talks to are built here.
---

## Stack

- **.NET 10**, C# with `Nullable` and `ImplicitUsings` enabled
- **Blazor Web App**, Interactive Server the app runs on the home network, so a separate
  API layer for the UI would be pure overhead
- **PostgreSQL 17** via **EF Core 10 + Npgsql**, code-first
- **EFCore.NamingConventions** snake_case tables and columns
- **`pg_trgm`** for fuzzy matching of receipt lines against products
- Central package versions in [`src/Directory.Packages.props`](src/Directory.Packages.props),
  shared build properties in [`src/Directory.Build.props`](src/Directory.Build.props)

Postgres rather than SQLite, because EF Core stores `decimal` in SQLite as TEXT and can
neither sort nor compare it correctly precisely where money is concerned. On top of that
come `jsonb` for the raw OCR payloads, window functions for price histories, and real
concurrency while n8n writes and you click at the same time.

---

## The data model in four layers

The most common mistake in systems like this is throwing "a product", "one of them in the
fridge", and "a line on a receipt" into *one* table. After that, no meaningful price
analysis is possible. Hence the strict separation:

| Layer | Entities | Question |
|---|---|---|
| **1 Catalog** | `Product`, `ProductAlias`, `Category` | What exists? |
| **2 Stock** | `StockLot`, `StockMovement`, `Asset`, `AssetDocument` | What is on hand? |
| **3 Purchases** | `Purchase`, `PurchaseItem`, `Store` | What did it cost? |
| **4 Demand** | `ShoppingList`, `ShoppingListItem` | What is missing? |

Plus `StorageLocation` as master data, and `Recipe`, `RecipeIngredient`, and
`MealPlanEntry`.

**Two kinds of stock, modeled differently on purpose.** `StockLot` is quantity-based
(750 g of flour, best-before date, opened since, storage location); `Asset` is
unit-based (this one specific device, serial number, warranty, value). Both point at the
same `Product` catalog and the same `PurchaseItem` price provenance.

### Base units

Every product has a base unit (`Gram`, `Milliliter`, `Piece`) and a package size expressed
in it. Stock and movements are stored exclusively in base units; kg and l are purely
display formatting.

```
Receipt: "2 x milk 1L  @ 1.09 EUR"
  -> Product 42, BaseUnit = Milliliter, PackageSize = 1000
  -> Quantity = 2, QuantityBase = 2000 ml
  -> PricePerBaseUnit = 2.18 EUR / 2000 ml = 0.00109 EUR/ml = 1.09 EUR/l
```

Only this makes the 1 l carton comparable to the 1.5 l one, the promo price to the regular
price, one store to another.

**Money is always `decimal`**, never `double`: `numeric(12,2)` for totals,
`numeric(14,6)` for unit prices otherwise the EUR/ml value rounds to zero. Only gross
prices are stored, exactly as printed on the receipt.

---

## Development

### Prerequisites

- .NET SDK 10
- PostgreSQL 17 (local or containerized)
- `dotnet-ef` for migrations: `dotnet tool install --global dotnet-ef`

### Start the database

```bash
docker run -d --name homebase-postgres \
  -e POSTGRES_DB=homebase \
  -e POSTGRES_USER=homebase \
  -e POSTGRES_PASSWORD=homebase \
  -p 5432:5432 \
  postgres:17
```

### Build and run

```bash
cd src
dotnet restore
dotnet build
dotnet run --project HomeBase
```

The app then listens on `http://localhost:5243` and `https://localhost:7229`
(see [`launchSettings.json`](src/HomeBase/Properties/launchSettings.json)).

### Ingest API key

The `/api/v1` endpoints are rejected with `401` unless the caller sends the configured key in
an `X-Api-Key` header, and answer `503` while no key is configured at all. Supply it through
the environment rather than a checked-in file:

```bash
Ingest__ApiKey=<a long random string>
```

### Asset documents

Invoices, manuals and photos attached to an asset are stored on disk, not in the database.
They land under `App_Data/assets/<assetId>/` beneath the content root unless another directory
is configured:

```bash
Assets__DocumentPath=/srv/homebase/documents
```

### Migrations

The design-time factory reads the connection string from the `HOMEBASE_DB` environment
variable and otherwise falls back to
`Host=localhost;Port=5432;Database=homebase;Username=homebase;Password=homebase`.

```bash
cd src
dotnet ef migrations add <Name> --project HomeBase.Database
dotnet ef database update --project HomeBase.Database
```

---

## n8n interface

Everything under `/api/v1`, as a Minimal API in the same ASP.NET host as the Blazor UI.
Authentication via an `X-Api-Key` header against a key from configuration, compared in
constant time, plus rate limiting.

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/api/v1/receipts` | Submit a receipt |
| `GET`  | `/api/v1/receipts?status=NeedsReview` | Open review cases |
| `POST` | `/api/v1/receipts/{id}/confirm` | Approve → books stock in |
| `GET`  | `/api/v1/products?q=…&gtin=…` | Product lookup |
| `POST` | `/api/v1/shopping-lists/{id}/items` | "Add milk to the list" from Telegram |
| `GET`  | `/api/v1/stock/low` | Below minimum stock |
| `GET`  | `/api/v1/stock/expiring?days=3` | Expiring soon |
| `POST` | `/api/v1/recipes` | Import a recipe (ingredient lines run through the same matching pipeline) |

The last two are poll targets for a cron workflow in n8n. That keeps notification logic
where it belongs and keeps this software lean.

**Idempotency is mandatory**, not optional: n8n retries workflows on failure.
`Purchase.ExternalId` (a hash of the receipt image) is UNIQUE a repeated call returns
`200` with the same `purchaseId` instead of booking the receipt twice. The complete OCR
payload stays untouched in `RawPayload`: if the parsing logic improves later, old receipts
can be reprocessed without sending the photos through OCR again.

