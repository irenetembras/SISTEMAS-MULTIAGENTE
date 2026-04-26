using UnityEngine;
using System.Collections.Generic; // Necesario para usar List<>

// ==========================================
// 1. DICCIONARIO TÁCTICO (NUEVO)
// ==========================================

// Los estados de la FSM Superior (GestorSocial)
public enum EstadoTactico
{
    Libre,              // Patrullando por su cuenta, escuchando la radio.
    Comandante,         // Ha visto al jugador, divide tareas y gestiona la subasta.
    Subordinado         // Ha aceptado un contrato y cumple un rol bajo las órdenes del comandante.
}

// Las fases del planificador global (Solo las usa el Comandante)
public enum FaseAlerta
{
    Tranquilidad,       // Todo normal.
    ContactoVisual,     // Jugador a la vista -> Emboscadas y Persecuciones.
    BusquedaActiva,     // Recién perdido -> Buscar en el sector donde desapareció.
    Contencion,
    Reorganizacion      // No aparece -> Repartir patrullas por los sectores del mapa.
}

// Los Roles que se envían en las subastas (El "Qué" hacer)
public enum RolTactico
{
    PatrullaNormal,             // Ruta estática original (por defecto).
    PersecucionActiva,          // Ir directamente a por el jugador.
    BloqueoSalida,              // Ir a la sala del botín / punto de extracción a emboscar.
    ExplorarSectorSospechoso,   // Revisar el punto exacto donde desapareció.
    PatrullaSectorAdyacente     // Hacer la ruta dinámica por un SectorTactico.
}

// ==========================================
// 2. EL PAQUETE JSON DE DATOS (NUEVO)
// ==========================================
// Esta es la "carta" real que va dentro del sobre FIPA.
// Al ponerle [System.Serializable], Unity nos permite convertirlo a texto JSON y viceversa.
[System.Serializable]
public class DatosContrato
{
    public RolTactico rolOfertado;
    
    // Lista de puntos para cuando mandemos a alguien a patrullar una zona entera
    public List<Vector3> puntosDeRuta = new List<Vector3>(); 
    
    // Punto único para persecuciones o emboscadas
    public Vector3 coordenadaObjetivo; 
}


// ==========================================
// 3. LA ESTRUCTURA DEL MENSAJE FIPA (ACTUALIZADA)
// ==========================================

public enum PerformativaFIPA 
{
    INFORM,             // "Te informo de un dato" (Ej: He visto al jugador, Alarma)
    REQUEST,            // "Te pido que hagas algo"
    CFP,                // "Call For Proposal": Pido voluntarios (Subasta de Roles)
    PROPOSE,            // "Yo me ofrezco" (Ej: Mi distancia es de 5 metros)
    REFUSE,             // "Me niego / No puedo"
    ACCEPT_PROPOSAL,    // "Acepto tu oferta. Aquí tienes el JSON con tu Tarea"
    REJECT_PROPOSAL     // "Rechazo tu oferta. Sigue con lo tuyo"
}

[System.Serializable]
public class MensajeFIPA
{
    public PerformativaFIPA performativa; 
    public GameObject emisor;             
    public GameObject receptor;           
    
    // Ahora en vez de "x|y|z", aquí meteremos el string del JSON (DatosContrato) o simples distancias
    public string contenido;              

    // Constructor
    public MensajeFIPA(PerformativaFIPA perf, GameObject emi, GameObject rec, string cont)
    {
        performativa = perf;
        emisor = emi;
        receptor = rec;
        contenido = cont;
    }
}