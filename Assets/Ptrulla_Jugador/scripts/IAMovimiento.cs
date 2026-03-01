using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

// Esta clase se encarga de mover al agente, ya sea patrullando, persiguiendo o buscando
public class IAMovimiento : MonoBehaviour
{
     // Variables Públicas
    [Header("Ruta de Patrulla")]
    public Transform[] puntosPatrulla;
    public float radioLlegada = 0.5f;

    [Header("Velocidades")]
    public float velocidadPatrulla = 3.5f;
    public float velocidadPersecucion = 8f;

    [Header("Animador (opcional)")]
    public Animator animator;

    // Variables Internas
    private NavMeshAgent agent;
    private int indicePatrulla = 0;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    // Inicializamos el agente para que empiece patrullando
    void Start()
    {
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            agent.SetDestination(puntosPatrulla[indicePatrulla].position);
        }
    }
    // Actualizamos el parámetro de velocidad en el animador para que las animaciones respondan al movimiento
    void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("Velocidad", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
    }


    // Nos dice si el agente ya ha llegado a su destino
    public bool HaLlegadoAlDestino()
    {
        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, radioLlegada))
        {
            return true;
        }
        return false;
    }

    // Va a un punto a la velocidad que le mande el cerebro
    public void MoverA(Vector3 destino, float velocidad)
    {
        agent.isStopped = false;
        agent.speed = velocidad;
        agent.SetDestination(destino);
    }

    // Genera un punto aleatorio en el NavMesh cerca de un centro (para buscar)
    public Vector3 ObtenerPuntoAleatorioCercano(Vector3 centro, float radio)
    {
        Vector3 direccionAleatoria = Random.insideUnitSphere * radio;
        direccionAleatoria += centro;
        
        NavMeshHit hit;
        // Busca el punto válido más cercano en el NavMesh
        if (NavMesh.SamplePosition(direccionAleatoria, out hit, radio, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return centro; // Si falla, se queda en el centro
    }

    // Función para perseguir al jugador
    public void Perseguir(Vector3 destino)
    {
        MoverA(destino, velocidadPersecucion);
    }

    // Función para patrullar entre puntos
    public void Patrullar()
    {
        agent.isStopped = false;
        agent.speed = velocidadPatrulla;

        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        if (HaLlegadoAlDestino())
        {
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            agent.SetDestination(puntosPatrulla[indicePatrulla].position);
        }
        else if (!agent.hasPath)
        {
            agent.SetDestination(puntosPatrulla[indicePatrulla].position);
        }
    }

    // Función para ir al punto de patrulla más cercano (usada al perder al jugador por completo)
    public void IrAlPuntoMasCercano()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;
        float mejorDist = float.MaxValue;
        int mejorIndex = 0;
        for (int i = 0; i < puntosPatrulla.Length; i++)
        {
            float d = Vector3.Distance(transform.position, puntosPatrulla[i].position);
            if (d < mejorDist)
            {
                mejorDist = d;
                mejorIndex = i;
            }
        }
        indicePatrulla = mejorIndex;
        agent.SetDestination(puntosPatrulla[indicePatrulla].position);
    }

    // Función para detener al agente (usada al morir o al desactivar)
    public void Detener()
    {
        agent.isStopped = true;
    }
}