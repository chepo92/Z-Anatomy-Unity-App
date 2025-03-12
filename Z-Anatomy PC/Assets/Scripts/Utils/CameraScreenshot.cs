using System;
using System.Collections;
using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;

// For a better rendering of transparency, the VolumeProfile effects must be disabled from the URP setting (RenderPipelineAsset),
// as well as increase the Grading Mode of the post-processing to High Dynamic Range before building the application
// otherwise it risks making a grayed-out transparency. We can also disable these settings:
// by checking the box 'automaticallyDisableVolumeEffects' or where to do it manually.
// Developed by Emmanuel Garraud / unity.dev@fantaziorka.com

namespace ZAnatomy
{
     public class CameraScreenshot : MonoBehaviour
    {
        [Tooltip("Camera to capture")] public Camera targetCamera;

        [Tooltip("Key from keyboard to make screen capture")]
        public KeyCode shortcutCapture = KeyCode.F12;
        
        [Header("UISettings")] [Tooltip("UI implementation for use window")]
        public CanvasGroup cg_WindowScreenshot;
        
        [Tooltip("UI toggle for antialiasing setting")]
        public Toggle tg_AntiAliasing;
        
        [Tooltip("UI dropdown for resolution factor")]
        public TMP_Dropdown dd_Factor;

        [Tooltip("UI text field for resolution")]
        public TMP_Text txt_Width;
        public TMP_Text txt_Height;
        
        [Header("Image Settings")] [Tooltip("Capture width resolution")]
        public int captureWidth = 1920;

        [Tooltip("Capture height resolution")] 
        public int captureHeight = 1080;

        [Tooltip("Anti-aliasing level")] [Range(0, 8)]
        public int antiAliasing = 8;

        [Tooltip("Export image with transparency")]
        public bool exportWithTransparency = true;

        [Tooltip("Automatically disable volume effects for better rendering of transparency")]
        public bool disableVolumeEffects = false;

        public VolumeProfile volumeProfile;
        private ProfileVolume currentVolumeProfile;
        
        [Header("Export Settings")] [Tooltip("Default file name prefix")]
        public string defaultFileName = "screenshot";

        [Tooltip("Prompt for file name and location")]
        public bool askForFilePath = true;

        [Tooltip("Custom save path")] public string customSavePath = "";

        private void Awake()
        {
            // Use the camera attached to this script if no camera is specified
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();

            // Ensure anti-aliasing value is valid (0, 2, 4 or 8)
            antiAliasing = ValidateAntiAliasingLevel(antiAliasing);

            CloseWindowScreenshot();
        }

        private void Start()
        {
            if (disableVolumeEffects)
            {
                StartCoroutine(StoreInitialVolumeProfileState());
            }

            OnChangeFactor();
        }

        private void OnValidate()
        {
            // Validate the anti-aliasing level in the editor
            antiAliasing = ValidateAntiAliasingLevel(antiAliasing);
        }

        private int ValidateAntiAliasingLevel(int level)
        {
            // Anti-aliasing in Unity must be 0, 2, 4 or 8
            if (level <= 0) return 0;
            else if (level <= 2) return 2;
            else if (level <= 4) return 4;
            else return 8;
        }

        [ContextMenu("Capture Screenshot")]
        public void CaptureScreenshot()
        {
            // Check if camera is assigned
            if (targetCamera == null)
            {
                Debug.LogError("No camera assigned for screenshot!");
                return;
            }

            StartCoroutine(PerformAction());
        }
        
        private IEnumerator PerformAction()
        {
            // Validate anti-aliasing level
            int validAntiAliasing = ValidateAntiAliasingLevel(antiAliasing);

            // Create render textures with anti-aliasing
            RenderTexture rtBlack = new RenderTexture(captureWidth, captureHeight, 24);
            rtBlack.antiAliasing = validAntiAliasing;
            rtBlack.format = RenderTextureFormat.ARGB32;
            rtBlack.filterMode = FilterMode.Trilinear;

            RenderTexture rtWhite = new RenderTexture(captureWidth, captureHeight, 24);
            rtWhite.antiAliasing = validAntiAliasing;
            rtWhite.format = RenderTextureFormat.ARGB32;
            rtWhite.filterMode = FilterMode.Trilinear;

            // Create textures to receive results
            Texture2D screenshotBlack = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);
            Texture2D screenshotWhite = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false);

