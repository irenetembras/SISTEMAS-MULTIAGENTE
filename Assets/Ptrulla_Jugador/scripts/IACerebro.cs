using UnityEngine;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
public class IACerebro : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento (Arrastra tus scripts aquí)")]
    public EstadoIA patrulla;
    public EstadoIA persecucion;
    public EstadoIA emboscada;
    public EstadoIA busqueda;              
    public EstadoIA exploracion;           
    public EstadoIA comprobandoObjetivo;   

    [Header("Ajustes de Tiempo")]
    public float tiempoPerdidoParaBuscar = 2.0f; // Tiempo de duda al perder al jugador
    private float cronometroPerdido = 0f;
    
    [Header("Memoria Global")]
    public Transform puntoMeta;
    public Transform puntoObjetivo;

    public EstadoIA estadoActual { get; private set; }
    private GestorSensores sensores;
    private IAMovimiento movimiento;

    // Memoria que usarán los cartuchos
    public bool objetivoDetectado = false; 
    public Vector3 ultimaPosJugador;

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();

        if(patrulla) patrulla.Configurar(this, movimiento);
        if(persecucion) persecucion.Configurar(this, movimiento);
        if(emboscada) emboscada.Configurar(this, movimiento);
        if(busqueda) busqueda.Configurar(this, movimiento);                       // <-- NUEVO
        if(exploracion) exploracion.Configurar(this, movimiento);                 // <-- NUEVO
        if(comprobandoObjetivo) comprobandoObjetivo.Configurar(this, movimiento); // <-- NUEVO
    }

    void OnEnable()
    {
        sensores.OnJugadorDetectado += AlDetectar;
        sensores.OnJugadorPerdido += AlPerder;
    }

    void OnDisable()
    {
        sensores.OnJugadorDetectado -= AlDetectar;
        sensores.OnJugadorPerdido -= AlPerder;
    }

    void Start()
    {
        CambiarEstado(patrulla); // Siempre empezamos patrullando
    }

    void Update()
{
    if (estadoActual == null) return;

    // --- TRANSICIONES SEGÚN EL ESTADO ACTUAL ---
    // 1. Si estamos patrullando y roban el botín, vamos a emboscada
    if (estadoActual == patrulla && RecogerObjetivo.tieneElBotin)
    {
        CambiarEstado(emboscada);
    }

    else if (estadoActual == persecucion)
    {
        // Si perdemos al jugador, esperamos un tiempo antes de buscar
        if (!objetivoDetectado)
        {
            cronometroPerdido += Time.deltaTime;
            if (cronometroPerdido >= tiempoPerdidoParaBuscar)
            {
                // Decisión: ¿Emboscada o Búsqueda?
                if (RecogerObjetivo.tieneElBotin) CambiarEstado(emboscada);
                else CambiarEstado(busqueda);
            }
        }
        else cronometroPerdido = 0f;
    }
    
    else if (estadoActual == busqueda)
    {
        // Si termina de buscar en la última posición conocida...
        if (movimiento.HaLlegadoAlDestino())
        {
            CambiarEstado(exploracion);
        }
    }


    else if (estadoActual == exploracion)
    {
        // 3. Y cuando termine de dar vueltas explorando, ENTONCES va a por el botín
        if (((EstadoExploracion)exploracion).exploracionTerminada)
        {
            CambiarEstado(comprobandoObjetivo);
        }
    }
    
    else if (estadoActual == comprobandoObjetivo)
    {
        // El estado que fallaba: comprobamos si el botín sigue ahí
        float distAlBotin = Vector3.Distance(transform.position, puntoObjetivo.position);
        
        // Si llega físicamente o lo ve de cerca (ej: 4 metros)
        if (movimiento.HaLlegadoAlDestino() || distAlBotin < 4.0f)
        {
            CambiarEstado(patrulla);
        }
    }
}

    // --- RESPUESTA A LOS EVENTOS DE LOS SENSORES ---
    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador = pos;
        CambiarEstado(persecucion); // Cambio inmediato de cartucho
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }

    // --- EL MOTOR QUE CAMBIA LOS ESTADOS ---
    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;
        
        if (estadoActual != null) estadoActual.AlSalir(); // Apaga el viejo
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar(); // Enciende el nuevo

        Debug.Log("<color=yellow>CEREBRO: Cambiando al estado -> " + nuevoEstado.GetType().Name + "</color>");
    }
}