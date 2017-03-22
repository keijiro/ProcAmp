using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Rendering;

namespace Klak.Video
{
    [ExecuteInEditMode]
    public class ProcAmp : MonoBehaviour
    {
        #region Video source properties

        [SerializeField] VideoPlayer _sourceVideo;

        public VideoPlayer sourceVideo {
            get { return _sourceVideo; }
            set { _sourceVideo = value; }
        }

        [SerializeField] Texture _sourceTexture;

        public Texture sourceTexture {
            get { return _sourceTexture; }
            set { _sourceTexture = value; }
        }

        #endregion

        #region Basic adjustment properties

        [SerializeField, Range(-1, 1)] float _brightness = 0;

        public float brightness {
            get { return _brightness; }
            set { _brightness = value; }
        }

        [SerializeField, Range(-1, 2)] float _contrast = 1;

        public float contrast {
            get { return _contrast; }
            set { _contrast = value; }
        }

        [SerializeField, Range(0, 2)] float _saturation = 1;

        public float saturation {
            get { return _saturation; }
            set { _saturation = value; }
        }

        #endregion

        #region Color balance properties

        [SerializeField, Range(-1, 1)] float _temperature = 0;

        public float temperature {
            get { return _temperature; }
            set { _temperature = value; }
        }


        [SerializeField, Range(-1, 1)] float _tint = 0;

        public float tint {
            get { return _tint; }
            set { _tint = value; }
        }

        #endregion

        #region Keying properties

        [SerializeField] bool _keying;

        public bool keying {
            get { return _keying; }
            set { _keying = value; }
        }

        [SerializeField, ColorUsage(false)] Color _keyColor = Color.green;

        public Color keyColor {
            get { return _keyColor; }
            set { _keyColor = value; }
        }

        [SerializeField, Range(0, 1)] float _keyThreshold = 0.5f;

        public float keyThreshold {
            get { return _keyThreshold; }
            set { _keyThreshold = value; }
        }

        [SerializeField, Range(0, 1)] float _keyTolerance = 0.2f;

        public float keyTolerance {
            get { return _keyTolerance; }
            set { _keyTolerance = value; }
        }

        [SerializeField, Range(0, 1)] float _spillRemoval = 0.5f;

        public float spillRemoval {
            get { return _spillRemoval; }
            set { _spillRemoval = value; }
        }

        #endregion

        #region Transform properties

        [SerializeField] Vector4 _trim = Vector4.zero;

        public Vector4 trim {
            get { return _trim; }
            set { _trim = value; }
        }

        [SerializeField] Vector2 _scale = Vector2.one;

        public Vector2 scale {
            get { return _scale; }
            set { _scale = value; }
        }

        [SerializeField] Vector2 _offset = Vector2.zero;

        public Vector2 offset {
            get { return _offset; }
            set { _offset = value; }
        }

        #endregion

        #region Final tweak properties

        [SerializeField] Color _fadeToColor = Color.clear;

        public Color fadeToColor {
            get { return _fadeToColor; }
            set { _fadeToColor = value; }
        }

        [SerializeField, Range(0, 1)] float _opacity = 1;

        public float opacity {
            get { return _opacity; }
            set { _opacity = value; }
        }

        #endregion

        #region Destination properties

        [SerializeField] RenderTexture _targetTexture;

        public RenderTexture targetTexture {
            get { return _targetTexture; }
            set { _targetTexture = value; }
        }

        [SerializeField] RawImage _targetImage;

        public RawImage targetImage {
            get { return _targetImage; }
            set { _targetImage = value; }
        }

        [SerializeField] bool _blitToScreen = true;

        public bool blitToScreen {
            get { return _blitToScreen; }
            set { _blitToScreen = value; }
        }

        #endregion

        #region Public utility functions (shared with the editor code)

        // YCgCo color space conversion
        public static Vector3 RGB2YCgCo(Color rgb)
        {
            var y  =  0.25f * rgb.r + 0.5f * rgb.g + 0.25f * rgb.b;
            var cg = -0.25f * rgb.r + 0.5f * rgb.g - 0.25f * rgb.b;
            var co =  0.50f * rgb.r                - 0.50f * rgb.b;
            return new Vector3(y, cg, co);
        }

        // An analytical model of chromaticity of the standard illuminant, by Judd et al.
        // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
        // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
        public static float StandardIlluminantY(float x)
        {
            return 2.87f * x - 3.0f * x * x - 0.27509507f;
        }

        // CIE xy chromaticity to CAT02 LMS.
        // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
        public static Vector3 CIExyToLMS(float x, float y)
        {
            var Y = 1.0f;
            var X = Y * x / y;
            var Z = Y * (1.0f - x - y) / y;

            var L =  0.7328f * X + 0.4296f * Y - 0.1624f * Z;
            var M = -0.7036f * X + 1.6975f * Y + 0.0061f * Z;
            var S =  0.0030f * X + 0.0136f * Y + 0.9834f * Z;

            return new Vector3(L, M, S);
        }

