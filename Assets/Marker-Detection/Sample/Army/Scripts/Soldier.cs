using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soldier : MonoBehaviour
{
    float f = 0f;
    Transform StartPoint;
    Transform EndPoint;

    private void Start()
    {
        StartPoint = GameObject.Find("StartPoint").transform;
        EndPoint = GameObject.Find("EndPoint").transform;
    }
    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(StartPoint.position, EndPoint.transform.position,f/3f);
        f += Time.deltaTime;
        if (f > 3f)
            Destroy(this.gameObject);
    }
}
