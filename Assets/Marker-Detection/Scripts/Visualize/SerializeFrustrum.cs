using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SerializeFrustrum : MonoBehaviour
{
    private float _nearDistance = 0.2f;
    private float _aspect = 1.778f;
    private float _vFov = 75f;
    Transform _viewFrustrum;

    private void Awake()
    {
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.parent = transform;
        _viewFrustrum = plane.transform;
        _viewFrustrum.localRotation = Quaternion.Euler(new Vector3(-90f, 0f, 0f));
        _viewFrustrum.localPosition = Vector3.forward * _nearDistance;
        float vslide = 2 * Mathf.Tan(Mathf.Deg2Rad * _vFov / 2) * _nearDistance * .1f;
        _viewFrustrum.localScale = new Vector3(vslide * _aspect, 1f, vslide);


        Material transparentGreen = new Material(Shader.Find("Standard"));
        transparentGreen.SetFloat("_Mode", 3);
        transparentGreen.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        transparentGreen.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        transparentGreen.SetInt("_ZWrite", 0);
        transparentGreen.DisableKeyword("_ALPHATEST_ON");
        transparentGreen.EnableKeyword("_ALPHABLEND_ON");
        transparentGreen.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        transparentGreen.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        transparentGreen.color = new Color(0.0f, 1.0f, 0.0f, 0.01f); ;
        Material outlineFill = new Material(Shader.Find("Custom/Outline Fill"));
        outlineFill.SetFloat("_OutlineWidth", 10f);
        outlineFill.SetColor("_OutlineColor", Color.green);
        Material outlineMask = new Material(Shader.Find("Custom/Outline Mask"));

        _viewFrustrum.GetComponent<Renderer>().material = null;
        _viewFrustrum.GetComponent<Renderer>().sharedMaterials = new Material[] { transparentGreen, outlineFill, outlineMask };
    }
    private void OnEnable()
    {
        _viewFrustrum.gameObject.SetActive(true);
    }
    private void OnDisable()
    {
        _viewFrustrum.gameObject.SetActive(false);
    }
    public void OnDrawGizmosSelected()
    {
        var rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, new Vector3(1, 1, 1));
        Gizmos.color = Color.green;
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawFrustum(Vector3.zero, 75.0f, 5f, 0.08f, 1.777f);
    }
}
