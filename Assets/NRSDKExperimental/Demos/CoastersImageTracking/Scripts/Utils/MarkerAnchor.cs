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

    public class MarkerAnchor: MonoBehaviour
    {
        private NRTrackableImage m_Image;

        public void AttachTo(NRTrackableImage image)
        {
            m_Image = image;
        }
        protected virtual void Update()
        {
            if(m_Image != null)
            {
                var pose = m_Image.GetCenterPose();

                transform.position = pose.position;
                transform.rotation = pose.rotation;
            }
        }
    }
}