            Texture2D screenshotResult = new Texture2D(captureWidth, captureHeight, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Point, // Avoid blurring
                wrapMode = TextureWrapMode.Clamp
            };

            // Save original camera settings
            CameraClearFlags originalClearFlags = targetCamera.clearFlags;
            Color originalBackgroundColor = targetCamera.backgroundColor;
            RenderTexture originalRenderTexture = targetCamera.targetTexture;

            try
            {
                if (exportWithTransparency)
                {
                    // Disable Volume Effects
                    if (disableVolumeEffects)
                    {
                        yield return StartCoroutine(DisableAllVolumeProfileParameters());
                        yield return new WaitForSeconds(0.1f);
                    }
                    // Capture with black background
                    targetCamera.clearFlags = CameraClearFlags.SolidColor;
                    targetCamera.backgroundColor = Color.black;
                    targetCamera.targetTexture = rtBlack;
                    targetCamera.Render();
                    RenderTexture.active = rtBlack;
                    screenshotBlack.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                    screenshotBlack.Apply();

                    // Capture with white background
                    targetCamera.backgroundColor = Color.white;
                    targetCamera.targetTexture = rtWhite;
                    targetCamera.Render();
                    RenderTexture.active = rtWhite;
                    screenshotWhite.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                    screenshotWhite.Apply();

                    // Compare pixels and create transparent image
                    Color32[] blackPixels = screenshotBlack.GetPixels32();
                    Color32[] whitePixels = screenshotWhite.GetPixels32();
                    Color32[] transparentPixels = new Color32[captureWidth * captureHeight];

                    for (int i = 0; i < blackPixels.Length; i++)
                    {
                        Color32 blackColor = blackPixels[i];
                        Color32 whiteColor = whitePixels[i];

                        bool isTransparent = IsTransparentBackground(blackColor, whiteColor);

                        // If background is transparent
                        if (isTransparent)
                        {
                            // Completely transparent pixel
                            transparentPixels[i] = new Color32(0, 0, 0, 0);
                        }
                        else
                        {
                            if (IsSameColors(blackColor, whiteColor))
                            {
                                transparentPixels[i] = blackColor;
                            }
                            else
                            {
                                {
                                    transparentPixels[i] = CalculateRealColor(blackColor, whiteColor);
                                }
                            }
                        }
                    }

                    // Apply pixels to texture
                    screenshotResult.SetPixels32(transparentPixels);
                    screenshotResult.Apply();
                }
                else
                {
                    // Capture with original camera background
                    targetCamera.targetTexture = RenderTexture.GetTemporary(captureWidth, captureHeight, 24);
                    targetCamera.Render();
                    RenderTexture.active = targetCamera.targetTexture;
                    screenshotResult.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
                    screenshotResult.Apply();
                    RenderTexture.ReleaseTemporary(targetCamera.targetTexture);
                }

                // Encode to PNG
                byte[] pngBytes = screenshotResult.EncodeToPNG();

                // Determine save path
                string filePath = DetermineFilePath(exportWithTransparency);

                if (string.IsNullOrEmpty(filePath))
                {
                    Debug.Log("Screenshot capture cancelled by user");
                    yield return null;
                }
                
                // Restore original Volume Effects
                if (disableVolumeEffects)
                {
                    yield return StartCoroutine(RestoreInitialVolumeProfileState());
                    yield return new WaitForSeconds(0.1f);
                }
                // Save image
                File.WriteAllBytes(filePath, pngBytes);

                Debug.Log($"Screenshot saved: {filePath}");
                Debug.Log(
                    $"Settings used - Resolution: {captureWidth}x{captureHeight}, Anti-aliasing: {validAntiAliasing}, Transparency: {exportWithTransparency}");

#if UNITY_EDITOR
                // Refresh asset database in editor
                if (filePath.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
                    AssetDatabase.ImportAsset(relativePath);
                }
#endif
            }
            finally
            {
                // Restore original camera settings
                targetCamera.targetTexture = originalRenderTexture;
                targetCamera.clearFlags = originalClearFlags;
                targetCamera.backgroundColor = originalBackgroundColor;
                RenderTexture.active = null;
            }
            yield return new WaitForEndOfFrame();
        }

        private bool IsTransparentBackground(Color32 blackColor, Color32 whiteColor)
        {
            if ((blackColor.r == 0) && (blackColor.g == 0) && (blackColor.b == 0))
            {
                if ((whiteColor.r == 255) && (whiteColor.g == 255) && (whiteColor.b == 255))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool IsSameColors(Color32 a, Color32 b)
        {
            return ((a.r == b.r) && (a.g == b.g) && (a.b == b.b) && (a.a == b.a));
        }

        private Color32 CalculateRealColor(Color32 colorOnBlack, Color32 colorOnWhite)
        {

            const float epsilon = 0.001f;
            // Calculate the alpha averaged over the three channels (r, g, b)
            float alphaR = Mathf.Clamp(1f - ((colorOnWhite.r - colorOnBlack.r) / 255f), epsilon, 1f);
            float alphaG = Mathf.Clamp(1f - ((colorOnWhite.g - colorOnBlack.g) / 255f), epsilon, 1f);
            float alphaB = Mathf.Clamp(1f - ((colorOnWhite.b - colorOnBlack.b) / 255f), epsilon, 1f);

            float alpha = Mathf.Clamp((alphaR + alphaG + alphaB) / 3f, 0f, 1f); // Average of the three values

            if (alpha <= 0)
                return new Color32(0, 0, 0, 0); // Fully transparent pixel

            // Calculate true color
            byte realR = (byte)Mathf.Clamp(colorOnBlack.r / alphaR, 0, 255);
            byte realG = (byte)Mathf.Clamp(colorOnBlack.g / alphaG, 0, 255);
            byte realB = (byte)Mathf.Clamp(colorOnBlack.b / alphaB, 0, 255);
            byte realA = (byte)Mathf.Clamp(alpha * 255, 0, 255);

            return new Color32(realR, realG, realB, realA);
        } 
        
        // Method to store the initial state of the VolumeProfile
        private IEnumerator StoreInitialVolumeProfileState()
        {
            // Check if Volume Profile is assigned
            if (volumeProfile == null)
            {
                Debug.LogWarning("Volume Profile is not assigned!");
                yield return null;
            }
            
            // Create a deep copy of the VolumeProfile
            currentVolumeProfile = new ProfileVolume();

            // Retrieve the current values of your Volume Profile components
            currentVolumeProfile.colorAdjustements = GetColorAdjustementsState();
            currentVolumeProfile.toneMapping = GetToneMappingState();
            currentVolumeProfile.bloom = GetBloomState();
            currentVolumeProfile.motionBlur = GetMotionBlurState();
            currentVolumeProfile.vignette = GetVignetteState();
            
            Debug.Log("currentVolumeProfile = "+currentVolumeProfile);
            Debug.Log("currentVolumeProfile.colorAdjustements  = "+currentVolumeProfile.colorAdjustements );
            Debug.Log("currentVolumeProfile.toneMapping  = "+currentVolumeProfile.toneMapping );
            Debug.Log("currentVolumeProfile.bloom  = "+currentVolumeProfile.bloom );
            Debug.Log("currentVolumeProfile.motionBlur  = "+currentVolumeProfile.motionBlur );
            Debug.Log("currentVolumeProfile.vignette  = "+currentVolumeProfile.vignette );
            yield return null;
        }

        // Method to disable all VolumeProfile settings
        private IEnumerator DisableAllVolumeProfileParameters()
        {
            // Check if Volume Profile is assigned
            if (volumeProfile == null)
            {
                Debug.LogWarning("Volume Profile is not assigned!");
                yield return null;
            }

            SetColorAdjustementsState(false);
            SetToneMappingState(false);
            SetBloomState(false);
            SetMotionBlurState(false);
            SetVignetteState(false);
            targetCamera.Render();
            Debug.LogWarning("Volume Profile is disable updated!");
            yield return new WaitForSeconds(0.1f);
        }

        // Method to reactivate the initial VolumeProfile settings
        private IEnumerator RestoreInitialVolumeProfileState()
        {
            // Check if Volume Profile is assigned
            if (volumeProfile == null)
            {
                Debug.LogWarning("Volume Profile is not assigned!");
                yield return null;
            }
            
            SetColorAdjustementsState(currentVolumeProfile.colorAdjustements);
            SetToneMappingState(currentVolumeProfile.toneMapping);
            SetBloomState(currentVolumeProfile.bloom);
            SetMotionBlurState(currentVolumeProfile.motionBlur);
            SetVignetteState(currentVolumeProfile.vignette);
            targetCamera.Render();
            Debug.LogWarning("Volume Profile is restore updated!");
            yield return new WaitForSeconds(0.1f);
        }
        
        // Retrieve the current state of effects in the Volume Profile
        private bool GetColorAdjustementsState()
        {
            // Try to get the ColorAdjustments component from the Volume Profile
            if (volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                // Return active state of the ColorAdjustments component
                return colorAdjustments.active;
            }
            else
            {
                Debug.LogWarning("ColorAdjustments component not found in the Volume Profile!");
                return false;
            }  
        }
        private bool GetToneMappingState()
        {
            // Try to get the Tonemapping component from the Volume Profile
            if (volumeProfile.TryGet<Tonemapping>(out var tonemapping))
            {
                // Return active state of the Tonemapping component
                return tonemapping.active;
            }
            else
            {
                Debug.LogWarning("Tonemapping component not found in the Volume Profile!");
                return false;
            }  
        }

        private bool GetBloomState()
        {
            // Try to get the Bloom component from the Volume Profile
            if (volumeProfile.TryGet<Bloom>(out var bloom))
            {
                // Return active state of the Bloom component
                return bloom.active;
            }
            else
            {
                Debug.LogWarning("Bloom component not found in the Volume Profile!");
                return false;
            }  
        }

        private bool GetMotionBlurState()
        {
            // Try to get the MotionBlur component from the Volume Profile
            if (volumeProfile.TryGet<MotionBlur>(out var motionBlur))
            {
                // Return active state of the MotionBlur component
                return motionBlur.active;
            }
            else
            {
                Debug.LogWarning("MotionBlur component not found in the Volume Profile!");
                return false;
            }  
        }

        private bool GetVignetteState()
        {
            // Try to get the Vignette component from the Volume Profile
            if (volumeProfile.TryGet<Vignette>(out var vignette))
            {
                // Return active state of the Vignette component
                return vignette.active;
            }
            else
            {
                Debug.LogWarning("Vignette component not found in the Volume Profile!");
                return false;
            }  
        }

        // Change the state of effects in the Volume Profile
        public void SetColorAdjustementsState(bool isEnabled)
        {
            // Try to get the ColorAdjustments component from the Volume Profile
            if (volumeProfile.TryGet<ColorAdjustments>(out var colorAdjustments))
            {
                // Set the override and active state of the ColorAdjustments component
                colorAdjustments.active = isEnabled;
            }
            else
            {
                Debug.LogWarning("ColorAdjustments component not found in the Volume Profile!");
            }  
        }
        public void SetToneMappingState(bool isEnabled)
        {
            // Try to get the Tonemapping component from the Volume Profile
            if (volumeProfile.TryGet<Tonemapping>(out var tonemapping))
            {
                // Set the override and active state of the Tonemapping component
                tonemapping.active = isEnabled;
            }
            else
            {
                Debug.LogWarning("Tonemapping component not found in the Volume Profile!");
            }  
        }

        private void SetBloomState(bool isEnabled)
        {
            // Try to get the Bloom component from the Volume Profile
            if (volumeProfile.TryGet<Bloom>(out var bloom))
            {
                // Set the override and active state of the Bloom component
                bloom.active = isEnabled;
            }
            else
            {
                Debug.LogWarning("Bloom component not found in the Volume Profile!");
            }  
        }

        private void SetMotionBlurState(bool isEnabled)
        {
            // Try to get the MotionBlur component from the Volume Profile
            if (volumeProfile.TryGet<MotionBlur>(out var motionBlur))
            {
                // Set the override and active state of the MotionBlur component
                motionBlur.active = isEnabled;
            }
            else
            {
                Debug.LogWarning("MotionBlur component not found in the Volume Profile!");
            }  
        }

        private void SetVignetteState(bool isEnabled)
        {
            // Try to get the Vignette component from the Volume Profile
            if (volumeProfile.TryGet<Vignette>(out var vignette))
            {
                // Set the override and active state of the Vignette component
                vignette.active = isEnabled;
            }
            else
            {
                Debug.LogWarning("Vignette component not found in the Volume Profile!");
            }  
        }

        // Method to determine save path
        private string DetermineFilePath(bool isTransparent)
        {
            // Generate default filename
            string defaultName =
                $"{defaultFileName}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}{(isTransparent ? "_transparent" : "")}.png";

            // If user should choose file location
            if (askForFilePath)
            {
#if UNITY_EDITOR
                // In Unity editor
                return EditorUtility.SaveFilePanel(
                    "Save Screenshot",
                    "",
                    defaultName,
                    "png"
                );
#else
            // In standalone build
            if (!string.IsNullOrEmpty(customSavePath))
            {
                // Use custom path if defined
                Directory.CreateDirectory(customSavePath);
                return Path.Combine(customSavePath, defaultName);
            }
            else
            {
                // Use default path
                string defaultPath = Path.Combine(Application.persistentDataPath, "Screenshots");
                Directory.CreateDirectory(defaultPath);
                return Path.Combine(defaultPath, defaultName);
            }
#endif
            }
            else
            {
                // Default path without dialog
                string defaultPath = Path.Combine(Application.dataPath, "Screenshots");
                Directory.CreateDirectory(defaultPath);
                return Path.Combine(defaultPath, defaultName);
            }
        }

        // Allow screenshot capture via keyboard
        /*private void Update()
        {
            // Capture with defined key
            if (Input.GetKeyDown(shortcutCapture))
            {
                CaptureScreenshot();
            }
        }*/
        
        // UI manager
        public void OpenWindowScreenshot()
        {
            cg_WindowScreenshot.alpha = 1f;
            cg_WindowScreenshot.interactable = true;
            cg_WindowScreenshot.blocksRaycasts = true;
        }
        public void CloseWindowScreenshot()
        {
            cg_WindowScreenshot.alpha = 0f;
            cg_WindowScreenshot.interactable = false;
            cg_WindowScreenshot.blocksRaycasts = false;
        }

        public void ExportScreenshot()
        {
            CloseWindowScreenshot();
            CaptureScreenshot();
        }
        public void OnChangeFactor()
        {
            switch (dd_Factor.value)
            {
                case 0:
                    ChangeResolution(0.5f);
                    break;
                case 1:
                    ChangeResolution(1f);
                    break;
                case 2:
                    ChangeResolution(1.5f);
                    break;
                case 3:
                    ChangeResolution(2f);
                    break;
                case 4:
                    ChangeResolution(3f);
                    break;
                case 5:
                    ChangeResolution(4f);
                    break;
                default:
                    ChangeResolution(1f);
                    break;
            }
        }

        private void ChangeResolution(float factor)
        {
            captureWidth = (int)(1920 * factor);
            captureHeight = (int)(1080 * factor);
            txt_Width.text = captureWidth.ToString();
            txt_Height.text = captureHeight.ToString();
        }

        public void ConfigureScreenshot()
        {
            if (tg_AntiAliasing.isOn)
            {
                antiAliasing = 8;
            }
            else
            {
                antiAliasing = 0;
            }
        }
        
    }
    // Class defined for Volume Profile Data
    [Serializable]
    public class ProfileVolume
    {
        public bool colorAdjustements;
        public bool toneMapping;
        public bool bloom;
        public bool motionBlur;
        public bool vignette;
    }
}
