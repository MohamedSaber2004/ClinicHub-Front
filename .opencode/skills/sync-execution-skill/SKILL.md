---
name: sync-execution-skill
description: Use when the user says "حدّث skill التنفيذ", "sync skill", "update execution skill", or after adding new implementations to the project. Scans the current project state and updates `.opencode/skills/execution/SKILL.md` to reflect any new patterns, endpoints, ViewBag properties, services, routes, or CSS classes.
---

# Sync Execution Skill

## Objective
Keep the execution skill documentation in sync with the actual project code. After any new implementation, run this skill to detect changes and update the reference data in the execution skill.

## Instructions

### Step 1: Scan controllers for new actions
- Read all controller files in `ClinicHub/Controllers/`
- For each controller, extract:
  - New action methods (public IActionResult / async Task<IActionResult>)
  - New JSON endpoints (return Json(...))
  - New ViewBag properties being set
  - New service interfaces injected via constructor
  - New routes/attributes like `[Route("...")]`
- Compare against what's documented in the execution skill

### Step 2: Scan Views for new files
- List all `.cshtml` files in `ClinicHub/Views/` recursively
- Check for:
  - New layout files in `Shared/`
  - New partial files in `Shared/`
  - New view folders (new controller areas)
  - New `@section Scripts` or `@section Styles` patterns

### Step 3: Scan Routes for new helpers
- Read all files in `ClinicHub/Routes/`
- Extract new route helper methods
- Extract new route patterns (URL segments, params)

### Step 4: Scan Services for new contracts
- Read all files in `ClinicHub.Services/Contracts/`
- Check for new service interfaces
- Check for new request/response DTOs in `RequestModels/` and `ReponseModels/`

### Step 5: Scan CSS for new classes
- Read `wwwroot/css/design-system.css` and `wwwroot/css/site.css`
- Check for new CSS custom properties (`--clr-*`, `--space-*`, `--fs-*`, etc.)
- Check for new utility classes (new `.badge-*`, `.btn-*`, `.icon-wrapper--*`, etc.)

### Step 6: Read the current execution skill
- Read `.opencode/skills/execution/SKILL.md`
- Map each section to what was found in steps 1-5

### Step 7: Update the execution skill
- For each section in the execution skill that is outdated:

  **Architecture / Integration Layer** — update controller list, service contract table, new patterns discovered
  ```markdown
  | `INewService` | Description | ControllerName |
  ```

  **ViewBag flow** — add new ViewBag properties with types and set-by location

  **Controller patterns** — add new endpoint patterns (e.g., new JSON patterns, new route conventions)

  **CSS Component table** — add new utility classes discovered

  **View patterns** — add new layout files, new partials, new rendering patterns

- Preserve all existing content that is still accurate
- Only modify sections where new information was found

### Step 8: Summary
- Print a summary of what was updated:
  ```
  ## Sync Complete
  - New services detected: X
  - New controller actions: X
  - New ViewBag properties: X
  - New routes: X
  - New CSS classes: X
  - New views/partials: X
  - Execution skill updated: YES
  ```
