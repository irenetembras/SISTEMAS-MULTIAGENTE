using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(CharacterController))]
public class MovimientoTopDown : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 6f;
    public float velocidadCorrer = 12f;  
    public float gravedad = -9.8f;

    [Header("Cámara")]
    public Camera camaraJugador; 
    public float sensibilidadRotacion = 0.5f;
    
    [Header("Zoom (Rueda Ratón)")]
    public float velocidadZoom = 0.8f; 
    public float zoomMinimo = 2f;
    public float zoomMaximo = 20f;

    private CharacterController controller;
    private Vector3 velocidadVertical; 

    public Animator animator;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (camaraJugador == null) camaraJugador = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        // --- 1. LEER TECLADO (WASD) ---
        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
        }

        // --- 2. DETECTAR SPRINTAR (SHIFT) ---
        float velocidadActual = velocidadCaminar; // Por defecto caminamos
        
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            velocidadActual = velocidadCorrer; // Si pulsas Shift, sprintamos
        }

        // --- 3. ROTACIÓN (Clic Derecho) ---
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            float mouseX = Mouse.current.delta.ReadValue().x;
            transform.Rotate(Vector3.up * mouseX * sensibilidadRotacion);
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // --- 4. ZOOM (Scroll ratón) ---
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.y.ReadValue();
            if (scroll != 0)
            {
                float zoomCambio = scroll * 0.75f * velocidadZoom;
                camaraJugador.orthographicSize -= zoomCambio;
                camaraJugador.orthographicSize = Mathf.Clamp(camaraJugador.orthographicSize, zoomMinimo, zoomMaximo);
            }
        }

        // --- 5. MOVER AL PERSONAJE ---
        Vector3 movimiento = transform.right * input.x + transform.forward * input.y;
        if (movimiento.magnitude > 1) movimiento.Normalize();

        controller.Move(movimiento * velocidadActual * Time.deltaTime);

        // --- 6. GRAVEDAD ---
        if (controller.isGrounded && velocidadVertical.y < 0) velocidadVertical.y = -2f;
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);



        Vector3 direccion = transform.right * input.x + transform.forward * input.y;
        float velocidadFinal = direccion.magnitude * velocidadActual;

        if (animator != null)
        {
            animator.SetFloat("Velocidad", velocidadFinal, 0.1f, Time.deltaTime);
        }
    }
}