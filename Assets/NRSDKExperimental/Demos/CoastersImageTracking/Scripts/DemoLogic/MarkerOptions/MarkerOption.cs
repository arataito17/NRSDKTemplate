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
    public abstract class MarkerOption: MonoBehaviour
    {

        const string TAG = "MarkerOption";
        #region events
        public abstract void NotifySelectEvent();
        #endregion

        #region settings
        [SerializeField]
        private int m_Index;
        #endregion

        public int Index => m_Index;

        public virtual void SetSelected(bool selected)
        {
        }

        public void Select()
        {
            NRDebugger.Info($"[{TAG}] Select index={m_Index}");
            SetSelected(true);
            NotifySelectEvent();
        }

        public void Deselect()
        {
            NRDebugger.Info($"[{TAG}] Deselect index={m_Index}");
            SetSelected(false);
        }

        #region unity messages

        private void Awake()
        {
            SetSelected(false);
        }
        #endregion

    }
}
