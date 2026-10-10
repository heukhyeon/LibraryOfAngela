# Cached automatic card selection

`AutoBattleImpl` forecasts card / owner slot / target / target slot combinations
using the existing custom priority, clash, light-management and expected-damage
scores. It reserves cards and light in copied state, never by equipping and
rolling back real cards. Enemy attacks are considered across all custom AI units
before filling remaining slots. Forecasts include actual committed cards and
never reuse their occupied slots.

`AutoBattlePatch` keeps the native `PlayTurnAutoForPlayer/Enemy` methods running:

- Native availability checks precede the cached card preference. A cached card
  that fails these checks falls back to the remaining native candidates.
- Only the base target setter's random choice receives the cached target. Full
  candidate lists, fixed targets, revive filtering, `ChangeAttackTarget`, custom
  target setters and other Harmony hooks remain effective.
- As in the original selector, ordinary forecasts and instant-card candidates
  target enemies. `TeamKill()` permits targeting allies but does not make them
  priority candidates. Explicit fixed targets and forced target hooks can still
  select allies. The base random fallback also prefers enemies if another card
  replaces the cached card; controllable units keep their native targeting.
- The cached target slot is an input to `ChangeTargetSlot`, never an override of
  its result. Enemy `OnSelectCardAuto` card replacements are also preserved.
- Every completed selection invalidates the remaining forecast. Rebuilding reads
  actual cards, targets, occupied slots and reserved light, including side effects
  on other units. The attempted source slot is marked complete even if a hook
  moves its card elsewhere; earlier selections are never unequipped or replayed.
- `AddCard` from ordinary AI or another mod also invalidates an active forecast.
  Session/context cleanup uses Harmony finalizers, including on exceptions and
  nested calls. The ordinary auto button also provides a session for uncontrolled
  units; controllable units retain native choices.
- Native auto selection excludes instant cards. A bounded prepass retains instant
  use from the normal and personal E.G.O. hands, re-reading state after each use
  and choosing an empty slot so existing selections are not deselected. The same
  card object is attempted at most once per prepass.

The score is a forecast, not a promise of the final clash. When every legal
remaining attack would steal a clash, the planner permits the native redirection
instead of manually restoring the enemy card's old target. Runtime card/target
hooks can change the outcome; the next forecast accounts for that result.

## Regression tests

Requires Windows .NET Framework 4.8, MSBuild, and the installed LoA HarmonyX files.
Override `HarmonyPath` when they are elsewhere. `App.config` redirects Cecil to
0.11.3, the version shipped alongside this installation's HarmonyX.

```powershell
msbuild Tests/AutoBattle/AutoBattleTests.csproj /t:Build
Tests/AutoBattle/bin/AutoBattleTests.exe
```

The executable compiles the production planner, patches and public interface
against a small game model and actually applies Harmony patches. It checks cached
choices, read-only budgeting, forced targets/slots/cards, source-slot movement,
changed costs, existing cards, exceptions, ordinary controllable AI, target-setter
patch compatibility, instant E.G.O., and global clash allocation.

## Installed game IL validation

Build the runtime with deployment/game-launch events disabled, then run:

```powershell
msbuild LibraryOfAngela.sln /p:Configuration=Release '/p:Platform=Any CPU' /p:PreBuildEvent= /p:PostBuildEvent=
msbuild Tests/AutoBattle/VerifyGameIL.csproj /t:Build
Tests/AutoBattle/bin/VerifyGameIL.exe `
  'D:/LibraryOfAngela/Runtime/bin/Release' `
  'C:/Program Files (x86)/Steam/steamapps/workshop/content/1256670/3727623705/Assemblies' `
  'C:/Program Files (x86)/Steam/steamapps/common/Library Of Ruina/LibraryOfRuina_Data/Managed'
```

This reads the installed game's real IL, runs the production transpilers, verifies
all intended call replacements, and checks evaluation-stack heights across branch
joins and exception handlers. It does not start Unity or run a real battle. Live
combat validation remains separate; Unity internal calls cannot be executed by a
normal .NET Framework process.
