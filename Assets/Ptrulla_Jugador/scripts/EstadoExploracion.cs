using UnityEngine;

public class EstadoExploracion : EstadoIA
{
    [Header("Configuración Búsqueda")]
    public float radioExploracion = 15f; 
    public int puntosAExplorar = 3;     
    public float tiempoMaximoBuscandoUnPunto = 10f;

    [HideInInspector] public bool exploracionTerminada = false;

    private int puntosExploradosActuales = 0;
    private float tiempoEnExploracionActual = 0f;

    public override void AlEntrar()
    {
        base.AlEntrar();
        puntosExploradosActuales = 0;
        exploracionTerminada=false;
        GenerarNuevoPunto(); // Generamos el primer punto nada más empezar
    }

    void Update()
    {
        tiempoEnExploracionActual += Time.deltaTime;
        
        bool haLlegado = movimiento.HaLlegadoAlDestino();
        bool seAcaboElTiempo = (tiempoEnExploracionActual >= tiempoMaximoBuscandoUnPunto);

        if (haLlegado || seAcaboElTiempo)
        {
                      
            puntosExploradosActuales++;

            if (puntosExploradosActuales >= puntosAExplorar)
            {
                // Ya he mirado en 3 sitios distintos y no está. Me rindo y voy a ver el botín.
                exploracionTerminada = true;            }
            else
            {
                // Aún me quedan sitios por mirar
                GenerarNuevoPunto();
            }
        }
    }

    private void GenerarNuevoPunto()
    {
        Vector3 puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(cerebro.ultimaPosJugador, radioExploracion);
        movimiento.MoverA(puntoExploracionActual, movimiento.velocidadPatrulla);
        tiempoEnExploracionActual = 0f; // Reseteamos el cronómetro de atascos
    }
}