/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

namespace NRKernal.Experimental
{
    using System;
    using UnityEngine;

    /// <summary> A trackable image in the real world detected by NRInternal. </summary>
    public static class NRTrackableImageExtension
    {
        public static UInt32 GetIdentify(this NRTrackableImage nrTrackableImage)
        {
            return nrTrackableImage.TrackableSubsystem.GetIdentify(nrTrackableImage.TrackableNativeHandle);
        }

        public static int GetCoastersDataBaseIndex(this NRTrackableImage nrTrackableImage)
        {
            UInt32 identify = nrTrackableImage.TrackableSubsystem.GetIdentify(nrTrackableImage.TrackableNativeHandle);
            identify &= 0X000000FF;
            return (int)identify;
        }

        /// <summary> 
        /// Get the color of trackable. 
        /// 0 - green
        /// 1 - blue
        /// 2 - orange
        /// other - red
        /// </summary>
        public static Color GetDebugColor(this NRTrackableImage nrTrackableImage)
        {
            int debugColorIndex = nrTrackableImage.GetDebugColorIndex();
            switch (debugColorIndex)
            {
                case 0:
                    return Color.green;
                case 1:
                    return Color.blue;
                case 2:
                    return new Color32(255, 165, 0,0);
            }
            return Color.red;
        }        
        
        /// <summary> 
        /// Get the color index of trackable. 
        /// 0 - 0
        /// 1 - 1
        /// 2 - 2
        /// other - 3
        /// </summary>
        public static int GetDebugColorIndex(this NRTrackableImage nrTrackableImage)
        {
            UInt32 identify = nrTrackableImage.TrackableSubsystem.GetIdentify(nrTrackableImage.TrackableNativeHandle);
            identify &= 0X0000FF00;
            identify >>= 8;
            switch (identify)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 2:
                    return 2;
            }
            return 3;
        }

    }
}
