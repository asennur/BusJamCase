#if UNITY_EDITOR
using BusJam.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ResultTimeline))]
public sealed class ResultTimelineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var timeline = (ResultTimeline)target;
        EditorGUILayout.HelpBox("Start controls appearance order. Collection starts on Continue; Retry starts on Retry. Reordering this list does not change Start times.", MessageType.Info);
        EditorGUILayout.HelpBox("Expand Celebration to tune the bus. CelebrationBus Enabled controls visibility; its Start and Duration are derived from Celebration.", MessageType.Info);
        DrawDefaultInspector();
        if (!string.IsNullOrEmpty(timeline.ValidationMessage)) EditorGUILayout.HelpBox(timeline.ValidationMessage, MessageType.Warning);
        foreach (ResultTrack track in System.Enum.GetValues(typeof(ResultTrack))) EditorGUILayout.LabelField(track + " length", timeline.Length(track).ToString("0.###") + " s");
        if (GUILayout.Button("Open Result Timeline Preview")) ResultTimelinePreview.Open(timeline);
    }
}

[CustomPropertyDrawer(typeof(ResultStep))]
public sealed class ResultStepDrawer : PropertyDrawer
{
    private static readonly string[] Fields = { "id", "enabled", "start", "duration", "ease", "overshoot", "stagger" };
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => (property.isExpanded ? 8 : 1) * (EditorGUIUtility.singleLineHeight + 2);
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position,label,property);
        var id = (ResultStepId)property.FindPropertyRelative("id").enumValueIndex;
        position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position,property.isExpanded,ResultTimeline.Track(id)+" / "+ObjectNames.NicifyVariableName(id.ToString()),true);
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (string field in Fields)
            {
                position.y += EditorGUIUtility.singleLineHeight+2;
                using (new EditorGUI.DisabledScope(id == ResultStepId.CelebrationBus && field != "enabled" && field != "id" || field=="overshoot" && property.FindPropertyRelative("ease").enumValueIndex!=(int)ResultEase.OutBack
                    || field=="stagger" && id!=ResultStepId.Confetti && id!=ResultStepId.CoinFlight))
                    EditorGUI.PropertyField(position,property.FindPropertyRelative(field));
            }
            EditorGUI.indentLevel--;
        }
        EditorGUI.EndProperty();
    }
}
#endif
