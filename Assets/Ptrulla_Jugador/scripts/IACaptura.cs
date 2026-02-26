using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IACaptura : MonoBehaviour
{
    [Header("Game Over")]
    public GameObject pantallaGameOver;
    
    private IAMovimiento movimiento;

    void Awake()
    {
        movimiento = GetComponent<IAMovimiento>();
    }

    // 1. Si el guardia tiene un collider normal (Físico - El que usaba tu compi)
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

    // Esta es la función principal que hace la magia, unificada para no repetir código
    private void AtraparJugador()
    {
        // 1. Paramos las piernas
        if (movimiento != null) movimiento.Detener();

        // 2. Mostramos el cartel
        if (pantallaGameOver != null) pantallaGameOver.SetActive(true);

        // 3. Congelamos el tiempo (Como hacía el de tu compi)
        Time.timeScale = 0f;

        // 4. Iniciamos la cuenta atrás para reiniciar
        StartCoroutine(ReiniciarJuego());
    }

    IEnumerator ReiniciarJuego()
    {
        yield return new WaitForSecondsRealtime(3f); // Espera 3 segundos
        Time.timeScale = 1f; // Descongela el tiempo
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Recarga el nivel
    }
}