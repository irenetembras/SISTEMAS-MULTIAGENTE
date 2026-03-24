using UnityEngine;

public class EstadoComprobandoObjetivo : EstadoIA
{
    public float distanciaParaVerBotin = 3f;

    public override void AlEntrar()
    {
        base.AlEntrar();
        if (cerebro.puntoObjetivo != null)
        {
            // Corremos hacia la sala del tesoro
            movimiento.MoverA(cerebro.puntoObjetivo.position, movimiento.velocidadPersecucion);
        }
    }

}