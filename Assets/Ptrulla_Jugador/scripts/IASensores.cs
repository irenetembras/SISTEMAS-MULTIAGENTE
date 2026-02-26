using UnityEngine;

public class IASensores : MonoBehaviour
{
    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(90f, 250f)] public float anguloVision = 90f;
    public float alturaOjos = 1.6f;
    public float alturaObjetivoRelativa = 1.0f;

    [Header("Oído")]
    public float distanciaOidoAndar = 4f;
    public float distanciaOidoCorrer = 12f;
    public float umbralVelocidadCorrer = 7f;

    // Variables Públicas (El Cerebro leerá esto)
    public bool JugadorDetectado { get; private set; }
    public Transform TransformJugador { get; private set; }

    // Variables Internas
    private Vector3 ultimaPosJugador;
    private float velocidadRealJugador;

    void Start()
    {
        GameObject go = GameObject.FindGameObjectWithTag("Player");
        if (go != null)
        {
            TransformJugador = go.transform;
            ultimaPosJugador = TransformJugador.position;
        }
    }

    void Update()
    {
        if (TransformJugador == null) return;

        // 1. Calcular velocidad real del jugador para el oído
        if (Time.deltaTime > 0f)
        {
            float dist = Vector3.Distance(TransformJugador.position, ultimaPosJugador);
            velocidadRealJugador = dist / Time.deltaTime;
            ultimaPosJugador = TransformJugador.position;
        }

        // 2. Reacción: ¿Veo u Oigo?
        bool visto = PuedeVerAlJugador();
        bool oido = PuedeOirAlJugador();

        // Actualizamos la variable pública para que el cerebro reaccione
        JugadorDetectado = visto || oido;
    }

    private bool PuedeVerAlJugador()
    {
        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 objetivo = TransformJugador.position + Vector3.up * alturaObjetivoRelativa;
        Vector3 dir = objetivo - ojos;
        float distancia = dir.magnitude;

        if (distancia > distanciaVision) return false;

        Vector3 dirH = dir;
        dirH.y = 0f;
        if (dirH.sqrMagnitude < 0.001f) return true;
        
        float ang = Vector3.Angle(transform.forward, dirH.normalized);
        if (ang > anguloVision / 2f) return false;

        RaycastHit[] hits = Physics.RaycastAll(ojos, dir.normalized, distancia, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            if (hit.transform == TransformJugador || hit.transform.IsChildOf(TransformJugador) || hit.collider.CompareTag("Player"))
                return true;
            else
                return false; 
        }
        return false;
    }

    private bool PuedeOirAlJugador()
    {
        float distancia = Vector3.Distance(transform.position, TransformJugador.position);
        
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