using UnityEngine;

[RequireComponent(typeof(IASensores))]
[RequireComponent(typeof(IAMovimiento))]
public class IACerebro : MonoBehaviour
{
    [Header("Tiempos y Zonas")]
    public float tiempoRecordarPerseguir = 0.5f;
    public float radioExploracion = 15f; 
    public int puntosAExplorar = 3;     

    // --- NUEVA VARIABLE ---
    public float tiempoMaximoBuscandoUnPunto = 10f; // Si tarda más de 10s en llegar, se rinde y pasa al siguiente
    // ----------------------

    [Header("Puntos Estratégicos")]
    public Transform puntoObjetivo; // Baldosa/Horno
    // botin se quitó, ya no hace falta
    public Transform puntoMeta;     
    public float distanciaParaVerBotin = 5f; 

    private IASensores sensores;
    private IAMovimiento movimiento;

    private enum Estado { 
        PATRULLANDO, 
        PERSIGUIENDO, 
        BUSCANDO_ULTIMA_POS, 
        EXPLORANDO, 
        COMPROBANDO_OBJETIVO, 
        EMBOSCADA 
    }
    
    private Estado estadoActual = Estado.PATRULLANDO;
    
    private float tiempoDesdePerdido = 0f;
    private Vector3 ultimaPosicionConocida;
    private int puntosExploradosActuales = 0;
    private Vector3 puntoExploracionActual;

    // --- NUEVA VARIABLE INTERNA ---
    private float tiempoEnExploracionActual = 0f;
    // ------------------------------

    void Awake()
    {
        sensores = GetComponent<IASensores>();
        movimiento = GetComponent<IAMovimiento>();
    }

    void Update()
    {
        // 1. SENSE
        bool objetivoDetectado = sensores.JugadorDetectado;
        bool botinRobado = RecogerObjetivo.tieneElBotin;
        bool estoyCercaDelBotin = botinRobado && (puntoObjetivo != null && Vector3.Distance(transform.position, puntoObjetivo.position) < distanciaParaVerBotin);

        // 2. THINK (La Máquina de Estados)
        
        // REGLA 1: Prioridad Absoluta -> Si te veo, te persigo (tengas el botín o no)
        if (objetivoDetectado)
        {
            estadoActual = Estado.PERSIGUIENDO;
            tiempoDesdePerdido = 0f;
            ultimaPosicionConocida = sensores.TransformJugador.position; 
        }
        // REGLA 2: Si NO te veo, pero sé que el botín ha sido robado...
        // (Lo sé porque pasé cerca del pedestal vacío, o porque ya estaba en la salida)
        else if (botinRobado && (estoyCercaDelBotin || estadoActual == Estado.EMBOSCADA || estadoActual == Estado.PERSIGUIENDO))
        {
            if (estadoActual == Estado.PERSIGUIENDO)
            {
                // Si te estaba persiguiendo y te escondes, espero un segundito...
                tiempoDesdePerdido += Time.deltaTime;
                if (tiempoDesdePerdido >= tiempoRecordarPerseguir)
                {
                    // ...y en vez de buscarte por la zona, voy directo a la salida a cortarte el paso
                    estadoActual = Estado.EMBOSCADA;
                }
            }
            else
            {
                // Si pasé por el pedestal vacío, voy directo a la salida
                estadoActual = Estado.EMBOSCADA;
            }
        }
        // REGLA 3: Comportamiento Normal (Si el botín sigue a salvo y no te veo)
        else
        {
            switch (estadoActual)
            {
                case Estado.PERSIGUIENDO:
                    tiempoDesdePerdido += Time.deltaTime;
                    if (tiempoDesdePerdido >= tiempoRecordarPerseguir)
                    {
                        estadoActual = Estado.BUSCANDO_ULTIMA_POS;
                    }
                    break;

                case Estado.BUSCANDO_ULTIMA_POS:
                    if (movimiento.HaLlegadoAlDestino())
                    {
                        estadoActual = Estado.EXPLORANDO;
                        puntosExploradosActuales = 0;
                        puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(ultimaPosicionConocida, radioExploracion);
                        tiempoEnExploracionActual = 0f;
                    }
                    break;

                case Estado.EXPLORANDO:
                    tiempoEnExploracionActual += Time.deltaTime;
                    bool haLlegadoPorPosicion = movimiento.HaLlegadoAlDestino();
                    bool seHaAcabadoElTiempo = (tiempoEnExploracionActual >= tiempoMaximoBuscandoUnPunto);

                    if (haLlegadoPorPosicion || seHaAcabadoElTiempo)
                    {
                        if (seHaAcabadoElTiempo) { movimiento.Detener(); }

                        puntosExploradosActuales++;
                        if (puntosExploradosActuales >= puntosAExplorar)
                        {
                            estadoActual = Estado.COMPROBANDO_OBJETIVO;
                        }
                        else
                        {
                            puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(ultimaPosicionConocida, radioExploracion);
                            tiempoEnExploracionActual = 0f;
                        }
                    }
                    break;

                case Estado.COMPROBANDO_OBJETIVO:
                    if (puntoObjetivo != null && (movimiento.HaLlegadoAlDestino() || Vector3.Distance(transform.position, puntoObjetivo.position) < 3f))
                    {
                        estadoActual = Estado.PATRULLANDO;
                        movimiento.IrAlPuntoMasCercano();
                    }
                    break;
            }
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
                movimiento.MoverA(puntoExploracionActual, movimiento.velocidadPatrulla); 
                break;

            case Estado.COMPROBANDO_OBJETIVO:
                if (puntoObjetivo != null)
                    movimiento.MoverA(puntoObjetivo.position, movimiento.velocidadPersecucion); 
                break;

            case Estado.EMBOSCADA:
                if (puntoMeta != null)
                    movimiento.MoverA(puntoMeta.position, movimiento.velocidadPersecucion);
                break;
        }
    }
    
    
}