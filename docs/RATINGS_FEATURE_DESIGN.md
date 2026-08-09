# ⭐ ClinicHub Ratings Feature — Design (Web Dashboard)

> **Status: DESIGN ONLY — nothing implemented in this pass.**
> This document specifies how to implement the rating feature on the web dashboard:
> (1) the **clinic owner** views all patient ratings for their clinic,
> (2) the **doctor** views all patient ratings made about them.
> Before writing code, verify the backend endpoints (§4) exist in the API repo.

---

## 1. Scope

| Capability | Web dashboard role | Status today |
|---|---|---|
| View clinic ratings (all + average + count) | SuperAdmin | ✅ Already built (`Admin/ClinicDetails` ratings tab) |
| View clinic ratings (owner's own clinic) | ClinicOwner | ❌ **Not found** — no page, no sidebar link |
| View doctor ratings (all + average + count) | ClinicOwner / Admin (on doctor profile) | ❌ **Not found** |
| View own ratings | Doctor | ❌ **Not found** — no page, no sidebar link |
| Submit / edit rating | Patient (mobile/SPA) | Out of web-dashboard scope (backend + mobile) |

---

## 2. What already exists (found in this repo) — reuse, don't reinvent

### 2.1 DTOs — already support both clinic AND doctor ratings
`ClinicHub.Services/ReponseModels/RatingDto.cs`:
```
Id (string) | UserId (string?) | UserName | DoctorId (string?) | ClinicId (string?)
Value (int, 1–5) | Review (string?) | CreatedAt (DateTime)
```
- `RatingDto` carries **both** `ClinicId` and `DoctorId` → one DTO serves the clinic page and the doctor page.
- `ClinicDetailsDto.cs` already exposes `AverageRating (double?)`, `TotalRatings (int)`, `RecentRatings (List<RatingDto>?)`.
- `ClinicManagmentDto.Rating (double?)` — used for list-level star display (`Admin/Clinics.cshtml:109-156`).

### 2.2 Views — the ratings UI pattern already exists once
`Views/Admin/ClinicDetails.cshtml:430-474` — the **ratings tab**:
- Summary card: `.ratings-header-card` → `.rhc-score` → `.rhc-number` (avg `"F1"`) + `.stars-inline` (full/half/empty stars: gold `#F59E0B`, empty `#D2D5DB`) + `.rhc-count` (`{n} تقييم`).
- List: `.ratings-list` → `.rating-full-card` per item → `.rfc-avatar` (initial from `UserName`, fallback `م`) + `.rfc-header` (`.rfc-name` + `.rfc-date` `yyyy-MM-dd`) + 12px stars + `.rfc-comment`.
- Empty state: `<div class="empty-state"><p>لا توجد تقييمات بعد.</p></div>`.
- Half-star rendering via `<clipPath>` (see lines 441-443).

### 2.3 Mock data (for design/reference only)
`ClinicHub/Data/MockData.cs`:
- `GetClinicRatings(Guid clinicId)` (line 744) — `MockPatientRating { Id, ClinicId, DoctorName, PatientName, PatientInitial, Rating, Comment, Date }`.
- `GetClinicAvgRating(Guid clinicId)` (line 780).
- ⚠️ No `GetDoctorRatings(...)` mock exists — doctor ratings have **no mock and no page**.

### 2.4 CSS — already in `site.css`
`.ratings-header-card`, `.rhc-number`, `.rhc-count`, `.stars-inline`, `.ratings-list`, `.rating-full-card`, `.rfc-avatar`, `.rfc-content`, `.rfc-header`, `.rfc-name`, `.rfc-date`, `.rfc-comment` — plus `.empty-state`, `.badge`, `.filter-bar`, `.table-card`, `_Pagination`.

---

## 3. What is NOT found (must be designed/built)

| Missing piece | Detail |
|---|---|
| Clinic-side ratings page | `_ClinicLayout` sidebar has **no** "التقييمات" item (`Views/Shared/_ClinicLayout.cshtml:80-170`) |
| Doctor ratings page | `_DoctorLayout` sidebar has no ratings item; no `Views/Doctor/Ratings.cshtml` |
| Dedicated ratings endpoints/service | `DoctoryRoutes.cs` has **no** ratings route; no `IRatingService`/`IRatingService` contract |
| Doctor-detail ratings tab | `.doctor-detail-header` block exists (`dd-header-*` CSS) but no ratings section |
| Patient submission UI | Not a dashboard concern (mobile/SPA) — backend + mobile |

---

## 4. Backend contract — VERIFY FIRST (backend is a separate repo)

Assumed endpoints (camelCase, `ApiResponse<T>` envelope, JWT bearer, `Accept-Language: ar`):

| # | Endpoint (assumed) | Purpose | Consumed by |
|---|---|---|---|
| R1 | `GET api/v1/clinics/{clinicId}/ratings?pageNumber&pageSize` → `PagginatedResult<RatingDto>` + avg/count | Clinic owner's own clinic ratings | ClinicController |
| R2 | `GET api/v1/doctors/{doctorId}/ratings?pageNumber&pageSize` → `PagginatedResult<RatingDto>` + avg/count | Doctor's own ratings | DoctorController |
| R3 | `POST api/v1/ratings` `{ clinicId, doctorId, value, review }` | Patient submits rating | (mobile/SPA — out of dashboard scope) |
| R4 | Aggregate block in R1/R2: `averageRating`, `totalCount` (either in envelope `data` or computed from `RatingDto.Value` client-side) | Summary card numbers | both pages |

> ⚠️ If R1/R2 do **not exist** on the backend, **stop** — do not build the frontend against missing endpoints. Possible fallbacks: reuse `GET /clinics/{id}/details` (`RecentRatings` is already returned there, but non-paginated) for a first pass of the clinic page only.

**Contracts to add in `ClinicHub.Services`:**
- `Contracts/IRatingsService.cs`:
  - `Task<PagginatedResult<RatingDto>> GetClinicRatingsAsync(Guid clinicId, int pageNumber, int pageSize)` → R1
  - `Task<PagginatedResult<RatingDto>> GetDoctorRatingsAsync(Guid doctorId, int pageNumber, int pageSize)` → R2
- `Routes/Api/DoctoryRoutes.cs`: `Ratings = new RatingsRoutes(BaseRoute)` with `ClinicRatings(clinicId)` / `DoctorRatings(doctorId)` helpers.
- DI registration: `AddHttpClient<IRatingsService, RatingsService>()` with `BearerTokenHandler` (+ `ClinicHeaderHandler` for the clinic-scoped endpoint, mirroring `IClinicService`).

---

## 5. Frontend design

### 5.1 Clinic owner — `Clinic/Ratings` page (التقييمات)

```
_ClinicLayout sidebar → new item "التقييمات" (star icon) → ClinicRoutes.Pages.Ratings()
Route: GET /Clinic/Ratings?pageNumber&pageSize  →  ClinicController.Ratings(pageNumber=1, pageSize=10)
```

- Controller (`ClinicController.Ratings`) — 3-layer error handling pattern, ViewBag only:
  - `clinicId` from `CurrentUser.ClinicId` (NOT from the route, same as `CreateAdOrder` precedent).
  - `ViewBag.Ratings` (`IReadOnlyCollection<RatingDto>`), `ViewBag.Pagination` (`PagginatedResult`), `ViewBag.AverageRating`, `ViewBag.TotalRatings`.
- View `Views/Clinic/Ratings.cshtml` (`Layout = "_ClinicLayout"`):
  - **Summary header** (reuse `.ratings-header-card`): `.rhc-number` avg `"F1"` + `.stars-inline` (full/half/empty — copy the `Admin/ClinicDetails.cshtml:49-50, 441-443` half-star clip-path) + `.rhc-count` `{n} تقييم`.
  - **Filter bar** (`.filter-bar`, optional pass 2): star-value filter (`5/4/3/2/1`) + doctor-name search → route values mirrored in `_Pagination.cshtml` (ViewBag `RatingFilter`/`RatingSearch`).
  - **List**: `.ratings-list` → `.rating-full-card` per item (identical markup to §2.2) — **add the doctor name row** when `RatingDto.DoctorId` is present: a `.rfc-meta` line "الطبيب: {DoctorName}" (DoctorName is not in `RatingDto` — either backend adds `doctorName`, or omit for now; see §5.4).
  - **Empty state**: `.empty-state` "لا توجد تقييمات بعد."
  - **Pagination**: `<partial name="_Pagination" />` (mandatory — paginated endpoint rule).
  - Time: format via the UTC→local pattern (see `_NotificationsCenter.cshtml` fix) if time-of-day matters; date-only `yyyy-MM-dd` otherwise.

### 5.2 Doctor — `Doctor/Ratings` page (تقييماتي)

```
_DoctorLayout sidebar → new item "تقييماتي" (star icon) → DoctorRoutes.Pages.Ratings()
Route: GET /Doctor/Ratings?pageNumber&pageSize → DoctorController.Ratings(pageNumber=1, pageSize=10)
```

- Controller: `doctorId` from `CurrentUser.UserId` (DoctorController already uses `CurrentUser`; verify the doctor-profile id convention used by `IDoctorDashboardService`).
- View `Views/Doctor/Ratings.cshtml` (`Layout = "_DoctorLayout"`): same summary card + list as §5.1, **no doctor-name row** (it's the doctor's own ratings). Empty state: "لا توجد تقييمات بعد."

### 5.3 Doctor-detail ratings tab (Admin + Clinic doctor profiles) — optional pass 2

Where a doctor profile is shown (`Admin/ClinicDetails` doctors tab, `Clinic/Doctors` detail card, `.doctor-detail-header` pages):
- Add a "التقييمات" tab/section consuming R2 with the doctorId; reuse the exact ratings markup.
- Only if R2 exists and the doctorId used by the detail page matches the rating `DoctorId`.

### 5.4 Data gap to confirm with backend
`RatingDto` lacks `DoctorName`/`ClinicName`. For the clinic page's per-item doctor label and the summary, either:
- (a) backend adds `DoctorName` to `RatingDto` for R1; or
- (b) first pass renders only patient name + comment + stars (doctor label optional).

---

## 6. Implementation order (when endpoints are confirmed)

1. Add `IRatingsService` + `RatingsService` + `DoctoryRoutes` ratings routes; register DI (BearerToken + ClinicHeader handlers).
2. `ClinicController.Ratings` + `ClinicRoutes.Pages.Ratings()` + sidebar item in `_ClinicLayout`.
3. `Views/Clinic/Ratings.cshtml` — clone the §2.2 pattern; add summary card + pagination + empty state.
4. `DoctorController.Ratings` + `DoctorRoutes.Pages.Ratings()` + sidebar item in `_DoctorLayout`.
5. `Views/Doctor/Ratings.cshtml` — same pattern, no doctor row.
6. Optional: doctor-detail ratings tab (Admin + Clinic) — §5.3.
7. Validate: `dotnet build` in `ClinicHub/`; manual checks — empty clinic (empty state), clinic with ratings (summary numbers match `Value` average), pagination preserves filters, UTC→local time rendering, RTL layout at 320 px.

---

## 7. Deliverable summary

- **Found & reusable:** `RatingDto` (clinic+doctor), `ClinicDetailsDto` aggregates, the `Admin/ClinicDetails` ratings tab markup/CSS, mock clinic ratings, `_Pagination`.
- **Not found (design above):** owner ratings page, doctor ratings page, doctor-detail ratings, ratings service contract + routes, doctor ratings mock.
- **Blocking dependency:** R1/R2 backend endpoints — verify in the API repo before any implementation.
