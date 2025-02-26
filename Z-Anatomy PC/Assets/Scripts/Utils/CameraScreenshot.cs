using UnityEngine;
using System.IO;

namespace ZAnatomy
{
    public class CameraScreenshot : MonoBehaviour
    {
        [SerializeField] private Camera captureCamera;
        [SerializeField] private int imageWidth = 1920;
        [SerializeField] private int imageHeight = 1080;

        public void CaptureScreenshot()
        {
            // Saving initial settings
            Color originalColor = captureCamera.backgroundColor;

            // Apply a transparent background temporarily
            captureCamera.backgroundColor = new Color(0, 0, 0, 0); 
            
            // Creating a RenderTexture with alpha support
            RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
            captureCamera.targetTexture = renderTexture;
            captureCamera.Render();

            // Texture creation and pixel recovery
            Texture2D screenshot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            
            // Ensure that the alpha channel is taken into account
            Color[] pixels = screenshot.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r == 0 && pixels[i].g == 0 && pixels[i].b == 0) // Even Black
                {
                    pixels[i].a = 0; // We force transparency
                }
            }
            screenshot.SetPixels(pixels);
            screenshot.Apply();

            // Save as PNG
            byte[] bytes = screenshot.EncodeToPNG();
            string filePath = Path.Combine(Application.persistentDataPath, "screenshot.png");
            File.WriteAllBytes(filePath, bytes);
            
            // Restoring original settings
            captureCamera.backgroundColor = originalColor;

            // Cleaning
            captureCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);
            Destroy(screenshot);

            Debug.Log($"Capture saved : {filePath}");
        }
    }
}
