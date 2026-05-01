using UnityEngine;

public class EstadoExploracion : EstadoIA
{
    [Header("Configuración Búsqueda")]
    public float radioExploracion = 10f; 
    public int puntosAExplorarPorZona = 5;     
    public float tiempoMaximoBuscandoUnPunto = 10f;

    [HideInInspector] public bool exploracionTerminada = false;

    private int indiceZonaActual = -1;

    public override void AlEntrar()
    {
        base.AlEntrar();
        indiceZonaActual = -1; 
        exploracionTerminada = false;
        // --- Ordenar mi lista asignada por cercanía a MIS PIES ---
        if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 1)
        {
            // Ordenamos los puntos de la ruta desde el más cercano al guardia hasta el más lejano
            cerebro.rutaExploracion.Sort((a, b) => 
                CalcularDistanciaCaminando(transform.position, a).CompareTo(Vector3.Distance(transform.position, b))
            );
        }

        IrAlSiguientePunto(); 
    }

    void Update()
    {
        if (exploracionTerminada) return;

        // Si hemos llegado a la baldosa exacta...
        if (movimiento.HaLlegadoAlDestino())
        {
            indiceZonaActual++;
            
            // Si nos quedan puntos en la lista que nos dio el jefe, vamos al siguiente
            if (cerebro.rutaExploracion != null && indiceZonaActual < cerebro.rutaExploracion.Count)
            {
                IrAlSiguientePunto();
            }
            else // Si ya no hay más puntos, terminamos el barrido
            {
                Debug.Log($"[{gameObject.name}] Zona peinada. Barajando los puntos para seguir buscando...");
                
                // Mezclamos la lista de puntos aleatoriamente (Algoritmo Fisher-Yates)
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

                // Volvemos a empezar a caminar desde el nuevo punto 0, SIN reiniciar el estado
                indiceZonaActual = 0;
                IrAlSiguientePunto();
            }
        }
    }


    private void IrAlSiguientePunto()
    {
        if (indiceZonaActual == -1)
        {
            // FASE 1: Corremos a toda leche al punto exacto donde vimos al ladrón por última vez
            movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadPersecucion);
        }

        else if (cerebro.rutaExploracion != null && cerebro.rutaExploracion.Count > 0)
        {   
            movimiento.MoverA(cerebro.rutaExploracion[indiceZonaActual], movimiento.velocidadExploracion);
        }
        else
        {
            // Plan B por si le mandan explorar sin darle lista de puntos: 
            // Va a la última posición conocida y termina.
            movimiento.MoverA(cerebro.ultimaPosJugador, movimiento.velocidadExploracion);
            if (movimiento.HaLlegadoAlDestino()) exploracionTerminada = true;
        }
    }

    private float CalcularDistanciaCaminando(Vector3 origen, Vector3 destino)
    {
        UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
        
        // Si logra trazar la ruta por el suelo...
        if (UnityEngine.AI.NavMesh.CalculatePath(origen, destino, UnityEngine.AI.NavMesh.AllAreas, path))
        {
            // Ojo a los caminos cortados
            if (path.status == UnityEngine.AI.NavMeshPathStatus.PathPartial) return 9999f;

            float distanciaTotal = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distanciaTotal += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distanciaTotal;
        }
        
        return 9999f; // Si es inalcanzable, lo mandamos al final de la lista
    }

}