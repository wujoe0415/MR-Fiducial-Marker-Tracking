using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TagTrackProvider))]
public class ArucoTrackerEditor : Editor
{
    private Texture2D previewTexture;

    private void OnEnable()
    {
        // Load your sprite as a Texture2D for preview
        TagTrackProvider arcuo = (TagTrackProvider)target;
        previewTexture = AssetPreview.GetAssetPreview(arcuo.PreviewSprite);
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        UpdatePreviewTexture();
        // Display the preview texture
        if (previewTexture != null)
        {
            GUILayout.Label("Preview Texture:");
            GUILayout.Label(previewTexture, GUILayout.MaxHeight(100f), GUILayout.MaxWidth(100f));
        }
        Repaint();
    }
    private void UpdatePreviewTexture()
    {
        TagTrackProvider aruco = (TagTrackProvider)target;
        
        string texturePath = "Assets/Marker-Detection/Markers/" + aruco.TagType.ToString() + "/aruco_markers_" + aruco.TagID + ".png";
        Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (loadedTexture != null) { 
            previewTexture  = AssetPreview.GetAssetPreview(loadedTexture);
        }
        else
        {
            Debug.Log("Texture not found for TagID: " + aruco.TagID);
            previewTexture = null;
        }
    }
}
