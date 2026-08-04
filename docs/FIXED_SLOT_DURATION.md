# Fixed Appointment Duration — 30 Minutes Rule

> **Rule:** Reservation/appointment duration is **fixed at 30 minutes** for all doctors, all days, all clinics.
> Doctors **cannot** change it — the field is removed from the UI and forced to `30` at every layer.

## Why fixed 30 minutes?

1. **Predictable scheduling** — every slot is exactly 30 min, so a 9:00–17:00 work day always yields 16 clean slots. No gaps, no overlaps, no calculation errors.
2. **Fair capacity planning** — clinic owners and staff can count on the same slot grid across every doctor. Comparisons (who has more availability) are apples-to-apples.
3. **Simpler patient booking** — the mobile/online booking page renders one uniform slot grid; patients don't need to reason about 30 vs 45 vs 60 min segments.
4. **Easier queue & waiting-room flow** — front-desk staff (staff dashboard) estimates wait times reliably when every visit has the same expected duration.
5. **Consistent revenue math** — appointment revenue and commission calculation share a uniform base unit (30 min per visit).
6. **One source of truth** — no per-day, per-doctor duration drift between what the clinic owner configures, what the doctor's schedule shows, and what the patient books.

## Benefit of showing it in the Clinic Configuration Settings page

The settings page is the **single trusted place** where the clinic owner sees the rule enforced:

- **Transparency** — the owner sees "مدة الموعد ثابتة: 30 دقيقة" without having to visit each doctor's schedule.
- **Prevents confusion** — because the field was previously "typical duration managed from doctor working hours", owners were unsure who controls it. Now the settings page states it is fixed and immutable.
- **Sync guarantee** — the same fixed value is displayed/used in: clinic settings page, doctor dashboard (availability), clinic-owner dashboard (add/edit doctor), admin user creation, and online booking. Data stays in sync by construction.

## Enforcement points (frontend)

| Layer | File | What was done |
|-------|------|---------------|
| Doctor availability UI | `Views/Doctor/Availability.cshtml` | Removed `.availability-slot` input; hint + badge say 30 fixed; payload always `slotDurationMinutes: 30` |
| Doctor controller | `Controllers/DoctorController.cs` | `TypicalDuration` = 30; `SaveAvailability` forces 30; stat label "مدة الحجز (ثابتة)" |
| Clinic owner add/edit doctor | `Views/Clinic/Doctors.cshtml` | Removed slot input from create + edit rows; payloads always 30; detail view shows "30 دقيقة ثابتة" |
| Clinic controller | `Controllers/ClinicController.cs` | `CreateDoctor`/`UpdateDoctor` force `SlotDurationMinutes = 30`; settings `TypicalSlotDuration` = 30 |
| Admin user creation | `Views/Admin/Users/Index.cshtml` | Removed slot input; serialized payload always 30 |
| Clinic settings page | `Views/Clinic/Settings.cshtml` | Shows "مدة الموعد ثابتة: 30 دقيقة — تُطبق على جميع الأطباء والأيام ولا يمكن تغييرها" |
| Online booking | `Views/Clinic/OnlineBooking.cshtml` | Slot badge always shows "30 دقيقة لكل موعد" |
| Mock stats | `Data/MockData.cs` | `GetDoctorAvailabilityStats` typical duration = 30 |

> **Backend note:** the backend should also validate `slotDurationMinutes == 30` on availability saves and the slots endpoint should generate 30-minute slots. The frontend never sends anything else.
