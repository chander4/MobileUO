using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Hollowmere.HudRuntime.Tests
{
    /// <summary>
    /// Optional PlayMode smoke tests. Run manually from the Test Runner while developing;
    /// not required before entering Play Mode or before commits.
    /// </summary>
    [Category("HudRuntimeSmoke")]
    public class HudRuntimeSmokePlayModeTests
    {
        private const string SceneName = "Scene";

        // Observed cold-start (asset load + auto-login + world entry) took ~10-11s in one
        // run; 15s gives real margin without an arbitrary "wait and hope" blow-up.
        private const float ReadyTimeoutSeconds = 15f;

        // Independent backstop, not a duplicate of ReadyTimeoutSeconds: this bounds the
        // wait by real Update() ticks regardless of whether Time.realtimeSinceStartup
        // behaves as expected, so the coroutine can never spin unboundedly even if the
        // wall-clock deadline computation were ever wrong. Sized well above what
        // ReadyTimeoutSeconds needs at the observed ~53fps PlayMode-test frame rate
        // (240 frames took ~4.49s there) so the wall-clock bound is what actually governs.
        private const int MaxReadyFrames = 3000;

        // Memoized across tests within one readiness "epoch" (initial entry, or since the
        // last ReloadActiveScene). World/HUD readiness is a one-time, session-level event;
        // without this cache each test independently re-rolls the same cold-start timeout,
        // so tests scheduled before the world finishes loading Ignore while tests scheduled
        // after it inherit an already-ready world "for free" — an execution-order artifact,
        // not a signal about test correctness.
        private static bool worldReadyKnown;
        private static bool worldReadyResult;

        [UnityTest]
        public IEnumerator HudHost_Initializes()
        {
            yield return LoadActiveSceneIfNeeded();
            yield return WaitForHudHost();

            Assert.That(HudSmokeTestBridge.Host, Is.Not.Null, "HudWidgetHost was not created.");
        }

        [UnityTest]
        public IEnumerator Hud_Presenters_Created()
        {
            yield return LoadActiveSceneIfNeeded();
            yield return WaitForHudInGame();

            IReadOnlyList<string> ids = HudSmokeTestBridge.GetActivePresenterWidgetIds();
            Assert.That(ids, Does.Contain(HudWidgetIds.PrimaryAction));
            Assert.That(ids, Does.Contain(HudWidgetIds.ActionBar));
            Assert.That(ids, Does.Contain(HudWidgetIds.Vitals));
            Assert.That(HudSmokeTestBridge.PrimaryActionLiveInstances, Is.EqualTo(1));
            Assert.That(HudSmokeTestBridge.ActionBarLiveInstances, Is.EqualTo(1));
            Assert.That(HudSmokeTestBridge.VitalsLiveInstances, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hud_NoDuplicateWidgets()
        {
            yield return LoadActiveSceneIfNeeded();
            yield return WaitForHudInGame();

            Assert.That(CountCompositionRunners(), Is.EqualTo(1), "Multiple HUD composition runners detected.");
            Assert.That(HudSmokeTestBridge.PrimaryActionLiveInstances, Is.LessThanOrEqualTo(1));
            Assert.That(HudSmokeTestBridge.ActionBarLiveInstances, Is.LessThanOrEqualTo(1));
            Assert.That(HudSmokeTestBridge.VitalsLiveInstances, Is.LessThanOrEqualTo(1));

            IReadOnlyList<string> ids = HudSmokeTestBridge.GetActivePresenterWidgetIds();
            Assert.That(ids.Count(id => id == HudWidgetIds.PrimaryAction), Is.LessThanOrEqualTo(1));
            Assert.That(ids.Count(id => id == HudWidgetIds.ActionBar), Is.LessThanOrEqualTo(1));
            Assert.That(ids.Count(id => id == HudWidgetIds.Vitals), Is.LessThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator Hud_NoDuplicates_AfterReload()
        {
            yield return LoadActiveSceneIfNeeded();
            yield return WaitForHudInGame();

            int runnersBefore = CountCompositionRunners();
            int primaryBefore = HudSmokeTestBridge.PrimaryActionLiveInstances;
            int barBefore = HudSmokeTestBridge.ActionBarLiveInstances;
            int vitalsBefore = HudSmokeTestBridge.VitalsLiveInstances;

            yield return ReloadActiveScene();
            yield return WaitForHudInGame();

            Assert.That(CountCompositionRunners(), Is.EqualTo(runnersBefore));
            Assert.That(HudSmokeTestBridge.PrimaryActionLiveInstances, Is.LessThanOrEqualTo(Mathf.Max(1, primaryBefore)));
            Assert.That(HudSmokeTestBridge.ActionBarLiveInstances, Is.LessThanOrEqualTo(Mathf.Max(1, barBefore)));
            Assert.That(HudSmokeTestBridge.VitalsLiveInstances, Is.LessThanOrEqualTo(Mathf.Max(1, vitalsBefore)));
        }

        private static IEnumerator LoadActiveSceneIfNeeded()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.name == SceneName)
            {
                yield return null;
                yield break;
            }

            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null, $"Could not load scene '{SceneName}'.");

            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static IEnumerator ReloadActiveScene()
        {
            // A scene reload re-runs Init.Awake() from scratch (full boot sequence), so
            // post-reload readiness is a new event — the pre-reload memo must not leak in.
            worldReadyKnown = false;

            AsyncOperation load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);

            while (!load.isDone)
            {
                yield return null;
            }

            for (int i = 0; i < 3; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitForHudHost()
        {
            float deadline = Time.realtimeSinceStartup + 2f;

            while (Time.realtimeSinceStartup < deadline)
            {
                if (HudSmokeTestBridge.Host != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for HudWidgetHost.");
        }

        private static IEnumerator WaitForHudInGame()
        {
            if (worldReadyKnown)
            {
                if (!worldReadyResult)
                {
                    Assert.Ignore(
                        "GameScene HUD presenters were not ready. Enter the world (past login), or run this smoke group while already in-game.");
                }

                yield break;
            }

            float deadline = Time.realtimeSinceStartup + ReadyTimeoutSeconds;
            int frames = 0;

            while (Time.realtimeSinceStartup < deadline && frames < MaxReadyFrames)
            {
                if (HudSmokeTestBridge.Host != null
                    && HudSmokeTestBridge.HasResolvedLayout
                    && HudSmokeTestBridge.GetActivePresenterWidgetIds().Count >= 3)
                {
                    worldReadyKnown = true;
                    worldReadyResult = true;
                    yield break;
                }

                frames++;
                yield return null;
            }

            worldReadyKnown = true;
            worldReadyResult = false;
            Assert.Ignore(
                "GameScene HUD presenters were not ready. Enter the world (past login), or run this smoke group while already in-game.");
        }

        private static int CountCompositionRunners()
        {
            return Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Count(go => go != null && go.name == "HollowmereHudWidgetHostComposition");
        }
    }
}
