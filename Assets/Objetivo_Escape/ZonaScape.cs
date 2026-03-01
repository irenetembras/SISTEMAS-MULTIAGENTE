using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Gestiona la zona de escape, detectando si el jugador ha recogido el botín,
// y mostrando la pantalla de victoria si escapa con él y reiniciando el juego de nuevo despues.
public class ZonaEscape : MonoBehaviour
{
    [Header("UI de Victoria")]
    public GameObject pantallaVictoria;

    //funcion para detectar la colisión con el jugador y comprobar si ha recogido el botín para ganar o no
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {

            if (RecogerObjetivo.tieneElBotin == true)
            {
                Debug.Log("¡HAS ESCAPADO CON EL BOTÍN! GANASTE.");
     
                if (pantallaVictoria != null) pantallaVictoria.SetActive(true);

                Time.timeScale = 0f;
                StartCoroutine(ReiniciarJuego());
            }
            else
            {
                Debug.Log("¡Aún no tienes el botín! Vuelve a por él.");
            }
        }
    }

    // Función para reiniciar el juego después de mostrar la pantalla de victoria
    IEnumerator ReiniciarJuego()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}