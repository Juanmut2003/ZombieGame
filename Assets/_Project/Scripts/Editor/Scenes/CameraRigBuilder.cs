#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using ZombieGame.Core.Rendering;

namespace ZombieGame.Editor.Scenes
{
    /// <summary>
    /// Builds the one camera setup every scene uses, so no scene can quietly differ from the rest.
    /// </summary>
    public static class CameraRigBuilder
    {
        private static readonly Color Background = new(0.06f, 0.07f, 0.08f, 1f);

        /// <summary>
        /// Creates the main camera with the pixel-perfect rig and an optional follow target.
        /// </summary>
        /// <param name="scene">Scene to put the camera in.</param>
        /// <param name="followTarget">What the camera should watch, if anything.</param>
        /// <param name="followSharpness">How fast the camera closes the gap, per second.</param>
        /// <param name="deadzoneRadius">How far the target drifts before the camera moves.</param>
        public static Camera Create(
            Scene scene,
            Transform? followTarget = null,
            float followSharpness = 8f,
            float deadzoneRadius = 0.5f)
        {
            GameObject cameraObject = SceneBuilder.CreateObject(scene, "Main Camera");
            cameraObject.tag = "MainCamera";

            // Ten units back: far enough that nothing in a 2D scene ends up behind the near plane,
            // close enough to stay well inside the far plane.
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = PixelPerfectSettings.OrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            // URP keeps its per-camera settings in a second component. Asking for it here means it
            // is serialised into the scene rather than being added at runtime on first render.
            camera.GetUniversalAdditionalCameraData();

            ConfigurePixelPerfect(cameraObject);
            AttachFollow(cameraObject, followTarget, followSharpness, deadzoneRadius);

            return camera;
        }

        private static void ConfigurePixelPerfect(GameObject cameraObject)
        {
            PixelPerfectCamera pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = PixelPerfectSettings.PixelsPerUnit;
            pixelPerfect.refResolutionX = PixelPerfectSettings.ReferenceWidth;
            pixelPerfect.refResolutionY = PixelPerfectSettings.ReferenceHeight;

            // PixelSnapping keeps every sprite on the pixel grid while still letting the camera
            // move smoothly. UpscaleRenderTexture would render at 480x270 and scale the whole
            // image up, which is more authentic but makes camera movement visibly step.
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;

            // No bars. At an aspect ratio other than 16:9 the player simply sees a little more of
            // the world, which suits a top-down game better than letterboxing it.
            pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
        }

        private static void AttachFollow(
            GameObject cameraObject, Transform? target, float sharpness, float deadzoneRadius)
        {
            CameraFollow follow = cameraObject.AddComponent<CameraFollow>();

            var serialized = new SerializedObject(follow);
            serialized.FindProperty("_target").objectReferenceValue = target;
            serialized.FindProperty("_sharpness").floatValue = sharpness;
            serialized.FindProperty("_deadzoneRadius").floatValue = deadzoneRadius;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (target is not null)
            {
                follow.SnapToTarget();
            }
        }
    }
}
