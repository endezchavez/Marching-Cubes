using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivety = 100f;
    public float minVerticalRot = -90f;
    public float maxVerticalRot = 90f;

    public Transform playerTransform;

    private float xRotation;

    private Vector2 mouseDelta;
    private float mouseX;
    private float mouseY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        mouseDelta = InputManager.Instance.GetMouseDelta();
        mouseX = mouseDelta.x * mouseSensitivety * Time.deltaTime;
        mouseY = mouseDelta.y * mouseSensitivety * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalRot, maxVerticalRot);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerTransform.Rotate(Vector3.up * mouseX);
    }
}
