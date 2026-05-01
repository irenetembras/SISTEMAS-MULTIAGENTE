using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]

// Esta clase se encarga de mover al agente, ya sea patrullando, persiguiendo o buscando
public class IAMovimiento : MonoBehaviour
{   
    [Header("El Turno de Guardia (Ruta Default)")]
    public SectorTactico sectorBase; // ARRASTRA AQUÍ EL SECTOR DESDE EL INSPECTOR DE UNITY
    public SectorTactico sectorActual; // El sector en el que está trabajando AHORA

    [Header("Ruta de Patrulla")]
    public float radioLlegada = 0.5f;

    [Header("Velocidades")]
    public float velocidadPatrulla = 3.5f;
    public float velocidadPersecucion = 8f;
    public float velocidadExploracion = 6f;

    [Header("Animador (opcional)")]
    public Animator animator;

    private Transform[] puntosPatrulla;
    private int indicePatrulla = 0;

    // Variables Internas
    private NavMeshAgent agent;
    private bool sprintHaciaSector = false;


    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    // Inicializamos el agente para que empiece patrullando
    void Start()
    {
        // Al empezar el juego, el guardia asume su puesto por defecto
        if (sectorBase != null)
        {
            AsignarNuevaRutaDesdeSector(sectorBase);
        }
        else
        {
            Debug.LogWarning($" [{gameObject.name}] no tiene un sector base asignado. Se quedará quieto.");
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
        if (agent.pathPending) return false;
        if (!agent.hasPath)
        { 
        // Si no hay ruta, comprobamos a la fuerza bruta si ya estamos sobre la meta
            if (Vector3.Distance(transform.position, agent.destination) <= Mathf.Max(agent.stoppingDistance, radioLlegada))
            {
                return true; // Ya estoy aquí, no necesito ruta.
            }

        return false;
        }

        // LA MAGIA ANTI-ATASCOS DE PUERTAS
        if (agent.pathStatus == NavMeshPathStatus.PathPartial) return false;

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, radioLlegada))
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

   public Vector3 ObtenerPuntoAleatorioCercano(Vector3 centro, float radio)
    {
        // Le damos 5 intentos para encontrar un punto que no esté al otro lado de un muro
        for (int i = 0; i < 5; i++)
        {
            Vector2 circulo = Random.insideUnitCircle * radio;
            Vector3 direccionAleatoria = centro + new Vector3(circulo.x, 0f, circulo.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(direccionAleatoria, out hit, 2.0f, NavMesh.AllAreas))
            {
                // LA CLAVE: Comprobamos si podemos llegar caminando
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position; // ¡Punto válido y accesible!
                }
            }
        }

        // Si todos caen mal, devolvemos nuestra posición actual para no congelarnos
        return transform.position; 
    }
    // Función para perseguir al jugador
    public void Perseguir(Vector3 destino)
    {
        MoverA(destino, velocidadPersecucion);
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
    public void Patrullar()
    {
        agent.isStopped = false;

        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;
        if (puntosPatrulla[indicePatrulla] == null) return;

        if (sprintHaciaSector)
        {
            agent.speed = velocidadPersecucion;
            // si ya llegamos al sector, releajamos la marcha
            if (HaLlegadoAlDestino())
            {
                sprintHaciaSector = false;
            }
        }
        else 
        {
            // Caminar normal mientras hacen la ronda
            agent.speed = velocidadPatrulla;
        }
        // Usamos TU función original para saber si hemos llegado
        if (HaLlegadoAlDestino())
        {
            // Transición instantánea al siguiente punto
            indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
            agent.SetDestination(puntosPatrulla[indicePatrulla].position);
        }
        else if (!agent.hasPath)
        {
            agent.SetDestination(puntosPatrulla[indicePatrulla].position);
        }
    }

    // Conexión con el Planificador Táctico
    public void AsignarNuevaRutaDesdeSector(SectorTactico nuevoSector)
    {
        if (nuevoSector == null || nuevoSector.puntosDeInteres == null || nuevoSector.puntosDeInteres.Length == 0) 
            return;

        if (sectorBase != null && nuevoSector != sectorBase)
        {
            sprintHaciaSector = true;
        }
        else
        {
            sprintHaciaSector = false;
        }

        sectorActual = nuevoSector;
        puntosPatrulla = nuevoSector.puntosDeInteres;
        
        indicePatrulla = 0; 

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(puntosPatrulla[0].position);
        }
    }


    public void VolverAlPuestoBase()
    {
        if (sectorBase != null && sectorActual != sectorBase)
        {
            AsignarNuevaRutaDesdeSector(sectorBase);
        }
    }
}