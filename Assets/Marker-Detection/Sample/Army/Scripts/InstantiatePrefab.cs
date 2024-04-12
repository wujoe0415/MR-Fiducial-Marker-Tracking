using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstantiatePrefab: MonoBehaviour
{
    public GameObject Prefab;
    private bool _isOpen = false;
    private MeshRenderer _render;

    private void Awake()
    {
        _render = GetComponent<MeshRenderer>();
        StartCoroutine(InstantiateArmy());
    }
    public void OnTrack()
    {
        _isOpen = true;
        _render.enabled = true;
    }
    public void OnLoseTrack()
    {
        _isOpen = false;
        _render.enabled = false;
    }
    private IEnumerator InstantiateArmy()
    {
        while (true)
        {
            if(_isOpen)
            {
                Instantiate(Prefab, transform.position, transform.rotation);
                Prefab.AddComponent(typeof(Soldier));
                yield return new WaitForSeconds(0.15f);
            }
            yield return null;
        }
    }
}
