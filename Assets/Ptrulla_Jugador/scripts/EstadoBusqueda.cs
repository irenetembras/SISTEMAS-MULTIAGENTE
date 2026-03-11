using UnityEngine;

public class EstadoBusqueda : EstadoIA
{
    public override void AlEntrar()
    {
        base.AlEntrar();
        // Nada más entrar en este estado, le decimos a las piernas que corran hacia allí
        movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
    }

    void Update()
    {
        // Si ya hemos llegado al sitio donde lo vimos por última vez...
        if (movimiento.HaLlegadoAlDestino())
        {
            // ...sacamos este cartucho y metemos el de Exploración
            cerebro.CambiarEstado(cerebro.exploracion); 
        }
    }
}