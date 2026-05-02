using UnityEngine;

public class EstadoEmboscada : EstadoIA
{
    public override void AlEntrar()
    {
        base.AlEntrar();
        Debug.Log($"[PIERNAS] {gameObject.name} entra en EMBOSCADA. Corriendo a: {cerebro.coordenadaTactica}");
        movimiento.MoverA(cerebro.coordenadaTactica, movimiento.velocidadPersecucion);
    }

    void Update()
    {
        if (cerebro.coordenadaTactica != Vector3.zero)
        {
            movimiento.MoverA(cerebro.coordenadaTactica, movimiento.velocidadPersecucion);

            // Al llegar al punto de trampa, miramos hacia donde vendría el jugador
            if (Vector3.Distance(transform.position, cerebro.coordenadaTactica) < 1f)
            {
                transform.LookAt(cerebro.ultimaPosJugador);
            }
        }
    }
}
