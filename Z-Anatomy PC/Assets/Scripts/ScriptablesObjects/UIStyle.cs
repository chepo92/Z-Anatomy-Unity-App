using UnityEngine;

namespace ZAnatomy
{
    [CreateAssetMenu(fileName = "NewUIStyle", menuName = "Z-Anatomy/UIStyle")]
    public class UIStyle : ScriptableObject
    {
        [Header("UI Style")]
        public string UIStyleName;
        [Header("UI Specific Colors")]
        public Color HighlightColor = Color.white;
        public Color SecondaryColor = Color.gray;
        public Color SurfaceColor = Color.white;
        public Color OnSurfaceColor = Color.black;
        public Color BackgroundColor = Color.black;
        public Color IconColor = Color.white;
        public Color DisabledIconColor = Color.gray;
        public Color TaskBarColor = Color.blue;
        [Header("UI Specific Sizes")]
        public float labelFontSize = 1.75f;
        public float titleLabelFontSize = 3.5f;
        public float lineSize = 1.5f;
    }
}
