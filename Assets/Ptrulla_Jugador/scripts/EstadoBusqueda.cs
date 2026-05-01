using UnityEngine;

public class EstadoBusqueda : EstadoIA
{
    [Header("Ajustes de Búsqueda Local")]
    public float radioBusqueda = 5f; 
    public int puntosAleatoriosABuscar = 3;
    public float tiempoPausaEnPunto = 1.0f; // Breve pausa al llegar para "mirar" a los lados

    [HideInInspector] public bool busquedaTerminada = false;
    
    private int puntosVisitados = 0;
    private bool esperandoEnPunto = false;
    private float cronometro = 0f;

    public override void AlEntrar()
    {
        base.AlEntrar();
        busquedaTerminada = false;
        puntosVisitados = 0;
        esperandoEnPunto = false;
        cronometro = 0f;
        
        // 1. Primero, corremos a la coordenada exacta donde se esfumó
        movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
    }

    void Update()
    {
        if (busquedaTerminada) return;

        if (movimiento.HaLlegadoAlDestino())
        {
            if (!esperandoEnPunto)
            {
                // Acabamos de llegar a un punto. Nos paramos a mirar.
                esperandoEnPunto = true;
                cronometro = 0f;
                movimiento.Detener();
            }
            else
            {
                cronometro += Time.deltaTime;
                if (cronometro >= tiempoPausaEnPunto)
                {
                    // Terminamos de mirar. ¿Seguimos buscando o nos rendimos?
                    puntosVisitados++;
                    
                    if (puntosVisitados <= puntosAleatoriosABuscar)
                    {
                        // Pedimos a tu función un punto aleatorio cercano en el NavMesh
                        Vector3 nuevoPunto = movimiento.ObtenerPuntoAleatorioCercano(cerebro.ultimaPosJugador, radioBusqueda);
                        movimiento.MoverA(nuevoPunto, movimiento.velocidadExploracion);
                        esperandoEnPunto = false; // Volvemos a caminar
                    }
                    else
                    {
                        // Ya hemos mirado en varios sitios y no está. Búsqueda local terminada.
                        busquedaTerminada = true;
                    }
                }
            }
        }
    }
}