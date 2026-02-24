using UnityEngine;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(CharacterController))]
public class MovimientoTopDown : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadCaminar = 6f;
    public float velocidadCorrer = 12f;  
    public float velocidadGiro = 150f; // NUEVO: Velocidad a la que gira sobre sí mismo
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
        float velocidadActual = velocidadCaminar; 
        
        if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
        {
            velocidadActual = velocidadCorrer; 
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

        // --- 5. GIRAR AL PERSONAJE (A y D) ---
        // Ahora A y D giran TODO el cuerpo del personaje de forma real
        transform.Rotate(Vector3.up * input.x * velocidadGiro * Time.deltaTime);

        // --- 6. MOVER HACIA ADELANTE/ATRÁS (W y S) ---
        // La W siempre te moverá exactamente hacia donde esté apuntando tu cara/cámara
        Vector3 movimiento = transform.forward * input.y;
        controller.Move(movimiento * velocidadActual * Time.deltaTime);

        // --- 7. GRAVEDAD ---
        if (controller.isGrounded && velocidadVertical.y < 0) velocidadVertical.y = -2f;
        velocidadVertical.y += gravedad * Time.deltaTime;
        controller.Move(velocidadVertical * Time.deltaTime);

        // --- 8. ANIMAR ---
        if (animator != null)
        {
            // Usamos Mathf.Abs para que camine tanto si le das a la W como a la S
            animator.SetFloat("Velocidad", Mathf.Abs(input.y) * velocidadActual, 0.1f, Time.deltaTime);
        }
    }
}