using UnityEngine;

public class EstadoExploracion : EstadoIA
{
    [Header("Configuración Búsqueda")]
    public float radioExploracion = 10f; 
    public int puntosAExplorarPorZona = 5;     
    public float tiempoMaximoBuscandoUnPunto = 10f;

    [HideInInspector] public bool exploracionTerminada = false;

    private int puntosExploradosEnZonaActual = 0;
    private float tiempoEnExploracionActual = 0f;
    private int indiceZonaActual = 0;

    public override void AlEntrar()
    {
        base.AlEntrar();
        puntosExploradosEnZonaActual = 0;
        indiceZonaActual = 0; // Empezamos por el primer punto de nuestra lista personal
        exploracionTerminada = false;
        GenerarNuevoPunto(); 
    }

    void Update()
    {
        tiempoEnExploracionActual += Time.deltaTime;
        
        bool haLlegado = movimiento.HaLlegadoAlDestino();
        bool seAcaboElTiempo = (tiempoEnExploracionActual >= tiempoMaximoBuscandoUnPunto);

        if (haLlegado || seAcaboElTiempo)
        {
            puntosExploradosEnZonaActual++;

            if (puntosExploradosEnZonaActual >= puntosAExplorarPorZona)
            {
                // ¡Hemos terminado de limpiar este Punto de Interés! 
                puntosExploradosEnZonaActual = 0; // Reseteamos el contador

                // Pasamos al siguiente Punto de Interés de la habitación
                if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 0)
                {
                    indiceZonaActual = (indiceZonaActual + 1) % cerebro.rutaExploracion.Count;
                    Debug.Log($"[{gameObject.name}] Zona limpia. Moviéndome al siguiente Punto de Interés.");
                }
            }
            
            GenerarNuevoPunto();
        }
    }

    private void GenerarNuevoPunto()
    {
        // Vemos cuál es el centro de la zona que nos toca limpiar ahora
        Vector3 centroDeZona = cerebro.ultimaPosJugador; // Por defecto
        if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 0)
        {
            centroDeZona = cerebro.rutaExploracion[indiceZonaActual];
        }

        // Generamos un punto aleatorio alrededor de ese centro
        Vector3 puntoExploracionActual = movimiento.ObtenerPuntoAleatorioCercano(centroDeZona, radioExploracion);
        movimiento.MoverA(puntoExploracionActual, movimiento.velocidadExploracion);
        tiempoEnExploracionActual = 0f; 
    }
}