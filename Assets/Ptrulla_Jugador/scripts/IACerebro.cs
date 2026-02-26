using UnityEngine;

[RequireComponent(typeof(IASensores))]
[RequireComponent(typeof(IAMovimiento))]
public class IACerebro : MonoBehaviour
{
    [Header("Tiempos y Zonas")]
    public float tiempoRecordarPerseguir = 0.5f;
    public float radioExploracion = 5f; // Cuánto se aleja para buscar
    public int puntosAExplorar = 3;     // Cuántos sitios mira antes de rendirse

    [Header("Puntos Estratégicos")]
    public Transform puntoObjetivo; // Dónde está el tesoro
    public Transform puntoMeta;     // Por dónde escapa el jugador (Emboscada)
    public float distanciaParaVerBotin = 5f; // A qué distancia el guardia se da cuenta de que falta el botín

    private IASensores sensores;
    private IAMovimiento movimiento;

    // TODOS LOS ESTADOS QUE HAS PEDIDO
    private enum Estado { 
        PATRULLANDO, 
        PERSIGUIENDO, 
        BUSCANDO_ULTIMA_POS, 
        EXPLORANDO, 
        COMPROBANDO_OBJETIVO, 
        EMBOSCADA 
    }
    
    private Estado estadoActual = Estado.PATRULLANDO;
    
    // Memoria del guardia
    private float tiempoDesdePerdido = 0f;
    private Vector3 ultimaPosicionConocida;
    private int puntosExploradosActuales = 0;
    private Vector3 puntoExploracionActual;

    void Awake()
    {
        sensores = GetComponent<IASensores>();
        movimiento = GetComponent<IAMovimiento>();
    }

    void Update()
    {
        // 1. SENSE
        bool objetivoDetectado = sensores.JugadorDetectado;
        
        // --- AQUÍ ESTÁ EL CAMBIO MÁGICO ---
        // Leemos directamente la variable estática que creó tu compañera
        bool botinRobado = RecogerObjetivo.tieneElBotin;

        // ¿Estoy lo bastante cerca del pedestal para darme cuenta de que no está?
        bool estoyCercaDelBotin = botinRobado && (Vector3.Distance(transform.position, puntoObjetivo.position) < distanciaParaVerBotin);

        // 2. THINK (La Máquina de Estados)
        // REGLA SUPREMA: El sistema de Alerta
        if (botinRobado && (objetivoDetectado || estoyCercaDelBotin))
        {
            estadoActual = Estado.EMBOSCADA;
        }
        else if (objetivoDetectado && estadoActual != Estado.EMBOSCADA)
        {
            estadoActual = Estado.PERSIGUIENDO;
            tiempoDesdePerdido = 0f;
            ultimaPosicionConocida = sensores.TransformJugador.position; 
        }

        // LÓGICA DE CADA ESTADO CUANDO NO VEO AL JUGADOR
        switch (estadoActual)
        {
            case Estado.PERSIGUIENDO:
                if (!objetivoDetectado)
                {
                    tiempoDesdePerdido += Time.deltaTime;
                    if (tiempoDesdePerdido >= tiempoRecordarPerseguir)
                    {
                        // Lo perdí. Voy corriendo a donde lo vi por última vez.
                        estadoActual = Estado.BUSCANDO_ULTIMA_POS;
                    }
                }
                break;

            case Estado.BUSCANDO_ULTIMA_POS:
                if (movimiento.HaLlegadoAlDestino())
                {
                    // Llegué y no está. Empiezo a explorar la zona.
                    estadoActual = Estado.EXPLORANDO;
                    puntosExploradosActuales = 0;
                    puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(ultimaPosicionConocida, radioExploracion);
                }
                break;

            case Estado.EXPLORANDO:
                if (movimiento.HaLlegadoAlDestino())
                {
                    puntosExploradosActuales++;
                    if (puntosExploradosActuales >= puntosAExplorar)
                    {
                        // Ya miré por aquí y nada. Voy a comprobar si el botín está a salvo.
                        estadoActual = Estado.COMPROBANDO_OBJETIVO;
                    }
                    else
                    {
                        // Busco otro punto cercano
                        puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(ultimaPosicionConocida, radioExploracion);
                    }
                }
                break;

            case Estado.COMPROBANDO_OBJETIVO:
                if (movimiento.HaLlegadoAlDestino())
                {
                    // Si llego aquí, el botín sigue ahí (si no, habría saltado la alerta suprema arriba)
                    // Así que falsa alarma, vuelvo a patrullar.
                    estadoActual = Estado.PATRULLANDO;
                    movimiento.IrAlPuntoMasCercano();
                }
                break;
        }

        // 3. ACT (Ejecutar las órdenes según el estado)
        switch (estadoActual)
        {
            case Estado.PATRULLANDO:
                movimiento.Patrullar();
                break;

            case Estado.PERSIGUIENDO:
                if (sensores.TransformJugador != null)
                    movimiento.Perseguir(sensores.TransformJugador.position);
                break;

            case Estado.BUSCANDO_ULTIMA_POS:
                movimiento.MoverA(ultimaPosicionConocida, movimiento.velocidadPersecucion);
                break;

            case Estado.EXPLORANDO:
                movimiento.MoverA(puntoExploracionActual, movimiento.velocidadPatrulla); // Explora caminando
                break;

            case Estado.COMPROBANDO_OBJETIVO:
                movimiento.MoverA(puntoObjetivo.position, movimiento.velocidadPersecucion); // Va rápido a mirar
                break;

            case Estado.EMBOSCADA:
                if (puntoMeta != null)
                {
                    // Corre a la salida a esperarte
                    movimiento.MoverA(puntoMeta.position, movimiento.velocidadPersecucion);
                }
                break;
        }
    }
}