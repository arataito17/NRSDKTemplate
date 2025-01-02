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

    public class MarkerShapeOption: MarkerOption
    {
        public ShapeOptionEvent m_ShapeSelectEvent;
        public Mesh m_Mesh;
        [SerializeField]
        private GameObject m_Highlight;

        public override void SetSelected(bool selected)
        {
            if (m_Highlight != null)
            {
                m_Highlight.SetActive(selected);
            }
        }
        public override void NotifySelectEvent()
        {
            m_ShapeSelectEvent.Invoke(m_Mesh);
        }
        [Serializable]
        public class ShapeOptionEvent : UnityEvent<Mesh> { }
    }
}
