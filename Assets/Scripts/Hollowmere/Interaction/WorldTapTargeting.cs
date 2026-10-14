using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.IO.Resources;
using Hollowmere.HudRuntime;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Mobile Tap Targeting v1: the input-layer bridge between a screen tap and
    /// ClassicUO's existing SelectedObject/PrimaryActionResolver pipeline.
    ///
    /// The problem: ClassicUO's world hit-test is draw-time/hover-based (a
    /// per-GameObject pixel test against Mouse.Position, run every frame during
    /// GameScene's render pass - see GameScene.cs:625-626/894-900). Desktop has
    /// a persistent cursor so this "just works" as continuous hover. Mobile does
    /// not: GameController.MouseUpdate()'s mobile branch only updates
    /// Mouse.Position while a LeanTouch finger is actively down/held - once the
    /// finger lifts, nothing keeps re-driving hover, so a selection has no way
    /// to outlive the touch that created it.
    ///
    /// Tried and rejected: re-asserting the captured entity back into
    /// SelectedObject.Object/LastObject every frame, so every consumer could
    /// keep reading SelectedObject alone. Confirmed live (three different
    /// timing points - Update(), LateUpdate(), a WaitForEndOfFrame coroutine)
    /// that GameScene.Update()'s own "if (!IsMouseOverViewport)
    /// SelectedObject.Object = SelectedObject.LastObject = null;" (called every
    /// frame from ClientRunner.Update(), ahead of this project's HUD tick in
    /// script execution order) always re-nulls it before HudWidgetHostComposition's
    /// tick loop gets to read it. There is no point in a frame this component can
    /// act from that both comes after that null-check and before every reader -
    /// short of reordering ClientRunner itself, which is out of scope (this is an
    /// input-layer adapter, not a renderer/scene-lifecycle change).
    ///
    /// What actually works: don't fight to keep SelectedObject populated at all.
    /// Expose the captured entity as a small read-only fallback (PinnedEntity),
    /// the same shape as BackpackPresenter.SelectedSerial/
    /// TargetVitalsPresenter.CurrentTarget - and have PrimaryActionPresenter
    /// prefer SelectedObject.LastObject when it's live, falling back to
    /// PinnedEntity only when it's null. PrimaryActionResolver itself is
    /// untouched: the fallback is resolved before the existing single
    /// BaseGameObject argument is passed in, not a new resolver parameter.
    ///
    /// Mobile Tap-to-Move v1: the same native hit-test that resolves an Item/
    /// Mobile under a tap also resolves Land/Static/Multi ground tiles when the
    /// tap lands on empty walkable space - GameSceneInputHandler.
    /// OnRightMouseDoubleClick() (the existing desktop click-to-walk gesture)
    /// already proves this and already contains the exact "is this tile
    /// walkable" classification (Land, or a Static/Multi/Item flagged
    /// IsSurface) and the exact movement call (Pathfinder.WalkTo). A ground tap
    /// reuses both verbatim instead of selecting anything - see
    /// TryResolveWalkDestination. Entity taps (Item/Mobile) are unaffected and
    /// still only ever pin a selection, never move the player.
    /// </summary>
    internal static class WorldTapTargeting
    {
        private static Runner instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new UnityEngine.GameObject("HollowmereWorldTapTargeting");
            UnityEngine.Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<Runner>();
        }

        /// <summary>The entity from the last qualifying world tap, or null if none/cleared.
        /// Consulted by PrimaryActionPresenter only when SelectedObject.LastObject is null.</summary>
        internal static BaseGameObject PinnedEntity => instance != null ? instance.Pinned : null;

        /// <summary>The single canonical "what is currently selected on mobile" answer -
        /// live SelectedObject when present, the tapped-and-released fallback otherwise.
        /// Mobile Target Feedback v1: both PrimaryActionPresenter and the target marker
        /// consult this directly (see the class doc's diagram) rather than one presenter
        /// depending on another or duplicating the fallback logic in two places.</summary>
        internal static BaseGameObject CurrentSelection => SelectedObject.LastObject ?? PinnedEntity;

        /// <summary>Called by PrimaryActionDefinitionFactory.ClearSelection() so a fired
        /// action's one-shot clear also drops the fallback, not just SelectedObject.</summary>
        internal static void ClearPin()
        {
            instance?.ClearPin();
        }

        private class Runner : MonoBehaviour
        {
            private WorldTapStateMachine tapMachine;
            // Mobile Long-Press Context v1: a second instance of the existing
            // LongPressStateMachine/LongPressSettings/JoystickExclusionZone
            // (the same classes ContextMenuLongPressTrigger already uses for
            // its own long-press gesture), fed the same finger stream as
            // tapMachine below - not a new gesture-detection system, one more
            // consumer of the same primitives. LongPressSettings' default
            // HoldDurationSeconds (0.5s) intentionally coincides with
            // WorldTapStateMachine's default maxTapDurationSeconds (0.5s):
            // a long press only ever fires once elapsed >= 0.5s, so by the
            // time the finger lifts tapMachine's own "< 0.5s" check has
            // already gone false - no double-fire. Changing either constant
            // independently would open a dead zone or cause both to fire.
            private LongPressStateMachine longPressMachine;
            private int trackedFingerIndex = -1;
            private bool captureNextFrame;
            private Vector2 capturedTapScreenPosition;
            private BaseGameObject pinnedEntity;

            internal BaseGameObject Pinned
            {
                get
                {
                    if (pinnedEntity is ClassicUO.Game.GameObjects.GameObject go && go.IsDestroyed)
                    {
                        pinnedEntity = null;
                    }

                    return pinnedEntity;
                }
            }

            private void Start()
            {
                // Same exclusion zone MobileInputController's joystick and
                // ContextMenuLongPressTrigger's long-press already use - the
                // left half (or right, in left-handed mode) is movement's, not
                // world-targeting's.
                tapMachine = new WorldTapStateMachine(new JoystickExclusionZone());
                tapMachine.TapDetected += OnTapDetected;

                longPressMachine = new LongPressStateMachine(new LongPressSettings(), new JoystickExclusionZone());
                longPressMachine.LongPressDetected += OnLongPressDetected;
            }

            private void Update()
            {
                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    pinnedEntity = null;
                    trackedFingerIndex = -1;
                    return;
                }

                // A tap resolved last frame - only now (one Update() later) has
                // this frame's OnPostRender/DrawUnity actually hit-tested the
                // release position, so only now is SelectedObject.LastObject
                // trustworthy for that tap.
                if (captureNextFrame)
                {
                    captureNextFrame = false;
                    BaseGameObject resolved = SelectedObject.LastObject;

                    // Mobile Spell Casting & Targeting v1: TargetManager.IsTargeting is
                    // the same flag GameSceneInputHandler's own mouse click already
                    // checks before doing anything else - while it's true, the existing
                    // targeting system is authoritative over every tap, universally (not
                    // just spell casts - any source of a TargetCursor packet), so this
                    // branch pre-empts both normal selection and tap-to-move below.
                    if (TargetManager.IsTargeting)
                    {
                        SendToTargetManager(resolved);
                    }
                    else if (resolved is Item || resolved is Mobile)
                    {
                        pinnedEntity = resolved;
                    }
                    else
                    {
                        // Ground, not an entity - move instead of selecting.
                        // Never both: Item/Mobile always wins the branch above,
                        // so an entity tap can never also issue movement.
                        pinnedEntity = null;

                        if (TryResolveWalkDestination(resolved, out int wx, out int wy, out int wz))
                        {
                            IssueWalk(wx, wy, wz);
                        }
                        else
                        {
                            // Mobile Empty-Target Feedback v1: neither an entity nor a
                            // walkable tile - a qualifying tap that produced nothing
                            // actionable. Acknowledge the tap itself rather than leave
                            // it silent; existing selection is already cleared above
                            // (pinnedEntity = null) exactly as it was before this change.
                            WorldEntityScreenAnchor.TriggerEmptyTapFeedback(capturedTapScreenPosition);
                        }
                    }
                }

                var fingers = Lean.Touch.LeanTouch.GetFingers(true, false);

                if (trackedFingerIndex != -1)
                {
                    var finger = FindFinger(fingers, trackedFingerIndex);
                    if (finger == null)
                    {
                        trackedFingerIndex = -1;
                    }
                    else if (finger.Up)
                    {
                        tapMachine.OnFingerUp(finger.ScreenPosition, Time.unscaledTime);
                        longPressMachine.OnFingerUp(Time.unscaledTime);
                        trackedFingerIndex = -1;
                    }
                    else
                    {
                        tapMachine.OnFingerMove(finger.ScreenPosition, Time.unscaledTime);
                        longPressMachine.OnFingerMove(finger.ScreenPosition, Time.unscaledTime);
                        longPressMachine.Tick(Time.unscaledTime);
                    }
                }
                else
                {
                    foreach (var finger in fingers)
                    {
                        if (finger.Down)
                        {
                            trackedFingerIndex = finger.Index;
                            tapMachine.OnFingerDown(finger.ScreenPosition, Time.unscaledTime);
                            longPressMachine.OnFingerDown(finger.ScreenPosition, Time.unscaledTime);
                            break;
                        }
                    }
                }
            }

            private void OnTapDetected(Vector2 screenPosition)
            {
                // UI wins over world targeting. Two separate UI surfaces exist
                // in this project: native ClassicUO Gumps (Primary Action,
                // Action Bar, any window - UIManager.IsMouseOverWorld, the same
                // check MobileInputController's joystick already uses) and
                // Unity UI panels (Backpack - LeanTouch.PointOverGui, the same
                // check GameController's own desktop mouse branch already uses
                // to suppress world input over Unity UI). Both must say "this
                // is the world" for the tap to count.
                if (Lean.Touch.LeanTouch.PointOverGui(screenPosition) || !UIManager.IsMouseOverWorld)
                {
                    return;
                }

                capturedTapScreenPosition = screenPosition;
                captureNextFrame = true;
            }

            private void OnLongPressDetected(Vector2 screenPosition)
            {
                // Same UI-exclusion rule as OnTapDetected (both Gump and
                // Unity-UI surfaces win over world targeting).
                if (Lean.Touch.LeanTouch.PointOverGui(screenPosition) || !UIManager.IsMouseOverWorld)
                {
                    return;
                }

                // Unlike OnTapDetected, do not defer to next frame: a long
                // press fires while the finger is still down, so mobile's
                // continuous Mouse.Position drive during a held touch (see
                // class doc) already keeps SelectedObject.LastObject live
                // this frame - the same assumption ContextMenuLongPressTrigger
                // relies on when it reads SelectedObject.Object inline for
                // its own long-press. Deferring a frame here would only risk
                // losing the pin if the finger lifts before that frame runs.
                // Ground/Land is intentionally a no-op: long-press only pins
                // Mobile/Item (no pathfinding invented for this gesture).
                //
                // Mobile Spell Casting & Targeting v1: same pre-emption as the
                // short-tap path - while the existing targeting system is active, a
                // long press completes it too, rather than pinning a selection the
                // player never asked for mid-cast.
                if (TargetManager.IsTargeting)
                {
                    SendToTargetManager(SelectedObject.LastObject);
                    return;
                }

                if (SelectedObject.LastObject is Item || SelectedObject.LastObject is Mobile)
                {
                    pinnedEntity = SelectedObject.LastObject;

                    // Mobile Long-Press Visual Feedback v1: fires only here, not
                    // from the short-tap captureNextFrame path above - the one
                    // thing that distinguishes "long press selected this" from
                    // an ordinary tap selection is that this call happens at all.
                    WorldEntityScreenAnchor.TriggerSelectionFeedback(pinnedEntity);

                    // Mobile UO Context Menu v1: long press is also "open the
                    // secondary action surface" for the same pin above - not a
                    // second selection, not a second pin. Open() itself decides
                    // whether there's anything to show (see its own doc).
                    ContextActionMenuPresenter.Instance?.Open(pinnedEntity);
                }
            }

            internal void ClearPin()
            {
                pinnedEntity = null;
            }

            // Mobile Spell Casting & Targeting v1: the exact decision tree
            // GameSceneInputHandler's own mouse-click-while-targeting branch already
            // uses (case Entity -> Target(serial), case Land -> Target(0, x, y, z,
            // isWet), case GameObject -> Target(graphic, x, y, z)) - reused verbatim,
            // not a new targeting resolution. TargetManager.Target() only sends the
            // existing PTargetObject/PTargetXYZ packet and lets the server decide
            // what happens; it never executes the spell/skill itself. Resolving
            // nothing (tapped empty space) is intentionally a no-op, matching
            // desktop: clicking nothing while targeting neither cancels nor moves.
            private static void SendToTargetManager(BaseGameObject resolved)
            {
                switch (resolved)
                {
                    case Entity entity:
                        TargetManager.Target(entity.Serial);
                        break;
                    case Land land:
                        TargetManager.Target(0, land.X, land.Y, land.Z, land.TileData.IsWet);
                        break;
                    case ClassicUO.Game.GameObjects.GameObject gameObject:
                        TargetManager.Target(gameObject.Graphic, gameObject.X, gameObject.Y, gameObject.Z);
                        break;
                }
            }

            // Mirrors GameSceneInputHandler.OnRightMouseDoubleClick()'s own
            // walkable-tile classification exactly (Land, or a Static/Multi/Item
            // flagged IsSurface) - not a new "is this walkable" answer, the same
            // one the existing desktop click-to-walk gesture already uses.
            private static bool TryResolveWalkDestination(BaseGameObject resolved, out int x, out int y, out int z)
            {
                x = y = z = 0;

                if (!(resolved is ClassicUO.Game.GameObjects.GameObject obj))
                {
                    return false;
                }

                if (obj is Land)
                {
                    x = obj.X;
                    y = obj.Y;
                    z = obj.Z;
                    return true;
                }

                if (obj is Static || obj is Multi || obj is Item)
                {
                    ref var itemdata = ref TileDataLoader.Instance.StaticData[obj.Graphic];
                    if (itemdata.IsSurface)
                    {
                        x = obj.X;
                        y = obj.Y;
                        z = obj.Z;
                        return true;
                    }
                }

                return false;
            }

            // Same call the desktop double-right-click gesture makes
            // (Pathfinder.WalkTo) - reused, not reimplemented. Deliberately does
            // not gate on ProfileManager.Current.EnablePathfind: that flag only
            // controls whether the *desktop* double-click gesture is armed
            // (defaults off, a power-user opt-in); on mobile, tap-to-move is the
            // primary movement paradigm, not an optional extra, so it shouldn't
            // silently no-op for players who never touched that desktop setting.
            // !Pathfinder.AutoWalking is kept, matching the existing gesture,
            // so a second tap mid-walk doesn't fight the in-flight path.
            private static void IssueWalk(int x, int y, int z)
            {
                if (Pathfinder.AutoWalking)
                {
                    return;
                }

                Pathfinder.WalkTo(x, y, z, 0);
            }

            private static Lean.Touch.LeanFinger FindFinger(System.Collections.Generic.List<Lean.Touch.LeanFinger> fingers, int index)
            {
                for (var i = 0; i < fingers.Count; i++)
                {
                    if (fingers[i].Index == index)
                    {
                        return fingers[i];
                    }
                }

                return null;
            }
        }
    }
}