        // Calculate the color balance coefficients.
        public static Vector3 CalculateColorBalance(float temp, float tint)
        {
            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            var x = 0.31271f - temp * (temp < 0.0f ? 0.1f : 0.05f);
            var y = StandardIlluminantY(x) + tint * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        #endregion

        #region Private members

        [SerializeField, HideInInspector] Shader _shader;
        [SerializeField, HideInInspector] Mesh _quadMesh;
        [SerializeField, HideInInspector] Texture _offlineTexture;

        Material _material;
        RenderTexture _buffer;

        bool isImageEffect {
            get { return GetComponent<Camera>() != null; }
        }

        Texture currentSource {
            get {
                if (Application.isPlaying)
                    if (_sourceVideo != null)
                        return _sourceVideo.texture;
                    else
                        return _sourceTexture;
                else
                    return _offlineTexture;
            }
        }

        void UpdateMaterialProperties()
        {
            // Input
            _material.SetTexture("_MainTex", currentSource);

            // Basic adjustment
            _material.SetFloat("_Brightness", _brightness);
            _material.SetFloat("_Contrast", _contrast);
            _material.SetFloat("_Saturation", _saturation);

            // Color balance
            var balance = CalculateColorBalance(_temperature, _tint);
            _material.SetVector("_ColorBalance", balance);

            // Keying
            if (_keying)
            {
                var ycgco = RGB2YCgCo(_keyColor);
                _material.SetVector("_KeyCgCo", new Vector2(ycgco.y, ycgco.z));
                _material.SetFloat("_KeyThreshold", _keyThreshold);
                _material.SetFloat("_KeyTolerance", _keyTolerance);
                _material.SetFloat("_SpillRemoval", _spillRemoval);
                _material.EnableKeyword("_KEYING");
            }
            else
            {
                _material.DisableKeyword("_KEYING");
            }

            // Transform
            _material.SetVector("_TrimParams", new Vector4(
                _trim.x, _trim.w,
                1 / (1 - _trim.x - _trim.z),
                1 / (1 - _trim.y - _trim.w)
            ));
            _material.SetVector("_Scale", _scale);
            _material.SetVector("_Offset", _offset);

            // Final tweaks
            _material.SetVector("_FadeToColor", _fadeToColor);
            _material.SetFloat("_Opacity", _opacity);

            // Blend mode
            if (_keying || _opacity < 1)
            {
                _material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                _material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            }
            else
            {
                _material.SetInt("_SrcBlend", (int)BlendMode.One);
                _material.SetInt("_DstBlend", (int)BlendMode.Zero);
            }
        }

        #endregion

        #region MonoBehaviour functions

        void Reset()
        {
            // Auto assign the VideoPlayer component if there is one.
            _sourceVideo = GetComponent<VideoPlayer>();
        }

        void OnDestroy()
        {
            if (_material != null)
                if (Application.isPlaying)
                    Destroy(_material);
                else
                    DestroyImmediate(_material);
        }

        void Update()
        {
            // Material instantiation.
            if (_material == null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.DontSave;
            }

            // Material update.
            UpdateMaterialProperties();

            // Release previous frames.
            if (_buffer != null) RenderTexture.ReleaseTemporary(_buffer);
            _buffer = null;

            // Do nothing here if image effect mode.
            if (isImageEffect) return;

            // Do nothing if no source is given.
            var source = currentSource;
            if (source == null) return;

            // Determine the destination.
            var dest = _targetTexture;
            if (dest == null)
            {
                if (_targetImage == null) return; // No target, do nothing.
                // Allocate an internal temporary buffer.
                _buffer = RenderTexture.GetTemporary(source.width, source.height);
                dest = _buffer;
            }

            // Invoke the ProcAmp shader.
            Graphics.Blit(source, dest, _material, 0);

            // Update the UI image target.
            if (_targetImage != null) _targetImage.texture = dest;
        }

        void OnRenderObject()
        {
            if (!_blitToScreen || isImageEffect || _material == null) return;

            // Use the simple blit pass when we already have a processed image.
            var processed = _buffer != null ? _buffer : _targetTexture;
            if (processed != null)
            {
                _material.SetTexture("_MainTex", processed);
                _material.SetPass(2);
            }
            else
            {
                // Blit with ProcAmp pass
                _material.SetTexture("_MainTex", currentSource);
                _material.SetPass(1);
            }

            Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var video = currentSource;
            if (video != null && _material != null)
            {
                // Coefficients for aspect ratio conversion.
                var screenAspect = (float)source.height / source.width;
                var textureAspect = (float)video.height / video.width;
                var aspectConv = screenAspect / textureAspect;
                if (aspectConv > 1)
                    _material.SetVector("_AspectConv", new Vector2(1, aspectConv));
                else
                    _material.SetVector("_AspectConv", new Vector2(1 / aspectConv, 1));

                // Composite with the source.
                _material.SetTexture("_BaseTex", source);
                Graphics.Blit(video, destination, _material, 3);
            }
            else
            {
                // Do nothing because the video source is not ready.
                Graphics.Blit(source, destination);
            }
        }

        #endregion
    }
}
