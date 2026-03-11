using UnityEngine;

public class EstadoPatrulla : EstadoIA
{
    void Update()
    {
        movimiento.Patrullar();

        // Regla: Si me entero de que han robado, meto el cartucho de emboscada
        if (RecogerObjetivo.tieneElBotin)
        {
            cerebro.CambiarEstado(cerebro.emboscada);
        }
    }
}
