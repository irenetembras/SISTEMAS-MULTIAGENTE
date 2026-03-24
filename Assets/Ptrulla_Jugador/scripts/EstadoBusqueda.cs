using UnityEngine;

public class EstadoBusqueda : EstadoIA
{
    public override void AlEntrar()
    {
        base.AlEntrar();
        // Nada más entrar en este estado, le decimos a las piernas que corran hacia allí
        movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
    }
}