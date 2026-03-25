using UnityEngine;

// 1. LAS INTENCIONES (Performatives FIPA)
// Esto define el "tono" o "propósito" del mensaje.
public enum PerformativaFIPA 
{
    INFORM,             // "Te informo de un dato" (Ej: He visto al jugador)
    REQUEST,            // "Te pido que hagas algo" (Ej: Ve a la puerta)
    CFP,                // "Call For Proposal": Pido voluntarios (Ej: ¿Quién está más cerca del tesoro?)
    PROPOSE,            // "Yo me ofrezco" (Ej: Yo estoy a 5 metros, voy yo)
    REFUSE,             // "Me niego / No puedo" (Ej: Estoy muy lejos, pasa de mí)
    ACCEPT_PROPOSAL,    // "Acepto tu oferta" (Ej: Vale, ve tú a la puerta)
    REJECT_PROPOSAL     // "Rechazo tu oferta" (Ej: No, mejor va el guardia 2 que está más cerca)
}

// 2. LA ESTRUCTURA DEL MENSAJE
// Fíjate que NO hereda de MonoBehaviour. Esto no se pone en ningún objeto de Unity.
// Es solo una estructura de datos (como un sobre de correos).
[System.Serializable]
public class MensajeFIPA
{
    public PerformativaFIPA performativa; // El propósito del mensaje
    public GameObject emisor;             // Quién envía el mensaje (para poder responderle)
    public GameObject receptor;           // Para quién es el mensaje (null si es un grito para todos)
    public string contenido;              // El texto o datos del mensaje (Ej: coordenadas)

    // Constructor: Una función cómoda para rellenar el sobre rápidamente al crearlo
    public MensajeFIPA(PerformativaFIPA perf, GameObject emi, GameObject rec, string cont)
    {
        performativa = perf;
        emisor = emi;
        receptor = rec;
        contenido = cont;
    }
} 