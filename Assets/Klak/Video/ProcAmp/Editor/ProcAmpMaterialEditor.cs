using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Klak.Video
{
    class ProcAmpMaterialEditor : ShaderGUI
    {
        #region Private variables

        // Serialized properties
        MaterialProperty _mainTex;
        MaterialProperty _brightness;
        MaterialProperty _contrast;
        MaterialProperty _saturation;
        MaterialProperty _temperature;
        MaterialProperty _tint;
        MaterialProperty _keying;
        MaterialProperty _keyColor;
        MaterialProperty _keyThreshold;
        MaterialProperty _keyTolerance;
        MaterialProperty _spillRemoval;
        MaterialProperty _trim;
        MaterialProperty _scale;
        MaterialProperty _offset;
        MaterialProperty _fadeToColor;
        MaterialProperty _opacity;

        // Labels used in the trim property
        static GUIContent[] _textsLTRB = {
            new GUIContent("L"), new GUIContent("T"),
            new GUIContent("R"), new GUIContent("B")
        };

        #endregion

        #region Overridden methods

        // Inspector GUI function
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            // These references should be updated every time :(
            _mainTex = FindProperty("_MainTex", props);
            _brightness = FindProperty("_Brightness", props);
            _contrast = FindProperty("_Contrast", props);
            _saturation = FindProperty("_Saturation", props);
            _temperature = FindProperty("_Temperature", props);
            _tint = FindProperty("_Tint", props);
            _keying = FindProperty("_Keying", props);
            _keyColor = FindProperty("_KeyColor", props);
            _keyThreshold = FindProperty("_KeyThreshold", props);
            _keyTolerance = FindProperty("_KeyTolerance", props);
            _spillRemoval = FindProperty("_SpillRemoval", props);
            _trim = FindProperty("_Trim", props);
            _scale = FindProperty("_Scale", props);
            _offset = FindProperty("_Offset", props);
            _fadeToColor = FindProperty("_FadeToColor", props);
            _opacity = FindProperty("_Opacity", props);

            if (ShaderPropertiesGUI(editor.target as Material, editor))
                foreach (var m in _mainTex.targets)
                    UpdateHiddenProperties((Material)m);
        }

        #endregion

        #region Private methods

        // GUI controls for properties
        // Returns true if the hidden properties should be updated.
        bool ShaderPropertiesGUI(Material material, MaterialEditor editor)
        {
            bool dirty = false;

            editor.TexturePropertySingleLine(new GUIContent("Source"), _mainTex);

            EditorGUILayout.Space();

            editor.ShaderProperty(_brightness, "Brightness");
            editor.ShaderProperty(_contrast, "Contrast");
            editor.ShaderProperty(_saturation, "Saturation");

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_temperature, "Temperature");
            editor.ShaderProperty(_tint, "Tint (cyan-purple)");
            dirty |= EditorGUI.EndChangeCheck();

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_keying, "Keying");
            editor.ShaderProperty(_keyColor, "Key Color");
            dirty |= EditorGUI.EndChangeCheck();

            if (_keying.floatValue > 0)
            {
                editor.ShaderProperty(_keyThreshold, "Threshold");
                editor.ShaderProperty(_keyTolerance, "Tolerance");
                editor.ShaderProperty(_spillRemoval, "Spill Removal");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            Vector4Field(_trim, _textsLTRB, "Trim");
            dirty |= EditorGUI.EndChangeCheck();

            Vector2Field(_scale, "Scale");
            Vector2Field(_offset, "Offset");

            EditorGUILayout.Space();

            editor.ShaderProperty(_fadeToColor, "Fade To Color");

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_opacity, "Opacity");
            dirty |= EditorGUI.EndChangeCheck();

            return dirty;
        }

        // Update the hidden properties based on the exposed ones.
        void UpdateHiddenProperties(Material material)
        {
            // Color balance
            var temp = material.GetFloat("_Temperature");
            var tint = material.GetFloat("_Tint");
            material.SetVector("_ColorBalance", ProcAmp.CalculateColorBalance(temp, tint));

            // Key color
            var ycgco = ProcAmp.RGB2YCgCo(material.GetColor("_KeyColor"));
            material.SetVector("_KeyCgCo", new Vector2(ycgco.y, ycgco.z));

            // Trimming
            var trim = material.GetVector("_Trim");
            material.SetVector("_TrimParams", new Vector4(
                trim.x, trim.w, 1 / (1 - trim.x - trim.z), 1 / (1 - trim.y - trim.w)
            ));

            if (material.GetFloat("_Opacity") == 1 && material.GetFloat("_Keying") == 0)
            {
                // Opaque
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.SetOverrideTag("RenderType", "");
                material.renderQueue = -1;
            }
            else
            {
                // Transparent
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
        }

        #endregion

        #region Special GUI controls

        static void Vector2Field(MaterialProperty prop, string label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            var newValue = EditorGUILayout.Vector2Field(label, prop.vectorValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck()) prop.vectorValue = newValue;
        }

        static void Vector4Field(MaterialProperty prop, GUIContent[] labels, string prefix)
        {
            var height = (EditorGUIUtility.wideMode ? 1 : 2) * EditorGUIUtility.singleLineHeight;
            var rect = EditorGUILayout.GetControlRect(true, height);

            var v = prop.vectorValue;
            var fa = new [] { v.x, v.y, v.z, v.w };

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = prop.hasMixedValue;
            EditorGUI.MultiFloatField(rect, new GUIContent(prefix), labels, fa);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                prop.vectorValue = new Vector4(fa[0], fa[1], fa[2], fa[3]);
        }

        #endregion
    }
}
