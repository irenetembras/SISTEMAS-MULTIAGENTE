using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Esta clase se encarga de detectar si un jugador ha sido atrapado, mostrando el Game Over y reiniciando el juego después de unos segundos.
public class IACaptura : MonoBehaviour
{
    [Header("Game Over")]
    public GameObject pantallaGameOver;
    
    private IAMovimiento movimiento;

    void Awake()
    {
        movimiento = GetComponent<IAMovimiento>();
    }

    // 1. Si el guardia tiene un collider normal (Físico)
    private void OnCollisionEnter(Collision choque)
    {
        if (choque.gameObject.CompareTag("Player"))
        {
            AtraparJugador();
        }
    }

    // 2. Si el guardia tiene un collider tipo "Trigger" (Fantasma)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AtraparJugador();
        }
    }

    // Esta es la función que se encarga de atrapar al jugador, mostrar el Game Over y reiniciar el juego después de unos segundos
    private void AtraparJugador()
    {
        if (movimiento != null) movimiento.Detener();

        if (pantallaGameOver != null) pantallaGameOver.SetActive(true);
        Time.timeScale = 0f;
        StartCoroutine(ReiniciarJuego());
    }

    // funcion para reiniciar el juego después de unos segundos, descongelando el tiempo y recargando la escena actual
    IEnumerator ReiniciarJuego()
    {
        yield return new WaitForSecondsRealtime(3f); 
        Time.timeScale = 1f; 
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }
}