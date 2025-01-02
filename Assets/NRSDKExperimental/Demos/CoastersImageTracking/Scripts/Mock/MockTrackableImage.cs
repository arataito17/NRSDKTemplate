/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System;
using UnityEngine;

namespace NRKernal.Experimental.NRExamples
{
    [Serializable]
    public class MockTrackableImage
    {
        public int DataBaseIndex;
        public TrackingState TrackingState;
        public Vector3 CenterPosePos;
        public Vector3 CenterPoseRot;
        public float ExtentX;
        public float ExtentZ;
        public MockTrackableImage(int databaseIndex)
        {
            DataBaseIndex = databaseIndex;
            TrackingState = TrackingState.Paused;
        }

        public int GetCoastersDataBaseIndex()
        {
            return DataBaseIndex;
        }

        public TrackingState GetTrackingState()
        {
            return TrackingState;
        }

        public Pose GetCenterPose()
        {
            return new Pose(CenterPosePos, Quaternion.Euler(CenterPoseRot));
        }
    }
}



