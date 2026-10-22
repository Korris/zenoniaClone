---
name: review-report-format
description: How the zenonia lead wants code-review output shaped (severity-ordered, file:line, score /10, short handback)
metadata:
  type: feedback
---

Lead wants review reports ordered Critical/High/Medium/Low, each finding with `file:line`, a one-line "why it's a bug", and a minimal fix snippet; an overall score /10; handback summary limited to Critical+High plus score (<=15 lines). No formatting nitpicks, no file rewrites.

**Why:** Lead triages quickly from the summary and applies fixes from the report; verbose or style-heavy reviews slow that down. Known/accepted gaps are listed in the prompt (e.g. no shop, no audio, no mobile controls, attack dropped during knockback) and should not be re-reported.

**How to apply:** For any review in D:\zenonia, read the prompt's "Known/accepted" list first, verify suspected bugs numerically when cheap (node script in scratchpad), and keep the handback terse. Tests run with `npm test` (node --test).
