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

        #region Basic adjustment properties

        [SerializeField, Range(-1, 1)] float _brightness = 0;

        public float brightness {
            get { return _brightness; }
            set { _brightness = value; }
        }

        [SerializeField, Range(-1, 1)] float _contrast = 0;

        public float contrast {
            get { return _contrast; }
            set { _contrast = value; }
        }

        [SerializeField, Range(-1, 1)] float _saturation = 0;

        public float saturation {
            get { return _saturation; }
            set { _saturation = value; }
        }

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

        [SerializeField] Color _fadeToColor = Color.clear;

        public Color fadeToColor {
            get { return _fadeToColor; }
            set { _fadeToColor = value; }
        }

        #endregion

        #region Private members

        [SerializeField, HideInInspector] Shader _shader;
        [SerializeField, HideInInspector] Mesh _quadMesh;

        Material _material;
        RenderTexture _buffer;

        // An analytical model of chromaticity of the standard illuminant, by Judd et al.
        // http://en.wikipedia.org/wiki/Standard_illuminant#Illuminant_series_D
        // Slightly modifed to adjust it with the D65 white point (x=0.31271, y=0.32902).
        static float StandardIlluminantY(float x)
        {
            return 2.87f * x - 3.0f * x * x - 0.27509507f;
        }

        // CIE xy chromaticity to CAT02 LMS.
        // http://en.wikipedia.org/wiki/LMS_color_space#CAT02
        static Vector3 CIExyToLMS(float x, float y)
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
        Vector3 CalculateColorBalance()
        {
            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            var x = 0.31271f - _temperature * (_temperature < 0.0f ? 0.1f : 0.05f);
            var y = StandardIlluminantY(x) + _tint * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        // YCgCo color space conversion
        Vector3 RGB2YCgCo(Color rgb)
        {
            var y  =  0.25f * rgb.r + 0.5f * rgb.g + 0.25f * rgb.b;
            var cg = -0.25f * rgb.r + 0.5f * rgb.g - 0.25f * rgb.b;
            var co =  0.50f * rgb.r                - 0.50f * rgb.b;
            return new Vector3(y, cg, co);
        }

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
            var temp = null as RenderTexture;

            if (_keying)
            {
                temp = RenderTexture.GetTemporary(source.width, source.height);

                // Keying
                var ycgco = RGB2YCgCo(_keyColor);
                _material.SetVector("_CgCo", new Vector2(ycgco.y, ycgco.z));
                _material.SetVector("_Matte", new Vector2(
                    _keyThreshold * 0.1f, (_keyThreshold + _keyTolerance) * 0.1f
                ));
                _material.SetFloat("_Spill", _spillRemoval);
                Graphics.Blit(source, _buffer, _material, 2);
                Graphics.Blit(_buffer, temp);

/*
                // Alpha dilate
                Graphics.Blit(_buffer, temp, _material, 3);

                // Alpha blur (horizontal)
                _material.SetVector("_BlurDir", Vector3.right);
                Graphics.Blit(temp, _buffer, _material, 4);

                // Alpha blur (vertical)
                _material.SetVector("_BlurDir", Vector3.up);
                Graphics.Blit(_buffer, temp, _material, 4);
                */
            }

            // Adjustment
            _material.SetVector("_AmpParams", new Vector3(
                _brightness / 2, _contrast + 1, _saturation + 1
            ));

            _material.SetVector("_ColorBalance", CalculateColorBalance());
            _material.SetVector("_FadeToColor", _fadeToColor);

            var pass = _temperature == 0 && _tint == 0 ? 0 : 1;
            Graphics.Blit(temp != null ? temp : source, _buffer, _material, pass);

            if (temp != null) RenderTexture.ReleaseTemporary(temp);
        }

        void OnRenderObject()
        {
            _material.SetTexture("_MainTex", _buffer);
            _material.SetPass(5);
            Graphics.DrawMeshNow(_quadMesh, Matrix4x4.identity);
        }

        #endregion
    }
}
