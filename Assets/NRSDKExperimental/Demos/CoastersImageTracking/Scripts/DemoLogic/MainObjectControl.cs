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
    using UnityEngine;
    using static UnityEngine.GraphicsBuffer;

    public class MainObjectControl: MonoBehaviour
    {
        const string TAG = "MainObjectControl";
        [SerializeField]
        private Renderer m_Renderer;
        [SerializeField]
        private MeshFilter m_MeshFilter;
        public void GoToTarget(Transform target)
        {
            NRDebugger.Info($"[{TAG}] GoToTarget {target.position} {target.rotation.eulerAngles}");
            transform.position = target.position;
            transform.rotation = target.rotation;
            transform.localScale = target.localScale;
        }

        public void SetMaterial(Material material)
        {
            m_Renderer.material = material;
        }

        public void SetShape(Mesh mesh)
        {
            NRDebugger.Info($"[{TAG}] SetShape {mesh.name}");
            m_MeshFilter.mesh = mesh;
        }
    }
}
