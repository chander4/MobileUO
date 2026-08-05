# Issue #23 — Phase 0: Interruption Resilience

Detailed scoping document for Phase 0 of the Mobile Modernization Vision.
Nothing here has been implemented. Written to be reviewed and approved
before any code is written, per the project's plan-first workflow.

---

## Goal

Mobile play is inherently interrupted - a phone call, a notification, the
screen locking, switching apps to check something. Confirmed earlier:
**zero `OnApplicationPause`/`OnApplicationFocus` handling exists anywhere
in this codebase.** Make backgrounding/foregrounding the app a handled,
intentional case instead of whatever happens to occur by accident.

---

## What already exists (reuse, don't rebuild)

This is a smaller lift than it first looks, because ClassicUO already has
a mature reconnect engine underneath - it's just never wired to Unity's
app-lifecycle events, and its main switch is off by default.

- **`Settings.GlobalSettings.Reconnect`** (bool, default `false`) and
  **`ReconnectTime`** (int, seconds, default `0`) - the master switch and
  delay for automatic reconnection. Currently off.
- **`GameScene.SocketOnDisconnected`** (`GameScene.cs:336`) - already
  branches on `Reconnect`: if on, tears down the scene and hands off to
  `LoginScene` with `Reconnect = true`; if off, shows a manual "Connection
  lost" popup requiring a tap before returning to login.
- **`LoginScene`'s reconnect loop** (`LoginScene.cs:492` on) - already a
  complete, working flow: shows a "Reconnect, please wait...`N`" popup
  with a retry counter, reuses `Settings.GlobalSettings.LastServerNum` to
  re-select the same server, and reuses the exact same autologin path
  (`AutoLogin || Reconnect`) to walk back through server/character
  selection without user input.
- **None of this is currently tied to app backgrounding at all** - today
  it only fires reactively, whenever the OS eventually surfaces a socket
  error. There's no proactive "we were backgrounded, get ready" signal,
  and no distinction between a brief interruption (should feel seamless)
  and a long one (server will have long since dropped/timed out the
  character - should show a clear state, not a silent failure).

---

## Design

### 1. Wire real app-lifecycle awareness

Add `OnApplicationPause(bool paused)` and `OnApplicationFocus(bool
hasFocus)` to `ClientRunner.cs` (the MonoBehaviour already driving
`Update()` and the rest of the client lifecycle - this is the natural,
consistent place for it, matching how the Assistant-toggle handler
already lives here for the same "central lifecycle owner" reason).
`ClientRunner.cs` is not in the explicitly protected list (only its
*movement logic* is), same reasoning already applied for the Assistant
fix earlier this session - still calling this out explicitly since it's
a foundational file.

