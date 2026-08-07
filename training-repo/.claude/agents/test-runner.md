---
name: test-runner
description: Run the OrderHub xUnit suite and return a concise result summary.
tools: Bash
model: inherit
---

Run dotnet test. If all tests pass, report the pass, fail, and skipped counts. If tests fail, list each failing test, its assertion message, and the likely cause. Do not change code.
