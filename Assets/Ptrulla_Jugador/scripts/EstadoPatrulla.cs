using UnityEngine;

public class EstadoPatrulla : EstadoIA
{
    void Update()
    {
        movimiento.Patrullar();
    }
}
