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
    using System.Collections.Generic;
    using UnityEngine;

    public class MarkerOptionCollection : MarkerAnchor
    {
        [SerializeField]
        private List<MarkerOption> m_RelatedOptions = new List<MarkerOption>();
        [SerializeField]
        private Dictionary<int, MarkerOption> m_IndexOptionDict = new Dictionary<int, MarkerOption>();
        private int m_Index;

        private HashSet<int> m_SelectedIndex = new HashSet<int>();
        #region unity messages
        private void Awake()
        {
            m_RelatedOptions.AddRange(GetComponentsInChildren<MarkerOption>());
            foreach (var option in m_RelatedOptions)
            {
                m_IndexOptionDict.Add(option.Index, option);
            }
            gameObject.SetActive(false);
        }
        #endregion

        public bool IsRelatedTo(int index)
        {
            return m_IndexOptionDict.ContainsKey(index);
        }


        public void Select(int index)
        {
            if (m_IndexOptionDict.TryGetValue(m_Index, out MarkerOption option))
            {
                option.Deselect();
                m_SelectedIndex.Remove(m_Index);
            }

            if (m_IndexOptionDict.TryGetValue(index, out option))
            {
                if (!gameObject.activeSelf)
                {
                    gameObject.SetActive(true);
                }
                m_Index = index;
                m_SelectedIndex.Add(index);
                option.Select();
            }
        }

        public void Deselect(int index)
        {
            if (m_IndexOptionDict.TryGetValue(index, out MarkerOption option))
            {
                option.Deselect();
                m_SelectedIndex.Remove(index);
            }
            if (m_SelectedIndex.Count == 0)
            {
                if (gameObject.activeSelf)
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
