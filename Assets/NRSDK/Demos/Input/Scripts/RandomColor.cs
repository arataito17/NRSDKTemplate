using UnityEngine;
using UnityEngine.UI;

public class RandomColor: MonoBehaviour
{
    public GameObject targetObject;  // 要更改颜色的目标对象
    public Button changeColorButton; // 触发更改颜色的按钮

    void Start()
    {
        // 确保按钮绑定事件
        if (changeColorButton != null)
        {
            changeColorButton.onClick.AddListener(ChangeColor);
        }
    }

    void ChangeColor()
    {
        // 随机生成颜色
        Color newColor = new Color(Random.value, Random.value, Random.value);

        // 将颜色应用到目标对象的材质上
        if (targetObject != null)
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = newColor;
            }
        }
    }
}