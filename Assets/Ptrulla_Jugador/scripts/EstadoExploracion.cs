using UnityEngine;

public class EstadoExploracion : EstadoIA
{
    [Header("Configuración Búsqueda")]
    public float radioExploracion = 15f; 
    public int puntosAExplorar = 3;     
    public float tiempoMaximoBuscandoUnPunto = 10f;

    private int puntosExploradosActuales = 0;
    private Vector3 puntoExploracionActual;
    private float tiempoEnExploracionActual = 0f;

    public override void AlEntrar()
    {
        base.AlEntrar();
        puntosExploradosActuales = 0;
        GenerarNuevoPunto(); // Generamos el primer punto nada más empezar
    }

    void Update()
    {
        tiempoEnExploracionActual += Time.deltaTime;
        
        bool haLlegado = movimiento.HaLlegadoAlDestino();
        bool seAcaboElTiempo = (tiempoEnExploracionActual >= tiempoMaximoBuscandoUnPunto);

        if (haLlegado || seAcaboElTiempo)
        {
            if (seAcaboElTiempo) movimiento.Detener(); // Failsafe por si se atasca
            
            puntosExploradosActuales++;

            if (puntosExploradosActuales >= puntosAExplorar)
            {
                // Ya he mirado en 3 sitios distintos y no está. Me rindo y voy a ver el botín.
                cerebro.CambiarEstado(cerebro.comprobandoObjetivo);
            }
            else
            {
                // Aún me quedan sitios por mirar
                GenerarNuevoPunto();
            }
        }
    }

    private void GenerarNuevoPunto()
    {
        puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(cerebro.ultimaPosJugador, radioExploracion);
        movimiento.MoverA(puntoExploracionActual, movimiento.velocidadPatrulla);
        tiempoEnExploracionActual = 0f; // Reseteamos el cronómetro de atascos
    }
}