using UnityEngine;

public class RecogerObjetivo : MonoBehaviour
{
    [Header("Configuración")]
    public GameObject objetoParaOcultar; // La mesa visual

    // Esta es la "memoria" global del juego. Empieza en falso.
    public static bool tieneElBotin = false; 

    void Start()
    {
        // Muy importante: Al empezar o reiniciar el nivel, no tenemos el botín
        tieneElBotin = false; 
    }

    private void OnTriggerEnter(Collider other)
    {
        // Si nos toca el jugador y AÚN NO tiene el botín...
        if (other.CompareTag("Player") && !tieneElBotin)
        {
            Debug.Log("¡Botín recogido! ¡Corre a la salida!");
            
            // 1. Guardamos en la memoria que ya lo tenemos
            tieneElBotin = true;

            // 2. Ocultamos la mesa visual
            if (objetoParaOcultar != null)
            {
                objetoParaOcultar.SetActive(false);
            }
        }
    }
}