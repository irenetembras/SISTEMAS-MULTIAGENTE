using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ZonaEscape : MonoBehaviour
{
    [Header("UI de Victoria")]
    public GameObject pantallaVictoria;

    private void OnTriggerEnter(Collider other)
    {
        // Si nos toca el jugador...
        if (other.CompareTag("Player"))
        {
            // ...le preguntamos al otro script si ya robó la mesa
            if (RecogerObjetivo.tieneElBotin == true)
            {
                Debug.Log("¡HAS ESCAPADO CON EL BOTÍN! GANASTE.");

                // 1. Mostramos el cartel
                if (pantallaVictoria != null) pantallaVictoria.SetActive(true);

                // 2. Pausamos el tiempo
                Time.timeScale = 0f;

                // 3. Reiniciamos
                StartCoroutine(ReiniciarJuego());
            }
            else
            {
                // Si entra a la salida pero no tiene el botín, le avisamos por consola
                Debug.Log("¡Aún no tienes el botín! Vuelve a por él.");
            }
        }
    }

    IEnumerator ReiniciarJuego()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}