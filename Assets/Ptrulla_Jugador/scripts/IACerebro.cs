using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
[RequireComponent(typeof(GestorSocial))]
public class IACerebro : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento")]
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
    public bool enMisionAsignada = false;
    public bool objetivoDetectado = false; 
    public Vector3 ultimaPosJugador;


    private GestorSocial capaSocial;

    void Awake() 
    {
        // 1. Inicializamos las referencias a los otros scripts del guardia
        sensores = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();
        capaSocial = GetComponent<GestorSocial>(); 

        // 2. Configuramos los cartuchos de estado
        if(patrulla) patrulla.Configurar(this, movimiento);
        if(persecucion) persecucion.Configurar(this, movimiento);
        if(emboscada) emboscada.Configurar(this, movimiento);
        if(busqueda) busqueda.Configurar(this, movimiento); 
        if(exploracion) exploracion.Configurar(this, movimiento);
        if(comprobandoObjetivo) comprobandoObjetivo.Configurar(this, movimiento); 
    }

    void OnEnable()
    {
        sensores.OnJugadorDetectado += AlDetectar;
        sensores.OnJugadorPerdido += AlPerder;
    }
    void OnDisable()
    {
        if (sensores != null)
        {
            sensores.OnJugadorDetectado -= AlDetectar;
            sensores.OnJugadorPerdido -= AlPerder;
        }
    }

    void Start()
    {
        CambiarEstado(patrulla); // Siempre empezamos patrullando
    }

    void Update()
    {
        if (estadoActual == null) return;

        // ACTUALIZACIÓN DE CAPA SOCIAL: Decimos si estamos libres para ayudar a otros
        // Solo estamos disponibles si no estamos persiguiendo ni en una emboscada
        capaSocial.estaDisponible = (estadoActual != persecucion && estadoActual != emboscada);

        if (objetivoDetectado && sensores.TransformJugador != null)
        {
            ultimaPosJugador = sensores.TransformJugador.position;
        }

        // 2.CAPA SOCIAL (Prioridad de comunicación)
        // Solo aceptamos órdenes si no estamos ya en plena persecución (instinto primario)
        if (capaSocial.tieneNuevaOrden && estadoActual != persecucion) 
        {
            ultimaPosJugador = movimiento.ObtenerPuntoAleatorioCercano(capaSocial.coordenadaOrdenada, 4f);
            enMisionAsignada = true;
            capaSocial.tieneNuevaOrden = false; 
            CambiarEstado(busqueda);
            return; // Salimos del Update este frame para empezar el nuevo estado limpios
        }

        // --- TRANSICIONES SEGÚN EL ESTADO ACTUAL (EL CÓDIGO DE TU COMPAÑERO) ---
        if (estadoActual == patrulla && RecogerObjetivo.tieneElBotin)
        {
            CambiarEstado(emboscada);
        }
        else if (estadoActual == persecucion)
        {
            if (!objetivoDetectado)
            {
                cronometroPerdido += Time.deltaTime;
                if (cronometroPerdido >= tiempoPerdidoParaBuscar)
                {
                    if (RecogerObjetivo.tieneElBotin) CambiarEstado(emboscada);
                    else CambiarEstado(busqueda);
                }
            }
            else cronometroPerdido = 0f;
        }
        else if (estadoActual == busqueda)
        {
            if (((EstadoBusqueda)busqueda).busquedaTerminada)
            {
                CambiarEstado(exploracion);
            }
        }
        else if (estadoActual == exploracion)
        {
            // Ojo aquí: Si en tu script exploracion no tienes "exploracionTerminada", 
            // asegúrate de que el código de tu compañero coincide con tus scripts de estado.
            if (((EstadoExploracion)exploracion).exploracionTerminada)
            {
                CambiarEstado(comprobandoObjetivo);
            }
        }
        else if (estadoActual == comprobandoObjetivo)
        {
            float distAlBotin = Vector3.Distance(transform.position, puntoObjetivo.position);
            if (movimiento.HaLlegadoAlDestino() || distAlBotin < 4.0f)
            {
                CambiarEstado(patrulla);
            }
        }
    }

    // --- RESPUESTA A LOS EVENTOS DE LOS SENSORES (FUSIONADO) ---
    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador = pos;

        // SOLO abro subasta si yo soy el primero en verlo y no estaba ya ocupado
        if (estadoActual != persecucion && !enMisionAsignada)
        {
            capaSocial.IniciarSubasta(pos);
        }

        // Pase lo que pase, si lo tengo delante, ¡le persigo!
        CambiarEstado(persecucion); 
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }

    // --- EL MOTOR QUE CAMBIA LOS ESTADOS ---
    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;

        // Si vuelvo a la rutina, reseteo mi rol
        if (nuevoEstado == patrulla) enMisionAsignada = false; 

        if (estadoActual != null) estadoActual.AlSalir(); 
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar(); 
        Debug.Log("<color=yellow>CEREBRO: Cambiando al estado -> " + nuevoEstado.GetType().Name + "</color>");
    }

}
