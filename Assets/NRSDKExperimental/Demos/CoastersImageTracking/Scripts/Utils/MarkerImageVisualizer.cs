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
    using UnityEngine;

#if UNITY_EDITOR
    using NRTrackableImage = MockTrackableImage;
#endif

    public class MarkerImageVisualizer: MonoBehaviour
    {
        /// <summary> The TrackingImage to visualize. </summary>
        public NRTrackableImage Image;

        public TextMesh Title;

        public void Init(NRTrackableImage image, int dataBaseIndex)
        {
            Image = image;
            transform.parent = transform;
            if (Title != null)
            {
                Title.text = $"{dataBaseIndex}";
            }
        }

        /// <summary> Updates this object. </summary>
        public void Update()
        {
            if (Image == null || Image.GetTrackingState() != TrackingState.Tracking)
            {
                return;
            }

            var center = Image.GetCenterPose();
            transform.position = center.position;
            transform.rotation = center.rotation;

            var dir = Title.transform.position - NRSessionManager.Instance.NRHMDPoseTracker.centerCamera.transform.position;
            dir.y = 0;
            Title.transform.forward = dir;
        }

    }
}