- On pause (backgrounded): record the timestamp. Do not force-disconnect
  - let the OS/socket behave as it naturally will (mobile OSes vary in
    how long they keep a backgrounded app's network alive).
- On resume (foregrounded again): compare elapsed time against a
  threshold (see below) to decide which of the two paths below applies.

### 2. Two distinct resume paths, not one

- **Short interruption** (under some threshold - needs a real number,
  proposing ~30s as a starting point, tunable): if the socket is still
  connected, nothing visible needs to happen at all - genuinely seamless.
  If it dropped anyway (OS killed the connection despite the short gap),
  turn on `Settings.GlobalSettings.Reconnect` behavior for this one
  reconnect attempt so the existing `LoginScene` loop above handles it
  automatically, landing the player back in exactly where they were
  without them navigating anything.
- **Long interruption** (past the threshold, or several failed reconnect
  attempts): the server has very likely already dropped the character
  server-side (idle timeout). Auto-reconnecting here would either fail
  repeatedly or silently log into a stale session - show a clear,
  explicit "You were disconnected while away - tap to log back in" state
  instead of an unexplained black screen, silent failure, or an infinite
  auto-retry loop the player can't see is happening.

### 3. Turn `Reconnect` on for mobile, deliberately

Rather than leaving the engine's own reconnect switch off and building a
parallel mechanism, this milestone's core move is: default
`Settings.GlobalSettings.Reconnect` to `true` on mobile (matching the
same `Application.isMobilePlatform`-gated pattern already used elsewhere
in this codebase, e.g. `RunMouseInASeparateThread`, `touchPan` in the
WorldMapGump work), and set a sane `ReconnectTime`. This is genuinely
most of the "automatic resume" behavior, already built and tested by the
upstream ClassicUO project - the new work is entirely the Unity-lifecycle
wiring and the short-vs-long resume distinction above.

### No changes needed to

- `LoginScene`'s reconnect loop, `GameScene.SocketOnDisconnected`, or any
  networking/packet code - all reused as-is. This milestone is additive
  lifecycle wiring around an existing, working engine feature.

---

## Suggested deliverable breakdown

(One deliverable at a time, stop and validate after each, matching prior
milestones' discipline.)

1. **App-lifecycle hooks in `ClientRunner.cs`** - `OnApplicationPause`/
   `OnApplicationFocus`, recording pause timestamp and elapsed time on
   resume. No behavior change yet - validate via device testing
   (background/foreground the app, confirm the hooks actually fire and
   log the right elapsed time) before wiring any reconnect behavior to
   them.
2. **Mobile default for `Settings.GlobalSettings.Reconnect`/
   `ReconnectTime`** - turn on the existing engine behavior for mobile
   builds. Validate on-device: kill wifi briefly while in-game, confirm
   the existing `LoginScene` reconnect-with-counter flow already fires
   correctly on its own, independent of anything from Deliverable 1.
3. **Short-interruption path** - wire Deliverable 1's resume signal to a
   one-shot reconnect attempt when under the threshold. Validate:
   background the app briefly (phone call simulation), confirm resume is
   seamless or shows the existing reconnect popup briefly, not a longer
   failure state.
4. **Long-interruption path** - the explicit "you were disconnected while
   away, tap to log back in" state for over-threshold gaps, replacing
   whatever would otherwise happen (repeated silent reconnect failures or
   an unexplained state). Validate: background the app for several
   minutes, confirm a clear, explicit resume prompt rather than silence
   or an infinite retry loop.

---

## Open questions to resolve before/during implementation

- **Exact threshold** for short vs. long interruption - 30s is a
  starting proposal, not researched against actual server-side idle
  timeout behavior. Worth checking Ultima-Memento's (or a representative
  shard's) actual idle-kick timing if known, rather than guessing.
- **Does background execution continue at all on iOS/Android**, or does
  Unity suspend all code (including a running `Update()`) the moment the
  app backgrounds? This determines whether "record pause timestamp, do
  nothing else" is really all that's possible while backgrounded, or
  whether limited background work is feasible. Needs verifying on-device
  rather than assumed.

---

## Risks

- **`ClientRunner.cs` is foundational** - even though app-lifecycle hooks
  aren't "movement logic," this is still the single most central
  MonoBehaviour in the project. Keep the diff narrow (new methods only,
  no changes to existing `Update()`/`Awake()` bodies) to minimize
  regression surface.
- **Reconnect loops can mask real problems** - if a shard is genuinely
  down (not just a brief mobile network blip), an aggressive auto-retry
  could spam reconnect attempts. The existing `LoginScene` loop already
  has a retry counter shown to the player, which mitigates this somewhat,
  but worth confirming there's a sane upper bound rather than infinite
  retries.

## Out of scope for this milestone

- Any change to actual reconnect/login network protocol handling - all
  of that is reused as-is from the existing engine.
- Preserving fine-grained client-side UI state (which Gumps were open,
  exact camera/scroll position) across a long-interruption relogin - the
  server is authoritative for character state; this milestone is about
  connection resilience, not UI-state snapshotting. A short-interruption
  seamless resume naturally preserves this anyway since nothing gets torn
  down; a long-interruption relogin naturally does not, matching what a
  fresh login already looks like today.

---

**Not started. Awaiting approval before implementation begins.**
