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
    using System.Collections.Generic;
    using UnityEngine;


#if UNITY_EDITOR
    using NRTrackableImage = MockTrackableImage;
#endif

    public class MarkerDemo: MonoBehaviour
    {
        [SerializeField]
        private MarkerImageExampleController m_Controller;

        [SerializeField]
        private List<MarkerOptionCollection> m_OptionCollectionList;

        private void Awake()
        {
            m_Controller.OnImageLoaded += OnImageLoaded;
            m_Controller.OnImageLost += OnImageLost;
        }


        private void OnImageLoaded(NRTrackableImage image)
        {
            int index = image.GetCoastersDataBaseIndex();
            foreach(var collect in m_OptionCollectionList)
            {
                if (collect.IsRelatedTo(index))
                {
                    collect.AttachTo(image);
                    collect.Select(index);
                    return;
                }
            }
        }
        private void OnImageLost(NRTrackableImage image)
        {
            int index = image.GetCoastersDataBaseIndex();
            foreach (var collect in m_OptionCollectionList)
            {
                if (collect.IsRelatedTo(index))
                {
                    collect.Deselect(index);
                    return;
                }
            }
        }
    }
}
