using UnityEngine;

public class EstadoEmboscada : EstadoIA
{
    public override void AlEntrar()
    {
        base.AlEntrar();

        if (cerebro.puntoMeta != null)
        {
            // Le damos la orden de ir a la meta una sola vez.
            // Usamos velocidad de persecución porque el ladrón ya tiene el botín.
            movimiento.MoverA(cerebro.puntoMeta.position, movimiento.velocidadPersecucion);
        }
    }

}