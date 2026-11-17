#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;
using ZombieGame.Core.Events;
using ZombieGame.Core.SceneFlow;

namespace ZombieGame.Tests.EditMode
{
    [TestFixture]
    public sealed class SceneLoaderTests
    {
        /// <summary>A load that finishes exactly when the test says so.</summary>
        private sealed class FakeOperation : ISceneLoadOperation
        {
            public bool IsDone { get; private set; }

            public float Progress { get; private set; }

            public void Advance(float progress) => Progress = progress;

            public void Finish()
            {
                Progress = 1f;
                IsDone = true;
            }
        }

        private sealed class FakeBackend : ISceneLoadBackend
        {
            private readonly bool _completeImmediately;

            public FakeBackend(string activeScene = "Boot", bool completeImmediately = false)
            {
                ActiveSceneName = activeScene;
                _completeImmediately = completeImmediately;
            }

            public string ActiveSceneName { get; private set; }

            public List<string> Requested { get; } = new();

            public FakeOperation? Current { get; private set; }

            public ISceneLoadOperation LoadSingle(string sceneName)
            {
                Requested.Add(sceneName);
                var operation = new FakeOperation();
                Current = operation;

                if (_completeImmediately)
                {
                    ActiveSceneName = sceneName;
                    operation.Finish();
                }

                return operation;
            }

            /// <summary>Finishes the running load, as the engine would a frame or two later.</summary>
            public void CompletePending(string sceneName)
            {
                ActiveSceneName = sceneName;
                Current?.Finish();
            }
        }

        [Test]
        public void Load_AsksTheBackendForTheScene()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);

            Assert.That(loader.Load("MainMenu"), Is.True);
            Assert.That(backend.Requested, Is.EqualTo(new[] { "MainMenu" }));
            Assert.That(loader.IsLoading, Is.True);
            Assert.That(loader.PendingScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void ASecondLoadIsRefusedWhileOneIsRunning()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);
            loader.Load("MainMenu");

            Assert.That(loader.Load("Sandbox"), Is.False, "a double click must not race");
            Assert.That(backend.Requested, Has.Count.EqualTo(1));
            Assert.That(loader.PendingScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void LoadingTheSceneAlreadyActiveDoesNothing()
        {
            var backend = new FakeBackend(activeScene: "MainMenu");
            var loader = new SceneLoader(backend);

            Assert.That(loader.Load("MainMenu"), Is.False);
            Assert.That(backend.Requested, Is.Empty);
        }

        [Test]
        public void AnotherLoadIsPossibleOnceTheFirstFinished()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);
            loader.Load("MainMenu");

            backend.CompletePending("MainMenu");
            loader.Tick();

            Assert.That(loader.IsLoading, Is.False);
            Assert.That(loader.Load("Sandbox"), Is.True);
            Assert.That(backend.Requested, Is.EqualTo(new[] { "MainMenu", "Sandbox" }));
        }

        [Test]
        public void LoadStarted_FiresWithBothSceneNames()
        {
            var backend = new FakeBackend(activeScene: "Boot");
            var loader = new SceneLoader(backend);
            var started = new List<SceneLoadStartedEvent>();
            loader.LoadStarted += started.Add;

            loader.Load("MainMenu");

            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(started[0].FromScene, Is.EqualTo("Boot"));
            Assert.That(started[0].ToScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void LoadCompleted_FiresOnlyOnceTheSceneIsIn()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);
            var completed = new List<SceneLoadCompletedEvent>();
            loader.LoadCompleted += completed.Add;

            loader.Load("MainMenu");
            loader.Tick();
            Assert.That(completed, Is.Empty, "still loading");

            backend.CompletePending("MainMenu");
            loader.Tick();

            Assert.That(completed, Has.Count.EqualTo(1));
            Assert.That(completed[0].SceneName, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void TickingAfterCompletionDoesNotFireAgain()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);
            var completed = new List<SceneLoadCompletedEvent>();
            loader.LoadCompleted += completed.Add;
            loader.Load("MainMenu");
            backend.CompletePending("MainMenu");

            loader.Tick();
            loader.Tick();
            loader.Tick();

            Assert.That(completed, Has.Count.EqualTo(1));
        }

        [Test]
        public void ABackendThatFinishesInstantlyDoesNotLeaveTheLoaderStuck()
        {
            var backend = new FakeBackend(completeImmediately: true);
            var loader = new SceneLoader(backend);
            var completed = new List<SceneLoadCompletedEvent>();
            loader.LoadCompleted += completed.Add;

            Assert.That(loader.Load("MainMenu"), Is.True);

            Assert.That(loader.IsLoading, Is.False);
            Assert.That(completed, Has.Count.EqualTo(1));
        }

        [Test]
        public void BothEventsAlsoGoOutOnTheEventBus()
        {
            var bus = new EventBus();
            var started = new List<SceneLoadStartedEvent>();
            var completed = new List<SceneLoadCompletedEvent>();
            bus.Subscribe<SceneLoadStartedEvent>(started.Add);
            bus.Subscribe<SceneLoadCompletedEvent>(completed.Add);

            var backend = new FakeBackend();
            var loader = new SceneLoader(backend, bus);
            loader.Load("MainMenu");
            backend.CompletePending("MainMenu");
            loader.Tick();

            Assert.That(started, Has.Count.EqualTo(1));
            Assert.That(completed, Has.Count.EqualTo(1));
            Assert.That(completed[0].SceneName, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void Progress_FollowsTheOperationAndReadsFullWhenIdle()
        {
            var backend = new FakeBackend();
            var loader = new SceneLoader(backend);

            Assert.That(loader.Progress, Is.EqualTo(1f), "nothing is loading");

            loader.Load("MainMenu");
            backend.Current!.Advance(0.4f);
            Assert.That(loader.Progress, Is.EqualTo(0.4f).Within(0.0001f));

            backend.CompletePending("MainMenu");
            loader.Tick();
            Assert.That(loader.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void ActiveScene_ComesFromTheBackend()
        {
            var backend = new FakeBackend(activeScene: "Boot");
            var loader = new SceneLoader(backend);

            Assert.That(loader.ActiveScene, Is.EqualTo("Boot"));

            loader.Load("MainMenu");
            backend.CompletePending("MainMenu");
            loader.Tick();

            Assert.That(loader.ActiveScene, Is.EqualTo("MainMenu"));
        }

        [Test]
        public void Load_RejectsAnEmptySceneName()
        {
            var loader = new SceneLoader(new FakeBackend());

            Assert.Throws<ArgumentException>(() => loader.Load(" "));
            Assert.Throws<ArgumentException>(() => loader.Load(string.Empty));
        }

        [Test]
        public void Constructor_RejectsAMissingBackend()
        {
            Assert.Throws<ArgumentNullException>(() => new SceneLoader(null!));
        }

        [Test]
        public void TickWithoutALoadIsHarmless()
        {
            var loader = new SceneLoader(new FakeBackend());

            Assert.DoesNotThrow(() => loader.Tick());
            Assert.That(loader.IsLoading, Is.False);
        }
    }
}
