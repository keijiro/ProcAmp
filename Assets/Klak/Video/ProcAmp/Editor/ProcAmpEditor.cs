using UnityEngine;
using UnityEditor;

namespace Klak.Video
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProcAmp))]
    public class ProcAmpEditor : Editor
    {
        SerializedProperty _sourceVideo;
        SerializedProperty _sourceTexture;

        SerializedProperty _brightness;
        SerializedProperty _contrast;
        SerializedProperty _saturation;

        SerializedProperty _temperature;
        SerializedProperty _tint;

        SerializedProperty _keying;
        SerializedProperty _keyColor;
        SerializedProperty _keyThreshold;
        SerializedProperty _keyTolerance;
        SerializedProperty _spillRemoval;

        SerializedProperty _trim;
        SerializedProperty _scale;
        SerializedProperty _offset;

        SerializedProperty _fadeToColor;
        SerializedProperty _opacity;

        SerializedProperty _targetTexture;
        SerializedProperty _targetImage;
        SerializedProperty _blitToScreen;

        static GUIContent _textTint = new GUIContent("Tint (cyan-purple)");
        static GUIContent _textThreshold = new GUIContent("Threshold");
        static GUIContent _textTolerance = new GUIContent("Tolerance");
        static GUIContent _textTrim = new GUIContent("Trim");

        static GUIContent[] _textsLTRB = {
            new GUIContent("L"), new GUIContent("T"),
            new GUIContent("R"), new GUIContent("B")
        };

        void OnEnable()
        {
            _sourceVideo = serializedObject.FindProperty("_sourceVideo");
            _sourceTexture = serializedObject.FindProperty("_sourceTexture");

            _brightness = serializedObject.FindProperty("_brightness");
            _contrast = serializedObject.FindProperty("_contrast");
            _saturation = serializedObject.FindProperty("_saturation");

            _temperature = serializedObject.FindProperty("_temperature");
            _tint = serializedObject.FindProperty("_tint");

            _keying = serializedObject.FindProperty("_keying");
            _keyColor = serializedObject.FindProperty("_keyColor");
            _keyThreshold = serializedObject.FindProperty("_keyThreshold");
            _keyTolerance = serializedObject.FindProperty("_keyTolerance");
            _spillRemoval = serializedObject.FindProperty("_spillRemoval");

            _trim = serializedObject.FindProperty("_trim");
            _scale = serializedObject.FindProperty("_scale");
            _offset = serializedObject.FindProperty("_offset");

            _fadeToColor = serializedObject.FindProperty("_fadeToColor");
            _opacity = serializedObject.FindProperty("_opacity");

            _targetTexture = serializedObject.FindProperty("_targetTexture");
            _targetImage = serializedObject.FindProperty("_targetImage");
            _blitToScreen = serializedObject.FindProperty("_blitToScreen");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var procAmp = (ProcAmp)target;

            var showBoth = _sourceVideo.hasMultipleDifferentValues |
                           _sourceTexture.hasMultipleDifferentValues;

            if (showBoth || _sourceTexture.objectReferenceValue == null)
                EditorGUILayout.PropertyField(_sourceVideo);

            if (showBoth || _sourceVideo.objectReferenceValue == null)
                EditorGUILayout.PropertyField(_sourceTexture);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_brightness);
            EditorGUILayout.PropertyField(_contrast);
            EditorGUILayout.PropertyField(_saturation);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_temperature);
            EditorGUILayout.PropertyField(_tint, _textTint);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_keying);
            EditorGUILayout.PropertyField(_keyColor);

            if (_keying.hasMultipleDifferentValues || _keying.boolValue)
            {
                EditorGUILayout.PropertyField(_keyThreshold, _textThreshold);
                EditorGUILayout.PropertyField(_keyTolerance, _textTolerance);
                EditorGUILayout.PropertyField(_spillRemoval);
            }

            EditorGUILayout.Space();

            Vector4Field(_trim, _textsLTRB, _textTrim);
            EditorGUILayout.PropertyField(_scale);
            EditorGUILayout.PropertyField(_offset);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_fadeToColor);
            EditorGUILayout.PropertyField(_opacity);

            if (procAmp.GetComponent<Camera>() == null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_targetTexture);
                EditorGUILayout.PropertyField(_targetImage);
                EditorGUILayout.PropertyField(_blitToScreen);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void Vector4Field(SerializedProperty prop, GUIContent[] labels, GUIContent prefix)
        {
            var height = (EditorGUIUtility.wideMode ? 1 : 2) * EditorGUIUtility.singleLineHeight;
            var rect = EditorGUILayout.GetControlRect(true, height);
            var cur = prop.Copy();
            cur.NextVisible(true);
            EditorGUI.MultiPropertyField(rect, labels, cur, prefix);
        }
    }
}
