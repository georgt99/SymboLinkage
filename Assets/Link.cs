using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Link : MonoBehaviour
{
    public GameObject bar;
    public GameObject cylinder1;
    public GameObject cylinder2;

    public Joint j1;
    public Joint j2;

    public Vector3 normal = Vector3.forward;

    private void Update()
    {
        // overall position
        transform.position = (j1.transform.position + j2.transform.position) / 2f;
        // overall rotation
        transform.LookAt(
            transform.position + normal,
            Vector3.Cross(j1.transform.position - transform.position, normal)
            ) ;
        // "length"
        float jointDistance = Vector3.Distance(j1.transform.position, j2.transform.position);
        bar.transform.localScale = new Vector3(
            jointDistance,
            bar.transform.localScale.y,
            bar.transform.localScale.z
            );
        cylinder1.transform.localPosition = new Vector3(-jointDistance/2f, 0, 0);
        cylinder2.transform.localPosition = new Vector3(jointDistance/2f, 0, 0);
    }
}
