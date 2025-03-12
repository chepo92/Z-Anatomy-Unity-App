using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
// Put on the rectTransform from the windows you want to move
// Developed by Emmanuel Garraud / unity.dev@fantaziorka.com
namespace ZAnatomy
{
    public class DragWindowFZ : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] private RectTransform dragRectTransform;    // The window you want to move
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private bool isBlendingWindow = false;
        [SerializeField] private bool isMultipleWindows = false;
        private Color backgroundColor;

        private void Awake()
        {
            backgroundColor = backgroundImage.color;
            if (dragRectTransform == null)
            {
                dragRectTransform = transform.parent.GetComponent<RectTransform>();
            }
            if (canvas == null)
            {
                Transform testCanvasTransform = transform.parent;
                while (testCanvasTransform != null)
                {
                    canvas = testCanvasTransform.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        break;
                    }
                    testCanvasTransform = testCanvasTransform.parent;
                }
            }
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            backgroundColor.a = .4f;
            if (isBlendingWindow)
            backgroundImage.color = backgroundColor;
        }
        public void OnDrag(PointerEventData eventData)
        {
            dragRectTransform.anchoredPosition += eventData.delta /canvas.scaleFactor;
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            backgroundColor.a = 1f;
            if (isBlendingWindow)
            backgroundImage.color = backgroundColor;
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isMultipleWindows)
            dragRectTransform.SetAsLastSibling();
        }
    }
}
