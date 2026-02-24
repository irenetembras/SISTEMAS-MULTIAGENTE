using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // <-- AÑADE ESTO (Para recargar el nivel)
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class CerebroIA_PatrullaCorregida : MonoBehaviour
{
    [Header("Game Over")]
    public GameObject pantallaGameOver; // <--- NUEVA VARIABLE PARA EL TEXTO

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float radioLlegada = 0.5f; // cuando se considera "llegado" al punto

    [Header("Velocidades")]
    public float velocidadPatrulla = 3.5f;
    public float velocidadPersecucion = 8f;

    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(90f, 250f)] public float anguloVision = 90f;
    public float alturaOjos = 1.6f;
    public float alturaObjetivoRelativa = 1.0f;

    [Header("Oído")]
    public float distanciaOidoAndar = 4f;
    public float distanciaOidoCorrer = 12f;
    public float umbralVelocidadCorrer = 7f;

    [Header("Animador (opcional)")]
    public Animator animator; 

    // Variables internas
    NavMeshAgent agent;
    Transform jugador;
    Vector3 ultimaPosJugador;
    float velocidadRealJugador;
    int indicePatrulla = 0;

    enum Estado { PATRULLANDO, PERSIGUIENDO }
    Estado estado = Estado.PATRULLANDO;

    float tiempoDesdePerdido = 0f;
    public float tiempoRecordarPerseguir = 0.2f; // tiempo que mantiene persecución tras perder la vista/sonido

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Busca el jugador por tag
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            jugador = go.transform;
            ultimaPosJugador = jugador.position;
        }

        // Si hay puntos, ir al primero
        if (puntosPatrulla != null && puntosPatrulla.Length > 0)
        {
            indicePatrulla = 0;
            SetDestinationPatrulla(indicePatrulla);
        }
    }

    void Update()
    {
        // --- 1) Calculamos velocidad real del jugador (para oído)
        if (jugador != null && Time.deltaTime > 0f)
        {
            float dist = Vector3.Distance(jugador.position, ultimaPosJugador);
            velocidadRealJugador = dist / Time.deltaTime;
            ultimaPosJugador = jugador.position;
        }

        // --- 2) Chequeamos sentidos
        bool visto = PuedeVerAlJugador();
        bool oido = PuedeOirAlJugador();

        if (visto || oido)
        {
            estado = Estado.PERSIGUIENDO;
            tiempoDesdePerdido = 0f;
        }
        else
        {
            if (estado == Estado.PERSIGUIENDO)
            {
                tiempoDesdePerdido += Time.deltaTime;
                if (tiempoDesdePerdido >= tiempoRecordarPerseguir)
                {
                    estado = Estado.PATRULLANDO;
                    // Al volver a patrullar, vamos al punto más cercano para continuidad
                    IrAlPuntoPatrullaMasCercano();
                }
            }
        }

        // --- 3) Ejecutar comportamiento según estado
        if (estado == Estado.PERSIGUIENDO)
        {
            agent.isStopped = false;
            agent.speed = velocidadPersecucion;

            if (jugador != null)
            {
                agent.SetDestination(jugador.position);
            }
        }
        else 
        {
            agent.speed = velocidadPatrulla;

            PatrullarComportamiento();
        }

        // --- 4) Animador (se pasa la velocidad del agente para blend tree)
        if (animator != null)
        {
            float vel = agent.velocity.magnitude;
            animator.SetFloat("Velocidad", vel, 0.1f, Time.deltaTime);
        }
    }

    // ------ PATRULLA ------
    void PatrullarComportamiento()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;

        // Si no tiene ruta o ha llegado al punto actual, avanzamos al siguiente
        if (!agent.pathPending)
        {
            // comprobación de llegada
            if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, radioLlegada))
            {
                // Llegado al punto actual -> incrementamos índice y vamos al siguiente
                indicePatrulla = (indicePatrulla + 1) % puntosPatrulla.Length;
                SetDestinationPatrulla(indicePatrulla);
            }
            else
            {
                // Si no tiene destino (caso inicial) o el destino fue perdido, asegúrate de que siga con el punto actual
                if (!agent.hasPath)
                {
                    SetDestinationPatrulla(indicePatrulla);
                }
            }
        }
    }

    // ------ DESTINO PATRULLA ------
    void SetDestinationPatrulla(int index)
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0) return;
        agent.SetDestination(puntosPatrulla[index].position);
    }

    // ------ PUNTO MÁS CERCANO PATRULLA ------
    void IrAlPuntoPatrullaMasCercano()
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
        SetDestinationPatrulla(indicePatrulla);
    }

    // ------ SENTIDOS ------
    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;

        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 objetivo = jugador.position + Vector3.up * alturaObjetivoRelativa;
        Vector3 dir = objetivo - ojos;
        float distancia = dir.magnitude;

        if (distancia > distanciaVision) return false;

        Vector3 dirH = dir;
        dirH.y = 0f;
        if (dirH.sqrMagnitude < 0.001f) return true;
        float ang = Vector3.Angle(transform.forward, dirH.normalized);
        if (ang > anguloVision / 2f) return false;

        // RaycastAll ordenado por distancia para evitar autocolisión
        RaycastHit[] hits = Physics.RaycastAll(ojos, dir.normalized, distancia, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            // Ignora triggers
            if (hit.collider.isTrigger) continue;

            // Ignora colisiones con este enemigo (root o children)
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            // Si es el jugador (root o hijo) o tiene tag Player -> lo vemos
            if (hit.transform == jugador || hit.transform.IsChildOf(jugador) || hit.collider.CompareTag("Player"))
            {
                return true;
            }
            else
            {
                // Golpeó otra cosa primero: vista bloqueada
                return false;
            }
        }

        return false;
    }

    // ------ OÍDO ------
    bool PuedeOirAlJugador()
    {
        if (jugador == null) return false;

        float distancia = Vector3.Distance(transform.position, jugador.position);
        float vel = velocidadRealJugador;

        if (vel > umbralVelocidadCorrer)
        {
            if (distancia <= distanciaOidoCorrer) return true;
        }
        else if (vel > 0.1f)
        {
            if (distancia <= distanciaOidoAndar) return true;
        }

        return false;
    }

    // ---------------------------------------------------------
    //  NUEVO: LÓGICA PARA ATRAPAR AL JUGADOR
    // ---------------------------------------------------------
    // ---------------------------------------------------------
    //  LÓGICA PARA ATRAPAR AL JUGADOR
    // ---------------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Detenemos al guardia
            if (agent != null) agent.isStopped = true;

            // 2. Encendemos la pantalla de Game Over
            if (pantallaGameOver != null)
            {
                pantallaGameOver.SetActive(true);
            }

            // 3. Pausamos el tiempo del juego
            Time.timeScale = 0f;

            // 4. NUEVO: Empezamos la cuenta atrás para reiniciar
            StartCoroutine(ReiniciarJuego());
        }
    }

    // NUEVO: Temporizador para recargar la escena
    IEnumerator ReiniciarJuego()
    {
        // Esperamos 3 segundos reales (usamos Realtime porque Time.timeScale es 0)
        yield return new WaitForSecondsRealtime(3f);

        // ¡Súper importante! Volvemos a poner el tiempo a la velocidad normal (1)
        // Si no hacemos esto, el juego empezará pero todo estará congelado.
        Time.timeScale = 1f;

        // Recargamos el mapa en el que estamos ahora mismo
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}