/****************************************************************************
* Copyright 2019 Xreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.xreal.com/        
* 
*****************************************************************************/
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace NRKernal.Experimental.Editor
{
    [CustomEditor(typeof(NRCoastersTrackingImageDatabase))]
    public class NRCoastersTrackingImageDatabaseInspector: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NRCoastersTrackingImageDatabase database = target as NRCoastersTrackingImageDatabase;
            if(database == null)
            {
                return;
            }
            if (GUILayout.Button("rebuild", GUILayout.Width(100)))
            {
                database.BuildIfNeeded();
            }
            if (GUILayout.Button("deploy", GUILayout.Width(100)))
            {
                string deploy_path = database.TrackingImageDataOutPutPath;
                NRDebugger.Info("[TrackingImageDatabase] DeployData to path :" + deploy_path);
                ZipUtility.UnzipFile(database.RawData, deploy_path, NativeConstants.ZipKey);
            }
        }
    }
}
#endif
