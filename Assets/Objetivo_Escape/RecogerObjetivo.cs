using UnityEngine;

// Gestiona la recogida del botín y sirve como memoria global del juego 
// para que la IA (emboscada) y la Zona de Escape sepan si el jugador lo ha robado.
public class RecogerObjetivo : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject objetoParaOcultar;

    public static bool tieneElBotin = false; 

    void Start()
    {
        tieneElBotin = false; // forzar a que el botín no esté recogido al inicio del juego y al morir
    }

    // función que srve para detectar la colisión con el jugador y actualizar el estado del botín
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !tieneElBotin)
        {
            Debug.Log("¡Botín recogido! ¡Corre a la salida!");
            
            tieneElBotin = true;

            if (objetoParaOcultar != null)
            {
                objetoParaOcultar.SetActive(false);
            }
        }
    }
}