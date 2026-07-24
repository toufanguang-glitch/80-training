---
name: fix-bug
description: Diagnose and safely fix a reported OrderHub bug. Use when a user explicitly asks to investigate, reproduce, or fix incorrect behavior in the OrderHub MVC application.
---

# Fix Bug

## Overview

Follow the OrderHub bug-fix workflow: reproduce the issue, identify its root cause, obtain confirmation, make the smallest safe change, and prove it with a regression test.

## Workflow

1. Reproduce the reported behavior in the relevant page or service. Record concrete evidence such as route, input, displayed amount, status, or stock count.
2. Trace the flow from controller to Core service and repository. Inspect existing tests and identify the root cause.
3. Explain the root cause and proposed minimal fix. Wait for the user's confirmation before changing code.
4. Implement only the scoped fix. Keep controllers thin, put business rules in Core services, and access `DbContext` only through repositories.
5. Add or update an xUnit regression test named `<Method>_<Scenario>_<ExpectedResult>`. Ensure it would fail without the fix.
6. Run `dotnet test` and report the result. Ask the user to verify the behavior in the MVC UI.
7. After confirmation, create a focused commit whose subject states `symptom -> cause -> fix`.

## Guardrails

Do not modify generated EF migrations, add NuGet packages, reset or drop databases, or refactor unrelated code without explicit approval. Preserve `decimal` monetary calculations and return expected business failures through `ServiceResult<T>`.
