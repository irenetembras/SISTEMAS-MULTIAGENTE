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
    }
}