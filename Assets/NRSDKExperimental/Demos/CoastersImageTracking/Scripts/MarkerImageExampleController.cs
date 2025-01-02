/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

namespace NRKernal.Experimental.NRExamples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UnityEngine;

#if UNITY_EDITOR
    using NRTrackableImage = MockTrackableImage;
#endif

    /// <summary> Controller for TrackingImage example. </summary>
    [HelpURL("https://developer.xreal.com/develop/unity/image-tracking")]
    public class MarkerImageExampleController : MonoBehaviour
    {
        const string TAG = "MarkerImageExampleController";

        public event Action<NRTrackableImage> OnImageLoaded;
        public event Action<NRTrackableImage> OnImageLost;

        /// <summary> A prefab for visualizing an TrackingImage. </summary>
        public MarkerImageVisualizer MarkerImageVisualizerPrefab;



        /// <summary> The visualizers. </summary>
        private Dictionary<int, MarkerImageVisualizer> m_Visualizers
            = new Dictionary<int, MarkerImageVisualizer>();

        /// <summary> The temporary tracking images. </summary>
        private List<NRTrackableImage> m_TempTrackingImages = new List<NRTrackableImage>();

        /// <summary> Updates this object. </summary>
        public void Update()
        {
#if UNITY_EDITOR
            MockUpdate();
#else
            RealUpdate();
#endif
        }


#if !UNITY_EDITOR && UNITY_ANDROID
        void RealUpdate()
        {
            // Check that motion tracking is tracking.
            if (NRFrame.SessionStatus != SessionState.Running)
            {
                return;
            }

            // Get updated augmented images for this frame.
            NRFrame.GetTrackables<NRTrackableImage>(m_TempTrackingImages, NRTrackableQueryFilter.All);

            // Create visualizers and anchors for updated augmented images that are tracking and do not previously
            // have a visualizer. Remove visualizers for stopped images.
            foreach (var image in m_TempTrackingImages)
            {
                int dataBaseIndex = image.GetCoastersDataBaseIndex();

                NRDebugger.Debug($"[{TAG}] Image wholeId:{image.GetIdentify()} databaseId:{dataBaseIndex} colorId:{image.GetDebugColorIndex()} trackingState:{image.GetTrackingState()}");
                MarkerImageVisualizer visualizer = null;
                m_Visualizers.TryGetValue(dataBaseIndex, out visualizer);
                if (image.GetTrackingState() == TrackingState.Tracking && visualizer == null)
                {
                    NRDebugger.Info("Create new MarkerImageVisualizer!");
                    // Create an anchor to ensure that NRSDK keeps tracking this augmented image.
                    visualizer = Instantiate(MarkerImageVisualizerPrefab, image.GetCenterPose().position, image.GetCenterPose().rotation);
                    visualizer.Init(image, dataBaseIndex);
                    m_Visualizers.Add(dataBaseIndex, visualizer);
                    NRDebugger.Info($"[{TAG}] OnImageLoad {image.GetTrackingState()} {image.GetIdentify()} id:{dataBaseIndex} colorId: {image.GetDebugColorIndex()}");
                    OnImageLoaded?.Invoke(image);
                }
                else if (image.GetTrackingState() != TrackingState.Tracking && visualizer != null)
                {
                    NRDebugger.Info($"[{TAG}] OnImageLost {image.GetTrackingState()} {image.GetIdentify()} id:{dataBaseIndex} colorId: {image.GetDebugColorIndex()}");
                    OnImageLost?.Invoke(image);

                    m_Visualizers.Remove(dataBaseIndex);
                    Destroy(visualizer.gameObject);
                }

            }
        }
#endif
#if UNITY_EDITOR
        void MockUpdate()
        {
            // Get updated augmented images for this frame.
            MockTrackableImageFactory.GetTrackables(m_TempTrackingImages);

            // Create visualizers and anchors for updated augmented images that are tracking and do not previously
            // have a visualizer. Remove visualizers for stopped images.
            foreach (var image in m_TempTrackingImages)
            {
                int dataBaseIndex = image.GetCoastersDataBaseIndex();

                NRDebugger.Debug($"[{TAG}] Image databaseId:{dataBaseIndex}  trackingState:{image.GetTrackingState()}");
                MarkerImageVisualizer visualizer = null;
                m_Visualizers.TryGetValue(dataBaseIndex, out visualizer);
                if (image.GetTrackingState() != TrackingState.Tracking && visualizer != null)
                {
                    NRDebugger.Info($"[{TAG}] OnImageLost {image.GetTrackingState()} id:{dataBaseIndex} ");
                    OnImageLost?.Invoke(image);

                    m_Visualizers.Remove(dataBaseIndex);
                    Destroy(visualizer.gameObject);
                }
                else if (image.GetTrackingState() == TrackingState.Tracking && visualizer == null)
                {
                    NRDebugger.Info("Create new MarkerImageVisualizer!");
                    // Create an anchor to ensure that NRSDK keeps tracking this augmented image.
                    visualizer = Instantiate(MarkerImageVisualizerPrefab, image.GetCenterPose().position, image.GetCenterPose().rotation);
                    visualizer.Init(image, dataBaseIndex);
                    m_Visualizers.Add(dataBaseIndex, visualizer);
                    NRDebugger.Info($"[{TAG}] OnImageLoad {image.GetTrackingState()}  id:{dataBaseIndex} ");
                    OnImageLoaded?.Invoke(image);
                }


            }
        }
#endif
        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                _ = EnableImageTrackingAsync();
            }

        }

        async Task EnableImageTrackingAsync()
        {
            NRDebugger.Info("[TrackingImageExampleController] EnableImageTrackingAsync");
            var config = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
            int tryCount = 0;
            while (!NRSessionManager.Instance.IsRunning && tryCount < 10)
            {
                tryCount++;
                await Task.Delay(500);
            }
            if (!NRSessionManager.Instance.IsRunning)
            {
                NRDebugger.Warning($"[TrackingImageExampleController] Session is not running after 5 seconds");
                return;
            }

            if (config.ImageTrackingMode == TrackableImageFindingMode.ENABLE)
            {
                await DisableImageTracking();
            }

            await EnableImageTracking();
        }

        /// <summary> Enables the image tracking. </summary>
        public async Task EnableImageTracking()
        {
            NRDebugger.Info($"[TrackingImageExampleController] EnableImageTracking");
            var config = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
            config.ImageTrackingMode = TrackableImageFindingMode.ENABLE;
            await NRSessionManager.Instance.SetConfiguration(config);
        }

        /// <summary> Disables the image tracking. </summary>
        public async Task DisableImageTracking()
        {
            NRDebugger.Info($"[TrackingImageExampleController] DisableImageTracking");
            var config = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
            config.ImageTrackingMode = TrackableImageFindingMode.DISABLE;
            await NRSessionManager.Instance.SetConfiguration(config);
        }
    }
}
