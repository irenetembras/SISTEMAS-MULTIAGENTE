using UnityEngine;

public class EstadoExploracion : EstadoIA
{
    [HideInInspector] public bool exploracionTerminada = false;

    private int indiceZonaActual = -1;

    public override void AlEntrar()
    {
        base.AlEntrar();
        indiceZonaActual = -1;
        exploracionTerminada = false;

        // Ordenamos la ruta asignada por cercanía al guardia
        if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 1)
        {
            cerebro.rutaExploracion.Sort((a, b) =>
                UtilidadesNavMesh.CalcularDistancia(transform.position, a).CompareTo(
                UtilidadesNavMesh.CalcularDistancia(transform.position, b))
            );
        }

        IrAlSiguientePunto();
    }

    void Update()
    {
        if (exploracionTerminada) return;

        if (movimiento.HaLlegadoAlDestino())
        {
            indiceZonaActual++;

            if (cerebro.rutaExploracion != null && indiceZonaActual < cerebro.rutaExploracion.Count)
            {
                IrAlSiguientePunto();
            }
            else
            {
                Debug.Log($"[{gameObject.name}] Zona peinada. Barajando los puntos para seguir buscando...");

                // Mezclamos la lista aleatoriamente (Fisher-Yates) para no repetir el mismo orden
                if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 1)
                {
                    for (int i = 0; i < cerebro.rutaExploracion.Count; i++)
                    {
                        Vector3 temp = cerebro.rutaExploracion[i];
                        int randomIndex = Random.Range(i, cerebro.rutaExploracion.Count);
                        cerebro.rutaExploracion[i] = cerebro.rutaExploracion[randomIndex];
                        cerebro.rutaExploracion[randomIndex] = temp;
                    }
                }

                indiceZonaActual = 0;
                IrAlSiguientePunto();
            }
        }
    }

    private void IrAlSiguientePunto()
    {
        if (indiceZonaActual == -1)
        {
            // Primero corremos a la última posición conocida del ladrón
            movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
        }
        else if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 0)
        {
            movimiento.MoverA(cerebro.rutaExploracion[indiceZonaActual], movimiento.velocidadExploracion);
        }
        else
        {
            // Sin lista de puntos, vamos a la última posición conocida y terminamos
            movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadExploracion);
            if (movimiento.HaLlegadoAlDestino()) exploracionTerminada = true;
        }
    }
}
