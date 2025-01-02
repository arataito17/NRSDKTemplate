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
    using UnityEngine;
    using UnityEngine.Events;

    public class MarkerTransformOption: MarkerOption
    {
        public TransformOptionEvent m_TransformSelectEvent;
        public Transform m_Transform;

        public MeshFilter m_PlaneMeshFilter;
        public Mesh m_PlaneMesh;
        public override void NotifySelectEvent()
        {
            m_TransformSelectEvent.Invoke(m_Transform);
        }
        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            if (selected)
            {
                m_PlaneMeshFilter.mesh = m_PlaneMesh;
            }
        }

        [Serializable]
        public class TransformOptionEvent : UnityEvent<Transform> { }
    }
}
