using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(TutorialPage))]
public class TutorialPageEditor : Editor
{
    private SerializedProperty layoutProp;
    private SerializedProperty tutorialTextProp;
    private SerializedProperty media1Prop;
    private SerializedProperty media2Prop;
    private SerializedProperty associatedTowerProp;
    private SerializedProperty associatedSampleProp;

    private static readonly string[] layoutNames = {
        "Media Left\nText Right",
        "Media Right\nText Left",
        "Media Top\nText Bottom",
        "Media Bottom\nText Top",
        "Media Only",
        "Text Only"
    };

    void OnEnable()
    {
        layoutProp = serializedObject.FindProperty("layout");
        tutorialTextProp = serializedObject.FindProperty("tutorialText");
        media1Prop = serializedObject.FindProperty("media1");
        media2Prop = serializedObject.FindProperty("media2");
        associatedTowerProp = serializedObject.FindProperty("associatedTower");
        associatedSampleProp = serializedObject.FindProperty("associatedSample");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        TutorialLayout currentLayout = (TutorialLayout)layoutProp.enumValueIndex;
        bool needsText = currentLayout != TutorialLayout.MediaOnly;
        bool needsMedia = currentLayout != TutorialLayout.TextOnly;

        // Layout selector
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        DrawLayoutSelector();

        // Text content
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Text Content", EditorStyles.boldLabel);

        if (needsText)
        {
            EditorGUILayout.PropertyField(tutorialTextProp, new GUIContent("Tutorial Text"));
        }
        else
        {
            EditorGUILayout.HelpBox("Text hidden in Media Only layout.", MessageType.Info);
        }

        // Media content
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Media Content", EditorStyles.boldLabel);

        if (needsMedia)
        {
            DrawMediaItem("Media 1 (Primary)", media1Prop);
            EditorGUILayout.Space(10);
            DrawMediaItem("Media 2 (Optional)", media2Prop);
        }
        else
        {
            EditorGUILayout.HelpBox("Media hidden in Text Only layout.", MessageType.Info);
        }

        // Association
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("Association (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(associatedTowerProp, new GUIContent("Associated Tower"));
        EditorGUILayout.PropertyField(associatedSampleProp, new GUIContent("Associated Sample"));

        serializedObject.ApplyModifiedProperties();
    }

    void DrawLayoutSelector()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        int currentIndex = layoutProp.enumValueIndex;

        EditorGUILayout.BeginVertical();
        for (int row = 0; row < 2; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < 3; col++)
            {
                int index = row * 3 + col;
                if (index < layoutNames.Length)
                {
                    bool isSelected = (index == currentIndex);
                    GUIStyle style = isSelected ? GetSelectedStyle() : GUI.skin.button;

                    if (GUILayout.Button(layoutNames[index], style, GUILayout.Width(100), GUILayout.Height(40)))
                    {
                        layoutProp.enumValueIndex = index;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    GUIStyle GetSelectedStyle()
    {
        var style = new GUIStyle(GUI.skin.button);
        style.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.5f, 0.8f));
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        return style;
    }

    Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;

        Texture2D tex = new Texture2D(width, height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void DrawMediaItem(string label, SerializedProperty mediaProp)
    {
        SerializedProperty typeProp = mediaProp.FindPropertyRelative("type");
        SerializedProperty imageProp = mediaProp.FindPropertyRelative("image");
        SerializedProperty videoProp = mediaProp.FindPropertyRelative("video");
        SerializedProperty captionProp = mediaProp.FindPropertyRelative("caption");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Header with type toggle
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel, GUILayout.Width(120));

        typeProp.enumValueIndex = GUILayout.Toolbar(
            typeProp.enumValueIndex, 
            new string[] { "Image", "Video" }, 
            GUILayout.Width(110));

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Content field based on type
        if (typeProp.enumValueIndex == 0)
        {
            EditorGUILayout.PropertyField(imageProp, new GUIContent("Sprite"));

            // Thumbnail preview
            if (imageProp.objectReferenceValue != null)
            {
                Sprite sprite = imageProp.objectReferenceValue as Sprite;
                if (sprite != null && sprite.texture != null)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    Rect rect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(rect, sprite.texture, null, ScaleMode.ScaleToFit);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                }
            }
        }
        else
        {
            EditorGUILayout.PropertyField(videoProp, new GUIContent("Video Clip"));
        }

        EditorGUILayout.PropertyField(captionProp, new GUIContent("Caption"));

        EditorGUILayout.EndVertical();
    }
}
#endif