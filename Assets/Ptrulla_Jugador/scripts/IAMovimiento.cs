using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class IAMovimiento : MonoBehaviour
{
    [Header("El Turno de Guardia (Ruta Default)")]
    public SectorTactico sectorBase;
    public SectorTactico sectorActual;

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

    private NavMeshAgent agent;
    private bool sprintHaciaSector = false;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (sectorBase != null)
        {
            AsignarNuevaRutaDesdeSector(sectorBase);
        }
        else
        {
            Debug.LogWarning($" [{gameObject.name}] no tiene un sector base asignado. Se quedará quieto.");
        }
    }

    void Update()
    {
        if (animator != null)
        {
            animator.SetFloat("Velocidad", agent.velocity.magnitude, 0.1f, Time.deltaTime);
        }
    }

    public bool HaLlegadoAlDestino()
    {
        if (agent.pathPending) return false;
        if (!agent.hasPath)
        {
            if (Vector3.Distance(transform.position, agent.destination) <= Mathf.Max(agent.stoppingDistance, radioLlegada))
                return true;
            return false;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathPartial) return false;

        return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, radioLlegada);
    }

    public void MoverA(Vector3 destino, float velocidad)
    {
        agent.isStopped = false;
        agent.speed = velocidad;
        agent.SetDestination(destino);
    }

    public Vector3 ObtenerPuntoAleatorioCercano(Vector3 centro, float radio)
    {
        // Intentamos hasta 5 veces encontrar un punto accesible por NavMesh
        for (int i = 0; i < 5; i++)
        {
            Vector2 circulo = Random.insideUnitCircle * radio;
            Vector3 direccionAleatoria = centro + new Vector3(circulo.x, 0f, circulo.y);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(direccionAleatoria, out hit, 2.0f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    return hit.position;
                }
            }
        }

        return transform.position;
    }

    public void Perseguir(Vector3 destino)
    {
        MoverA(destino, velocidadPersecucion);
    }

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
            // Al llegar al nuevo sector, volvemos a paso de patrulla
            if (HaLlegadoAlDestino()) sprintHaciaSector = false;
        }
        else
        {
            agent.speed = velocidadPatrulla;
        }

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

    public void AsignarNuevaRutaDesdeSector(SectorTactico nuevoSector)
    {
        if (nuevoSector == null || nuevoSector.puntosDeInteres == null || nuevoSector.puntosDeInteres.Length == 0)
            return;

        sprintHaciaSector = (sectorBase != null && nuevoSector != sectorBase);

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
