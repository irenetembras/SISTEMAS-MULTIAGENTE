using UnityEngine;

public class EstadoPersecucion : EstadoIA
{
    public float tiempoRecordarPerseguir = 0.5f;
    private float tiempoDesdePerdido = 0f;
    
    // Guardamos una referencia directa al jugador para este estado
    private Transform jugador; 

    public override void Configurar(IACerebro c, IAMovimiento m)
    {
        base.Configurar(c, m);
        // Buscamos al jugador una sola vez al configurar
        jugador = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void AlEntrar()
    {
        base.AlEntrar();
        tiempoDesdePerdido = 0f; 
    }

    void Update()
    {
        // EL TRUCO: Le mandamos tu posición REAL actual. 
        // Así, si le haces la 13-60, girará en redondo al instante persiguiendo tu espalda.
        if (jugador != null)
        {
            movimiento.Perseguir(jugador.position);
        }

        if (!cerebro.objetivoDetectado)
        {
            tiempoDesdePerdido += Time.deltaTime;
            
            if (tiempoDesdePerdido >= tiempoRecordarPerseguir)
            {
                if (RecogerObjetivo.tieneElBotin)
                    cerebro.CambiarEstado(cerebro.emboscada);
                else
                    cerebro.CambiarEstado(cerebro.busqueda); 
            }
        }
        else
        {
            tiempoDesdePerdido = 0f;
        }
    }
}