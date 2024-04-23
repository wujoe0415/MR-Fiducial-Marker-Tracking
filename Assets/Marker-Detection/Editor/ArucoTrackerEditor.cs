using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MarkerTrackProvider))]
public class ArucoTrackerEditor : Editor
{
    private Texture2D previewTexture;

    private void OnEnable()
    {
        // Load your sprite as a Texture2D for preview
        MarkerTrackProvider arcuo = (MarkerTrackProvider)target;
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
        MarkerTrackProvider aruco = (MarkerTrackProvider)target;
        
        string texturePath = "Assets/Marker-Detection/Markers/" + aruco.MarkerType.ToString() + "/aruco_markers_" + aruco.MarkerID + ".png";
        Texture2D loadedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (loadedTexture != null) { 
            previewTexture  = AssetPreview.GetAssetPreview(loadedTexture);
        }
        else
        {
            Debug.Log("Texture not found for TagID: " + aruco.MarkerID);
            previewTexture = null;
        }
    }
}
