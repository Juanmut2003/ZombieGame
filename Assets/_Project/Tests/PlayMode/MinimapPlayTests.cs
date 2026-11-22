#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using ZombieGame.UI;

namespace ZombieGame.Tests.PlayMode
{
    /// <summary>
    /// The map in the corner, and the button that opens it out.
    /// </summary>
    /// <remarks>
    /// Three things have to be true for a button in a zone to work at all, and none of them is
    /// obvious from looking at the scene: the button exists, something is listening to it, and
    /// the zone has an event system to deliver the click. Zones had none until the map needed
    /// one, so that last part is the one worth pinning down.
    /// </remarks>
    [TestFixture]
    public sealed class MinimapPlayTests
    {
        private MinimapPresenter _map = null!;

        [UnitySetUp]
        public IEnumerator LoadTown()
        {
            yield return SceneManager.LoadSceneAsync("Mission_Town", LoadSceneMode.Single);
            yield return new WaitForFixedUpdate();

            GameObject? presenter = GameObject.Find("Minimap Presenter");
            Assert.That(presenter, Is.Not.Null, "the town should have a minimap");
            _map = presenter!.GetComponent<MinimapPresenter>();

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheZoneCanTakeAClickAtAll()
        {
            Assert.That(
                Object.FindAnyObjectByType<EventSystem>(),
                Is.Not.Null,
                "without an event system every button in the zone is a picture of a button");

            yield return null;
        }

        [UnityTest]
        public IEnumerator ItStartsInTheCorner()
        {
            Assert.That(_map.IsWide, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheButtonOpensTheMapOut()
        {
            GameObject? buttonObject = GameObject.Find("Minimap Toggle");
            Assert.That(buttonObject, Is.Not.Null, "there should be a button under the map");

            var button = buttonObject!.GetComponent<Button>();
            Assert.That(button, Is.Not.Null);

            // Through the button's own event, not by calling the presenter: the wiring between
            // the two is the part that can be left out.
            button.onClick.Invoke();
            yield return null;

            Assert.That(_map.IsWide, Is.True, "the map should have opened out");

            button.onClick.Invoke();
            yield return null;

            Assert.That(_map.IsWide, Is.False, "and closed again");
        }

        [UnityTest]
        public IEnumerator TheWideMapSeesMoreOfTheZone()
        {
            GameObject? cameraObject = GameObject.Find("Minimap Camera");
            Assert.That(cameraObject, Is.Not.Null);
            var view = cameraObject!.GetComponent<Camera>();

            float corner = view.orthographicSize;
            _map.Show(wide: true);
            yield return null;

            Assert.That(view.orthographicSize, Is.GreaterThan(corner * 2f));
        }

        [UnityTest]
        public IEnumerator TheMapDoesNotGiveAwayTheHorde()
        {
            GameObject? cameraObject = GameObject.Find("Minimap Camera");
            Assert.That(cameraObject, Is.Not.Null);
            var view = cameraObject!.GetComponent<Camera>();

            Assert.That(
                view.cullingMask & ZombieGame.Core.GameLayers.HordeMask,
                Is.Zero,
                "a map that lists every zombie is a radar, and makes sneaking pointless");

            // And it still shows the world, or the fix would be a blank square.
            Assert.That(view.cullingMask, Is.Not.Zero);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TheBodiesAreOnTheLayerTheMapSkips()
        {
            // The camera skipping a layer only helps if the bodies are actually on it, and they
            // are put there once, where they are made, rather than in the scene.
            yield return new WaitForSeconds(0.5f);

            GameObject? director = GameObject.Find("Zombie Director");
            Assert.That(director, Is.Not.Null);

            var views = director!.GetComponentsInChildren<ZombieGame.AI.ZombieView>(true);
            Assert.That(views, Is.Not.Empty, "the town should have a horde in it");

            foreach (ZombieGame.AI.ZombieView body in views)
            {
                Assert.That(
                    body.gameObject.layer,
                    Is.EqualTo(ZombieGame.Core.GameLayers.HordeIndex),
                    $"'{body.name}' would show up on the map");
            }
        }

        [UnityTest]
        public IEnumerator TheWideMapIsBiggerOnScreen()
        {
            GameObject? frame = GameObject.Find("Minimap");
            Assert.That(frame, Is.Not.Null);
            var rect = (RectTransform)frame!.transform;

            float corner = rect.sizeDelta.x;
            _map.Show(wide: true);
            yield return null;

            Assert.That(rect.sizeDelta.x, Is.GreaterThan(corner));
        }
    }
}
