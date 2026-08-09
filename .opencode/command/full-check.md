---
description: تشغيل كل الفحوصات مرة واحدة (Semgrep + مراجعة كود + مراجعة أمان) وتقرير موحد
agent: clinichub-dev
---

1. Run Semgrep on the latest changes (via the `semgrep` MCP server, or `semgrep scan` in the shell targeting the recent diff paths).
2. Call `@reviewer` to review the latest diff for general code quality.
3. Call `@security-reviewer` to review the latest diff from a security perspective.
4. Combine everything into one unified report with problems ordered by importance (critical first), each with a file reference and a suggested fix.
