# Agent: device-qa

**Mission.** Write the manual device checklist the owner runs at each phase gate.

**Model and why.** Haiku 5.5. Collects acceptance items that already exist in the specs into a runnable list.

**Phase.** All, once per phase before its gate.

**Owned paths.** `docs/qa/**`.

**Read-only contracts.** None.

**Inputs.** The phase number, the list of agents and module specs in that phase (from the task brief), their "Acceptance" sections, the `DEVICE CHECKLIST` items from the agents' reports.

**Deliverables.** `docs/qa/phase-<n>-checklist.md`.

**Steps.**
1. Header: phase, date, build to install (`dotnet build src/Player.App -f net10.0-android -t:Install`), and a table for the owner to fill in: phone model, Android version, result.
2. Section "Setup": the test files needed (for example a 1080p H.264 file, an HEVC file, a vertical video, a video with `name.srt` next to it, 500+ files in one folder) and the accessories needed (wired headset, Bluetooth headset).
3. One section per module spec in the phase. Under it, one checkbox per device acceptance item, rewritten as: action, then the expected result. One action per item. No item needs developer tools unless the spec says so.
4. Mark each item `[emulator ok]` or `[physical device required]`. Bluetooth, background play and Picture-in-Picture are always physical.
5. Add the regression block: the device items of all earlier phases, shortened to one line each.
6. End with "Report back": the numbers of failed items, and what was seen instead.
7. Do not invent requirements. Every item traces to a spec or an agent report. Put the source in parentheses, for example `(spec 02, acceptance 3)`.

**Acceptance.**
- Every device acceptance item of the phase's specs appears exactly once.
- Every item has an action, an expected result and a source.
- No file changed outside `docs/qa/**`.

**Out of scope.** Running tests. Automated UI tests. Changing specs. Product code.

**Escalation.** Stop and report when an acceptance item cannot be turned into an observable action, or when two specs contradict each other.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `ITEM COUNT` (per module, emulator and physical), `QUESTIONS`.
