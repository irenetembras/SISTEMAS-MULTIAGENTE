using UnityEngine;

public class EstadoEmboscada : EstadoIA
{
    public override void AlEntrar()
    {
        base.AlEntrar();

        // Ya no hace matemáticas raras. Va directo a la coordenada de la trampa
        // que le chivó el Vigía por la radio.
        // ---> AÑADE ESTE CHIVATO AQUÍ <---
        Debug.Log($"[PIERNAS] {gameObject.name} entra en EMBOSCADA. Corriendo a: {cerebro.coordenadaTactica}");
        movimiento.MoverA(cerebro.coordenadaTactica, movimiento.velocidadPersecucion);
    }

    void Update()
    {
    // Si no vemos al jugador, nos quedamos en el punto de trampa asignado
    if (cerebro.coordenadaTactica != Vector3.zero)
        {
            movimiento.MoverA(cerebro.coordenadaTactica, movimiento.velocidadPersecucion);
        
            // Si estamos muy cerca del punto, nos encaramos hacia donde vendría el jugador
            if (Vector3.Distance(transform.position, cerebro.coordenadaTactica) < 1f)
            {
            // Mirar hacia la última posición conocida o hacia el centro del sector
                transform.LookAt(cerebro.ultimaPosJugador);
            }
        }
    }
}