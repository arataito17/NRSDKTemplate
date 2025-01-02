/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/

using LitJson;
using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace NRKernal.Experimental
{
    public class NRCoastersTrackingImageDatabase : NRTrackingImageDatabase
    {
#if UNITY_EDITOR
        public override void BuildIfNeeded()
        {
            if (!m_IsRawDataDirty)
            {
                return;
            }
            m_IsRawDataDirty = false;

            string file_path = Path.GetFullPath("Assets/NRSDKExperimental/Demos/CoastersImageTracking/Database/e1c9507e-021f-4737-b381-452db44f5536.zip");

            if (!string.IsNullOrEmpty(file_path) && File.Exists(file_path))
            {
                // Read the zip bytes
                m_RawData = File.ReadAllBytes(file_path);

                EditorUtility.SetDirty(this);
                // Force a save to make certain build process will get updated asset.
                AssetDatabase.SaveAssets();
            }
            UpdateClipVersion();
        }
#endif
    }
}
