using UnityEngine;

public class EstadoBusqueda : EstadoIA
{
    [Header("Ajustes")]
    public float tiempoMirandoElSitio = 2.0f; 
    [HideInInspector] public bool busquedaTerminada = false;

    private bool yaHaLlegado = false;
    private float cronometro = 0f;

    public override void AlEntrar()
    {
        base.AlEntrar();
        busquedaTerminada = false;
        yaHaLlegado = false;
        cronometro = 0f;
        movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
    }

    void Update()
    {
        if (!yaHaLlegado && movimiento.HaLlegadoAlDestino())
        {
            yaHaLlegado = true;
            movimiento.Detener(); 
        }

        if (yaHaLlegado)
        {
            cronometro += Time.deltaTime;
            if (cronometro >= tiempoMirandoElSitio)
            {
                busquedaTerminada = true; 
            }
        }
    }
}