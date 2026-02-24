using UnityEngine;

public class AtraparLadron : MonoBehaviour
{
    // Esta variable guardará nuestro texto de Game Over
    public GameObject pantallaGameOver;

    // Esta función se activa automáticamente cuando el guardia choca con algo
    private void OnCollisionEnter(Collision choque)
    {
        // Comprobamos si el objeto con el que hemos chocado tiene la etiqueta "Player"
        if (choque.gameObject.CompareTag("Player"))
        {
            // Encendemos el texto de GAME OVER
            pantallaGameOver.SetActive(true);
            
            // Congelamos el tiempo para que el juego se detenga
            Time.timeScale = 0f; 
        }
    }
}
