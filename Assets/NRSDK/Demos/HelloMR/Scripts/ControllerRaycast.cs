using UnityEngine;

public class ControllerRaycast : MonoBehaviour
{
    // 控制器的 Transform（你需要在 Inspector 中设置或获取它）
    public Transform controllerTransform;
    
    // 射线检测的最大距离
    public float raycastDistance = 10f;

    // 用来显示射线的调试线
    public bool showRayDebug = true;

    void Update()
    {
        // 获取控制器的位置和朝向，构建射线
        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        RaycastHit hit;

        // 执行射线检测
        if (Physics.Raycast(ray, out hit, raycastDistance))
        {
            // 如果射线击中物体，输出信息
            Debug.Log("Hit object: " + hit.collider.gameObject.name);

            // 这里你可以对击中的物体执行其他操作，如改变颜色、播放音效等
            // 例如，可以将物体的颜色改变为红色：
            hit.collider.gameObject.GetComponent<Renderer>().material.color = Color.red;
        }

        // 如果需要调试射线，可在 Scene 视图中显示射线
        if (showRayDebug)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.green);
        }
    }
}