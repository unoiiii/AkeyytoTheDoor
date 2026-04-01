using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OHTools
{
    public class OHFixImageAlphaRaycastFilter : MonoBehaviour
    {
        // 当像素 alpha 小于此值时，不响应射线检测（建议设置为大于 0 的值）
        private float alphaThreshold = 0.1f;

        private Image image;

        void Awake()
        {
            image = GetComponent<Image>();
            // 设置 Image 的射线检测透明度下限
            image.alphaHitTestMinimumThreshold = alphaThreshold;
        }
    }
}