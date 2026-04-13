using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
[RequireComponent(typeof(GestorSocial))]
[RequireComponent(typeof(MaquinaDeEstados))] // <-- OBLIGAMOS A TENER LA FSM
public class IACerebro : MonoBehaviour
{
    [Header("Memoria Global")]
    public Transform puntoMeta;
    public Transform puntoObjetivo;
    public bool enMisionAsignada = false;
    public bool objetivoDetectado = false; 
    public Vector3 ultimaPosJugador;

    private GestorSensores sensores;
    private IAMovimiento movimiento;
    
    // Las referencias a los otros 2 bloques de la arquitectura
    public GestorSocial capaSocial { get; private set; }
    public MaquinaDeEstados fsm { get; private set; } 

    void Awake() 
    {
        sensores = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();
        capaSocial = GetComponent<GestorSocial>(); 
        fsm = GetComponent<MaquinaDeEstados>(); // Pillamos la FSM

        // Le damos los mandos a la Máquina de Estados para que cargue los cartuchos
        fsm.Inicializar(this, movimiento);
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

    void Update()
    {
        if (fsm.estadoActual == null) return;

        // --- ¡LA REACCIÓN A LA ALARMA GENERAL! ---
        if (capaSocial.alarmaGeneralActivada && fsm.estadoActual != fsm.emboscada)
        {
            fsm.CambiarEstado(fsm.emboscada);
            return; // Cortamos el Update aquí para que no haga nada más y corra a la salida
        }

        // 1. GESTIÓN DE MEMORIA Y ESTADO SOCIAL
        capaSocial.estaDisponible = (fsm.estadoActual != fsm.persecucion && fsm.estadoActual != fsm.emboscada);

        if (objetivoDetectado && sensores.TransformJugador != null)
        {
            ultimaPosJugador = sensores.TransformJugador.position;
        }

        // 2. ÓRDENES DE RADIO (Prioridad Social)
        if (capaSocial.tieneNuevaOrden && fsm.estadoActual != fsm.persecucion) 
        {
            ultimaPosJugador = movimiento.ObtenerPuntoAleatorioCercano(capaSocial.coordenadaOrdenada, 4f);
            enMisionAsignada = true;
            capaSocial.tieneNuevaOrden = false; 
            fsm.CambiarEstado(fsm.busqueda);
            return; 
        }

        // 3. DELEGAR EL TRABAJO NORMAL A LA MÁQUINA DE ESTADOS
        fsm.ActualizarMaquina();
    }

    // --- RESPUESTA A SENSORES ---
    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador = pos;

        // Avisar a la radio
        if (fsm.estadoActual != fsm.persecucion && !enMisionAsignada)
        {
            capaSocial.IniciarSubasta(pos);
        }

        // Obligar a la máquina a perseguir
        fsm.CambiarEstado(fsm.persecucion); 
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }
}