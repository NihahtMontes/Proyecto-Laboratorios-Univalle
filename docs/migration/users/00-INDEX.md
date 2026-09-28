# 00-INDEX.md — User/Person mig F0..F8 index

Phase status: **F0..F8 COMPLETE** (MIG-001 repository scope; READY TO DEPLOY/CUTOVER: NO — see `08-FINAL.md`).

## Reading order

1. `00-MASTER-PLAN.md` — F0..F8 plan: phases, inputs/outputs, surfaces, agents, validators, exit criteria.
2. `00-SOURCE-SPEC.md` — F0 source forensics of the ASP User/Person module (reference only, not edited by F8).
3. `00-PARITY-MATRIX.md` — parity matrix with the `Estado F8` reconciliation (recount, transitions, kept-decisions, product changes, no-promotion-by-documentation note).
4. `00-HANDOFF.md` — F0 handoff (canonical names and scope).
5. `01-IDENTITY-CONTRACT.md` — F1 canonical identity and data contract (FROZEN).
6. `01-DECISIONS.md` — F1 decision log (D001..D022, etc.).
7. `01-DATA-MAPPING.md` — F1 conceptual legacy SQL Server → PostgreSQL data mapping.
8. `01-HANDOFF.md` — F1 handoff.
9. `02-F2-APPLIED.md` — F2 implementation notes, offline verification, live PostgreSQL validation.
10. `02-HANDOFF.md` — F2 PostgreSQL handoff.
11. `03-HANDOFF.md` — F3 NestJS + Auth handoff.
12. `03-SOLID-PATTERNS.md` — F3 SOLID principles and design patterns as implemented.
13. `04-HANDOFF.md` — F4 Contracts + API client handoff.
14. `05-HANDOFF.md` — F5 React functional parity handoff.
15. `06-HANDOFF.md` — F6 visual parity handoff.
16. `07-HANDOFF.md` — F7 E2E + live PostgreSQL handoff.
17. `08-FINAL.md` — F8 reconciliation, final matrix summary, accepted gaps, decisions, cutover checklist, rollback, runtime configuration, follow-ups.
18. `00-INDEX.md` — this file.

## Files in `docs/migration/users/`

| File | Phase | One-line purpose |
|---|---|---|
| `00-MASTER-PLAN.md` | F0 | F0..F8 plan: phase inputs/outputs, surfaces, agents, validators, exit criteria. |
| `00-SOURCE-SPEC.md` | F0 | Source forensics of the ASP User/Person module (reference only). |
| `00-PARITY-MATRIX.md` | F0 + F2..F8 | Parity matrix (158 rows) with per-phase `Estado` sections and the F8 reconciliation. |
| `00-HANDOFF.md` | F0 | F0 handoff: names, scope, evidence, open inputs for F1. |
| `01-IDENTITY-CONTRACT.md` | F1 | Canonical identity and data contract (FROZEN). |
| `01-DECISIONS.md` | F1 | F1 decision log. |
| `01-DATA-MAPPING.md` | F1 | Conceptual legacy SQL Server → PostgreSQL data mapping. |
| `01-HANDOFF.md` | F1 | F1 handoff. |
| `02-F2-APPLIED.md` | F2 | F2 implementation notes, offline verification, live PostgreSQL validation. |
| `02-HANDOFF.md` | F2 | F2 PostgreSQL handoff. |
| `03-HANDOFF.md` | F3 | F3 NestJS + Auth handoff. |
| `03-SOLID-PATTERNS.md` | F3 | F3 SOLID principles and design patterns as implemented. |
| `04-HANDOFF.md` | F4 | F4 Contracts + API client handoff. |
| `05-HANDOFF.md` | F5 | F5 React functional parity handoff. |
| `06-HANDOFF.md` | F6 | F6 visual parity handoff. |
| `07-HANDOFF.md` | F7 | F7 E2E + live PostgreSQL handoff. |
| `08-FINAL.md` | F8 | F8 reconciliation, final matrix summary, accepted gaps, decisions, cutover checklist. |
| `00-INDEX.md` | F8 | This file (full F0..F8 index, reading order, phase status). |

Local PostgreSQL credentials are already provisioned outside tracked files.
