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
    public class MarkerMaterialOption : MarkerOption
    {
        public MaterialOptionEvent m_MaterialSelectEvent;
        public Material m_Material;
        [SerializeField]
        private Vector3 normalPos;
        [SerializeField]
        private Vector3 highlightPos;
        [SerializeField]
        private Transform m_ArrowTrans;
        [SerializeField]
        private Vector3  m_ArrowEulerAngle;
        public override void NotifySelectEvent()
        {
            m_MaterialSelectEvent.Invoke(m_Material);
        }
        public override void SetSelected(bool selected)
        {
            base.SetSelected(selected);
            if (selected)
            {
                transform.localPosition = highlightPos;
                m_ArrowTrans.localRotation = Quaternion.Euler(m_ArrowEulerAngle);
            }
            else
            {
                transform.localPosition = normalPos;
            }
        }
        [Serializable]
        public class MaterialOptionEvent : UnityEvent<Material> { }
    }
}
