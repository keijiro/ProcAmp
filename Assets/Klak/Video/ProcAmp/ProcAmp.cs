using UnityEngine;
using UnityEngine.Video;

namespace Klak.Video
{
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

        Material _material;
        RenderTexture _buffer;

        #endregion

        #region MonoBehaviour functions

        void Start()
        {
            _material = new Material(_shader);
        }

        void OnDestroy()
        {
            if (_buffer != null) RenderTexture.ReleaseTemporary(_buffer);
        }

        void Update()
        {
            if (_buffer != null) RenderTexture.ReleaseTemporary(_buffer);
            _buffer = null;

            var source = _sourceVideo != null ? _sourceVideo.texture : _sourceTexture;
            if (source == null) return;

            _buffer = RenderTexture.GetTemporary(source.width, source.height);

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

            // Final tweaks
            _material.SetVector("_FadeToColor", _fadeToColor);
            _material.SetFloat("_Opacity", _opacity);

            Graphics.Blit(source, _buffer, _material, 0);
        }

        void OnRenderObject()
        {
            _material.SetTexture("_MainTex", _buffer);
            _material.SetPass(_keying || _opacity < 1 ? 2 : 1);
            Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
        }

        #endregion
    }
}
