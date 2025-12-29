# ChemEats

A small food-ordering sample application built with Blazor WebAssembly and ASP.NET Core.  
Provides menu browsing, ordering, and per-user order management (view, aggregated summary, and deletion). The solution is split into clear layers (Web UI, API, Services, Domain, Shared DTOs) to demonstrate a maintainable, testable architecture.

---

## Quick overview

- Frontend: Blazor WebAssembly (WebApp)
- Backend: ASP.NET Core Minimal API (WebApi)
- Domain + EF Core: Domain, Services (repositories)
- Shared DTOs: Shared project used across client and server
- Language / platform: C# 13 (.NET 9)
- Recommended IDE: __Visual Studio 2022__ or the __dotnet__ CLI

Architecture (see diagram below for a visual summary)

---

## Key features

- Browse supplier menus and meal items
- Select quantities and place orders (batch POST)
- View your persisted orders (today + future) as individual persisted items and aggregated summaries
- Delete individual persisted MealOrder items or delete all orders for a given meal+date (aggregate delete)
- Authentication flow — endpoints require authorization

---

## Projects in the solution

- `WebApp` — Blazor WebAssembly client (UI)
  - Pages live under `WebApp/Pages` (e.g. `OrderMenu.razor`)
  - Services (HTTP clients) under `WebApp/Services`
- `WebApi` — Minimal API, routes under `WebApi/Routes` (e.g. `OrdersEndpoints.cs`)
- `Services` — repository implementations, Mapster mappings
- `Domain` — EF Core entities, DbContext, read-models
- `Shared` — DTOs used across client and server (e.g. `PlaceOrdersRequestDto`, `UserOrderDto`, `UserOrderItemDto`)

---

## Important DTOs / Models

- `PlaceOrdersRequestDto` — request to place one or more items (mealId, date, quantity)
- `PlaceOrdersResponse` — server response after placing orders
- `UserOrderDto` — aggregated per-meal order summary returned by `GET /api/mealorders/me`
- `UserOrderItemDto` — one persisted order item (contains `OrderId`, `MealId`, `UserId`, date, price, status)

Mapster is used for mapping domain read-models to shared DTOs (`Services/Mappings`).

---

## How to run locally

Prereqs:
- .NET 9 SDK installed
- Optionally __Visual Studio 2022__ (recommended for debugging WASM + API together)

Build and run (CLI)
1. From the repository root:
   - __dotnet clean__
   - __dotnet build__ (build order: Services -> WebApi -> WebApp is recommended)
2. Run API (or start via Visual Studio):
   - __dotnet run --project WebApi/WebApi.csproj__
3. Run the Blazor client:
   - __dotnet run --project WebApp/WebApp.csproj__
   - Or open `WebApp` in Visual Studio and press __Debug > Start Debugging__.

If using Visual Studio, open the solution and set multiple startup projects (WebApi + WebApp) or run the server and client independently.

---

## Common developer tips

- Service worker (PWA) can cache stale assets during development and trigger SRI / .pdb integrity errors. To avoid issues:
  - Unregister service worker in DevTools -> Application -> Service Workers or run:
    ```js
    navigator.serviceWorker.getRegistrations().then(r => r.forEach(reg => reg.unregister()));
    ```
  - In `wwwroot/index.html` gate service worker registration during development (only register in production).
- If you see SRI errors for `.pdb` files:
  - Make sure debug symbols are generated for Debug configuration (portable PDBs).
  - Confirm `WebApp` project has Debug symbols enabled, or disable symbol generation for the published/hosted config.
- When debugging HTTP flows, use the browser Network tab to inspect:
  - POST `api/mealorders` (placing orders)
  - GET `api/mealorders/me` and `api/mealorders/me/items` (view orders)
  - DELETE endpoints for deletion flows

---

## API summary (important endpoints)

- POST `/api/mealorders`  
  Place orders (body: `PlaceOrdersRequestDto`).

- GET `/api/mealorders/me`  
  Get aggregated user orders (query: optional `supplierId`, `startDate`, `endDate`) → returns `UserOrderDto[]`.

- GET `/api/mealorders/me/items`  
  Get persisted user order items (one row per `MealOrder`) → returns `UserOrderItemDto[]`.

- DELETE `/api/mealorders/{orderId}`  
  Delete a single persisted `MealOrder` by id.

- DELETE `/api/mealorders?mealId={mealId}&date={date}`  
  Delete all persisted orders for the current user that match `mealId` + `date` (aggregate delete).

All endpoints require authorization.

---

## UI behaviour notes

- Order page (`/menus/order`) shows:
  - Menus filtered by date
  - Selection controls (qty, checkbox), plus/minus buttons
  - Sticky order summary with two views:
    - Pending selection summary (client-side)
    - Persisted orders (today + future) — supports deletion per item
- After placing orders the UI reloads persisted items (so the user sees the saved orders).

---

## Testing & development workflow

- Rebuild projects in this order where needed:
  1. __Services__ (mappings, repo implementation)
  2. __WebApi__ (endpoints)
  3. __WebApp__ (client)
- Unit tests can be added next to Services or Domain using xUnit / NUnit (not included by default).
- Mapster mappings live in `Services/Mappings` — add mapping registrations here when new DTOs or read-models are added.

---

## Contribution and standards

- Follow C# coding guidelines in the repository: use file-scoped namespaces, prefer immutable records for DTOs / read-models, keep mapping code in `Services/Mappings`.
- Keep repository boundaries clean: Domain → Services (repositories) → WebApi → WebApp (client).
- Use descriptive names, flow cancellation tokens through async calls, and prefer `Try*` patterns where appropriate.

---

## Troubleshooting

- "Fetch event handler is recognized as no-op" — remove or fix the service worker fetch listener or avoid registering the worker in development.
- SRI / .pdb integrity warnings — ensure PDB files are generated and served consistently or disable symbol emission for the published build.
- If client can't deserialize DTOs, verify `Shared` project is referenced by both client and server and build order is correct.

---

## Visual architecture

(See below — a compact mermaid diagram representing components and data flow.)

---

## License

Add your chosen license (e.g. `MIT`) in `LICENSE` file.

---

If you want, I can:
- Add a short CONTRIBUTING.md
- Replace the browser `confirm` dialogs with SweetAlert2 UI in the Blazor pages
- Autogenerate Postman / OpenAPI snippets for the endpoints