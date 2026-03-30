using System;
using UnityEngine;

[Obsolete]
public class RotateComponentTest : MonoBehaviour
{
    [SerializeField]
    private float AnglePerSecond = 60;

    private void Update()
    {
        transform.Rotate(Vector3.up, AnglePerSecond * Time.deltaTime);
    }
}
