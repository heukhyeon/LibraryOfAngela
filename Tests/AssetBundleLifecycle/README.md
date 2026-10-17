# Reception request lifecycle regression

Originally synchronous operations register before dispatch and decrement once
on completion. Async background loads do not affect the required counter.
Completion requires exactly zero; no negative-counter workaround.

The scene-load guard checks only the current request's tracked target types.
If any associated node has a live bundle or an active native load, keep the
request. This also preserves completed requests with live bundles and requests
where only some operations have been unloaded. Reset/log only when every
associated operation has been destroyed or unloaded. Initialization is preserved.
A completed native request awaiting finalization counts as active if its bundle
is alive. A completed request whose bundle was destroyed does not.
No per-bundle recovery scan, roster reconstruction, or cache reset is performed.
The canceled node-load branch releases its finished native request reference.

Build AssetBundleLifecycleTests.csproj with Windows MSBuild/.NET Framework 4.8
and run bin/AssetBundleLifecycleTests.exe. All 16 cases passed, including stage
64/6661 reentry, counter balancing, completed/live preservation, pending operation
preservation, all-unloaded reset, and partial unload with live/pending siblings.

NativeRepro.cs executes production hooks with mocked game state in Unity
2019.3.15f1 using the real installed Carmen bundle. It verifies five entries per
stage 64/6661, a live completed request retained, native destroyed-object null
semantics, and resetting an all-unloaded stale request before reentry reload.
This is native lifecycle verification, not full game UI automation.
