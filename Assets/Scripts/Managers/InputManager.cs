using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get { return instance; } }

    private static InputManager instance;

    private InputMaster controls;


    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }

        controls = new InputMaster();
    }

    public Vector2 GetMovementInput()
    {
        return controls.Player.Movement.ReadValue<Vector2>();
    }

    public Vector2 GetMouseDelta()
    {
        return controls.Player.MouseDelta.ReadValue<Vector2>();
    }

    public bool PlayerJumpedThisFrame()
    {
        return controls.Player.Jump.triggered;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
