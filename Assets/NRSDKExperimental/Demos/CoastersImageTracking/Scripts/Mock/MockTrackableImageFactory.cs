/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace NRKernal.Experimental.NRExamples
{
    public class MockTrackableImageFactory : SingletonBehaviour<MockTrackableImageFactory>
    {

        public List<MockTrackableImage> m_TrackingImageList;
        public List<ToggleGroup<MockTrackableImage>> m_ToggleGroups;
        public static void GetTrackables(List<MockTrackableImage> trackingImageList)
        {
            trackingImageList.Clear();
            for (int i = 0; i < Instance.m_TrackingImageList.Count; i++)
            {
                var item = Instance.m_TrackingImageList[i];
                trackingImageList.Add(item);
            }
            
        }

        protected override void Awake()
        {
            base.Awake();
            m_ToggleGroups = new List<ToggleGroup<MockTrackableImage>>();
            m_TrackingImageList = new List<MockTrackableImage>();

            NewMarkerGroup(0, 1, new Vector3(0, -0.2f, 3));
            NewMarkerGroup(2, 4, new Vector3(0.3f, -0.2f, 2));
            NewMarkerGroup(5, 10, new Vector3(-0.3f, -0.2f, 2));
        }

        private void ToggleGroup_OnIndexChanged(ToggleGroup<MockTrackableImage> toggleGroup, int oldIndex, int newIndex)
        {
            if (toggleGroup.TryGetData(oldIndex, out var oldImage))
            {
                oldImage.TrackingState = TrackingState.Paused;
            }
            if (toggleGroup.TryGetData(newIndex, out var newImage))
            {
                newImage.TrackingState = TrackingState.Tracking;
            }
        }

        private ToggleGroup<MockTrackableImage> NewMarkerGroup(int startIndex, int endIndex, Vector3 mockPos)
        {
            ToggleGroup<MockTrackableImage> toggleGroup = new ToggleGroup<MockTrackableImage>();
            for (int i = startIndex; i <= endIndex; ++i)
            {
                var trackableImage = new MockTrackableImage(i);
                m_TrackingImageList.Add(trackableImage);
                trackableImage.CenterPosePos = mockPos;
                toggleGroup.Add(i, trackableImage);
            }

            m_ToggleGroups.Add(toggleGroup);
            toggleGroup.OnIndexChanged += ToggleGroup_OnIndexChanged;

            return toggleGroup;
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < m_ToggleGroups.Count; ++i)
            {
                GUILayout.Space(50);
                var group = m_ToggleGroups[i];
                group.OnGUI();
            }
            GUILayout.EndHorizontal();
        }


    }
}



