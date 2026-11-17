#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Renders a scene's main camera to a PNG from batch mode, so a change can be looked at
    /// without opening the editor.
    /// </summary>
    /// <remarks>
    /// This is the project's answer to "does it actually look right?" in an automated run: nothing
    /// else in the pipeline catches a scene that renders black, a missing light, or a sprite that
    /// imported wrong. The output is a picture a human reads, not an assertion — it does not
    /// replace playing the game.
    /// Run with
    /// <c>-executeMethod ZombieGame.Editor.Scenes.SceneScreenshot.CaptureFromCommandLine</c> plus
    /// <c>-scene &lt;asset path&gt;</c>, <c>-output &lt;png path&gt;</c> and optionally
    /// <c>-width</c> / <c>-height</c>.
    /// </remarks>
    public static class SceneScreenshot
    {
        /// <summary>The game's reference resolution, from the art direction in PROJECT.md.</summary>
        public const int ReferenceWidth = 480;

        /// <summary>The game's reference resolution, from the art direction in PROJECT.md.</summary>
        public const int ReferenceHeight = 270;

        /// <summary>
        /// Captures the sandbox at twice the reference resolution. Integer scaling keeps every
        /// source pixel a clean 2x2 block, so what you see is what the game renders.
        /// </summary>
        [MenuItem("ZombieGame/Scenes/Capture Sandbox Screenshot")]
        public static void CaptureSandbox()
        {
            string output = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? ".", "Sandbox.png");
            Capture(ProjectScenes.Sandbox, output, ReferenceWidth * 2, ReferenceHeight * 2);
        }

        /// <summary>Entry point for batch mode; reads its arguments from the command line.</summary>
        public static void CaptureFromCommandLine()
        {
            string scenePath = ArgumentOr("-scene", ProjectScenes.Sandbox);
            string output = ArgumentOr("-output", "Screenshot.png");
            int width = int.Parse(ArgumentOr("-width", (ReferenceWidth * 2).ToString()));
            int height = int.Parse(ArgumentOr("-height", (ReferenceHeight * 2).ToString()));

            Capture(scenePath, output, width, height);
        }

        /// <summary>
        /// Opens <paramref name="scenePath"/>, renders its main camera and writes a PNG.
        /// </summary>
        /// <exception cref="InvalidOperationException">The scene has no usable camera.</exception>
        public static void Capture(string scenePath, string outputPath, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width), $"{width}x{height}", "A screenshot needs a positive size.");
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Camera? camera = FindCamera(scene);
            if (camera is null)
            {
                throw new InvalidOperationException($"'{scenePath}' has no camera to render from.");
            }

            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
            {
                sRGB = true,
            };
            RenderTexture target = RenderTexture.GetTemporary(descriptor);
            RenderTexture? previousActive = RenderTexture.active;
            RenderTexture? previousTarget = camera.targetTexture;

            var readback = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);

            try
            {
                camera.targetTexture = target;
                Render(camera, target);

                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readback.Apply();

                string? directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                File.WriteAllBytes(outputPath, readback.EncodeToPNG());
                Debug.Log($"[Screenshot] {scenePath} -> {outputPath} ({width}x{height})");
            }
            finally
            {
                RenderTexture.active = previousActive;
                camera.targetTexture = previousTarget;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(readback);
            }
        }

        private static void Render(Camera camera, RenderTexture target)
        {
            // URP ignores Camera.Render; a render request is the supported way to drive a single
            // camera by hand. Older pipelines still need the direct call, hence the fallback.
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (RenderPipeline.SupportsRenderRequest(camera, request))
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
                return;
            }

            camera.Render();
        }

        private static Camera? FindCamera(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Camera? found = root.GetComponentInChildren<Camera>(includeInactive: false);
                if (found is not null)
                {
                    return found;
                }
            }

            return Camera.main;
        }

        private static string ArgumentOr(string name, string fallback)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], name, StringComparison.Ordinal))
                {
                    return arguments[i + 1];
                }
            }

            return fallback;
        }
    }
}
