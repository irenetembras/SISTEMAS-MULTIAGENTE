using UnityEngine;

public class EstadoEmboscada : EstadoIA
{
    void Update()
    {
        if (cerebro.puntoMeta != null)
        {
            movimiento.MoverA(cerebro.puntoMeta.position, movimiento.velocidadPersecucion);
        }
    }
}