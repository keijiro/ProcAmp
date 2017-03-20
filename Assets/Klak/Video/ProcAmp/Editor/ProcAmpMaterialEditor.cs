using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Klak.Video
{
    class ProcAmpMaterialEditor : ShaderGUI
    {
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

        static GUIContent[] _textsLTRB = {
            new GUIContent("L"), new GUIContent("T"),
            new GUIContent("R"), new GUIContent("B")
        };

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

            ShaderPropertiesGUI(editor.target as Material, editor);
        }

        public void ShaderPropertiesGUI(Material material, MaterialEditor editor)
        {
            editor.TexturePropertySingleLine(new GUIContent("Source"), _mainTex);

            EditorGUILayout.Space();

            editor.ShaderProperty(_brightness, "Brightness");
            editor.ShaderProperty(_contrast, "Contrast");
            editor.ShaderProperty(_saturation, "Saturation");

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_temperature, "Temperature");
            editor.ShaderProperty(_tint, "Tint (cyan-purple)");
            if (EditorGUI.EndChangeCheck())
            {
                var balance = ProcAmp.CalculateColorBalance(
                    _temperature.floatValue, _tint.floatValue
                );
                material.SetVector("_ColorBalance", balance);
            }

            EditorGUILayout.Space();

            editor.ShaderProperty(_keying, "Keying");

            EditorGUI.BeginChangeCheck();
            editor.ShaderProperty(_keyColor, "Key Color");
            if (EditorGUI.EndChangeCheck())
            {
                var ycgco = ProcAmp.RGB2YCgCo(_keyColor.colorValue);
                material.SetVector("_KeyCgCo", new Vector2(ycgco.y, ycgco.z));
            }

            if (_keying.floatValue > 0)
            {
                editor.ShaderProperty(_keyThreshold, "Threshold");
                editor.ShaderProperty(_keyTolerance, "Tolerance");
                editor.ShaderProperty(_spillRemoval, "Spill Removal");
            }

            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            Vector4Field(_trim, _textsLTRB, "Trim");
            if (EditorGUI.EndChangeCheck())
            {
                var v = _trim.vectorValue;
                material.SetVector("_TrimParams", new Vector4(
                    v.x, v.w, 1 / (1 - v.x - v.z), 1 / (1 - v.y - v.w)
                ));
            }

            Vector2Field(_scale, "Scale");
            Vector2Field(_offset, "Offset");

            EditorGUILayout.Space();

            editor.ShaderProperty(_fadeToColor, "Fade To Color");
            editor.ShaderProperty(_opacity, "Opacity");

            if (_opacity.floatValue == 1 && _keying.floatValue == 0)
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
    }
}
