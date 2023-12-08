using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotation : MonoBehaviour
{
    public float Speed;

    private Transform t;
    // Start is called before the first frame update
    void Start()
    {
        t = gameObject.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0,Speed * Time.deltaTime,0);
    }
}
