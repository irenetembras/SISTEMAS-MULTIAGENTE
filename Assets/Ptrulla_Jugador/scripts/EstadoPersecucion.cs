using UnityEngine;

public class EstadoPersecucion : EstadoIA
{
    public float tiempoRecordarPerseguir = 0.5f;
    
    // Guardamos una referencia directa al jugador para este estado
    private Transform jugador; 

    public override void Configurar(IACerebro c, IAMovimiento m)
    {
        base.Configurar(c, m);
        // Buscamos al jugador una sola vez al configurar
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
    }

void Update()
    {
        // ACCIÓN FÍSICA PURA: 
        // Mientras estemos en este estado y el cerebro diga que lo detecta, corremos.
        if (jugador != null && cerebro.objetivoDetectado)
        {
            movimiento.Perseguir(jugador.position);
        }
    }
}