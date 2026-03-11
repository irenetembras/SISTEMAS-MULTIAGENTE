using UnityEngine;

// Módulo exclusivo para la lógica auditiva (Responsabilidad Única)
public class SensorOido : MonoBehaviour
{
    [Header("Oído")]
    public float distanciaOidoAndar = 4f;
    public float distanciaOidoCorrer = 12f;
    public float umbralVelocidadCorrer = 7f;

    private Vector3 ultimaPosJugador;
    private float velocidadRealJugador;

    public bool EvaluarOido(Transform jugador)
    {
        if (Time.deltaTime > 0f)
        {
            float dist = Vector3.Distance(jugador.position, ultimaPosJugador);
            velocidadRealJugador = dist / Time.deltaTime;
            ultimaPosJugador = jugador.position;
        }

        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (velocidadRealJugador > umbralVelocidadCorrer)
        {
            if (distancia <= distanciaOidoCorrer) return true;
        }
        else if (velocidadRealJugador > 0.1f)
        {
            if (distancia <= distanciaOidoAndar) return true;
        }
        return false;
    }
}