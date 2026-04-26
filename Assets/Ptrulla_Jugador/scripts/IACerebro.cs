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

        // 1. ACTUALIZAR MEMORIA VISUAL
        if (objetivoDetectado && sensores.TransformJugador != null)
        {
            ultimaPosJugador = sensores.TransformJugador.position;
        }

        // 2. EL PUENTE ENTRE LA MENTE (Social) Y EL CUERPO (FSM)
        // El cerebro traduce el Rol asignado a un estado físico real.
        EjecutarRolTactico();

        // 3. DELEGAR EL TRABAJO NORMAL A LA MÁQUINA DE ESTADOS (Mover las piernas)
        fsm.ActualizarMaquina();
    }

    private void EjecutarRolTactico()
    {
        // Dependiendo de lo que diga el GestorSocial, forzamos un estado físico u otro
        switch (capaSocial.miRolAsignado)
        {
            case RolTactico.PatrullaNormal:
            case RolTactico.PatrullaSectorAdyacente:
                if (fsm.estadoActual != fsm.patrulla) fsm.CambiarEstado(fsm.patrulla);
                break;

            case RolTactico.BloqueoSalida:
                if (fsm.estadoActual != fsm.emboscada) fsm.CambiarEstado(fsm.emboscada);
                break;

            case RolTactico.PersecucionActiva:
                if (fsm.estadoActual != fsm.persecucion) fsm.CambiarEstado(fsm.persecucion);
                break;

            case RolTactico.ExplorarSectorSospechoso:
                // Si me mandan a investigar, uso mis estados de búsqueda/exploración
                if (fsm.estadoActual != fsm.busqueda && fsm.estadoActual != fsm.exploracion) 
                {
                    fsm.CambiarEstado(fsm.busqueda);
                }
                break;
        }
    }

    // --- RESPUESTA INMEDIATA A SENSORES ---
    private void AlDetectar(Vector3 pos)
    {
        objetivoDetectado = true;
        ultimaPosJugador = pos;

        // REFLEJO: Si veo al jugador con mis propios ojos, por instinto le persigo
        if (fsm.estadoActual != fsm.persecucion)
        {
            fsm.CambiarEstado(fsm.persecucion); 
        }

        // AVISO A LA MENTE: Le digo a mi Gestor Social que tome el mando y avise por radio
        // (Nota: Crearemos esta función en la Fase 3 dentro de GestorSocial)
        capaSocial.AsumirMandoYSubastar(pos); 
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }
}