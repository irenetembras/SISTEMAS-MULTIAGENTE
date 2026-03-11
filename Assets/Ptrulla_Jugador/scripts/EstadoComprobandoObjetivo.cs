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
        else
        {
            // Si por algún motivo no hay tesoro asignado, volvemos a patrullar por seguridad
            cerebro.CambiarEstado(cerebro.patrulla);
        }
    }

    void Update()
    {
        if (cerebro.puntoObjetivo == null) return;

        bool haLlegado = movimiento.HaLlegadoAlDestino();
        bool loVeDeLejos = Vector3.Distance(transform.position, cerebro.puntoObjetivo.position) < distanciaParaVerBotin;

        // Si llego, o si me acerco lo suficiente para verlo desde la puerta...
        if (haLlegado || loVeDeLejos)
        {
            // ...suspiro aliviado porque el botín sigue ahí, y vuelvo a mi patrulla normal
            movimiento.IrAlPuntoMasCercano();
            cerebro.CambiarEstado(cerebro.patrulla);
        }
    }
}