using UnityEngine;
using UnityEngine.AI; // Necesario para el cálculo de rutas (NavMeshPath)

// Estado: el guardia ejecuta el rol asignado por el Comandante.
// ¡NUEVO!: Ahora atiende a la radio y responde a subastas nuevas si lo detectan en otro lado.
public class EstadoTactico_Subordinado : EstadoTacticoBase
{
    public override void AlEntrar()
    {
        base.AlEntrar();
    }

    public override void ProcesarMensaje(MensajeFIPA mensaje)
    {
        switch (mensaje.performativa)
        {
            case PerformativaFIPA.INFORM:
                ProcesarInform(mensaje);
                break;

            case PerformativaFIPA.ACCEPT_PROPOSAL:
                DatosContrato nuevaOrden = JsonUtility.FromJson<DatosContrato>(mensaje.contenido);
                cerebro.capaSocial.miRolAsignado = nuevaOrden.rolOfertado;
                cerebro.coordenadaTactica = nuevaOrden.coordenadaObjetivo;
                cerebro.ultimaPosJugador = nuevaOrden.coordenadaObjetivo; 
                
                if (nuevaOrden.puntosDeRuta != null && nuevaOrden.puntosDeRuta.Count > 0) {
                    cerebro.rutaExploracion = nuevaOrden.puntosDeRuta;
                }

                Debug.Log($"[SUBORDINADO {gameObject.name}] Cambio de orden recibido: {nuevaOrden.rolOfertado}");
                break;

            case PerformativaFIPA.REJECT_PROPOSAL:
                Debug.Log($"[SUBORDINADO {gameObject.name}] Escuadrón disuelto. Volviendo a patrulla.");
                cerebro.capaSocial.miRolAsignado = RolTactico.PatrullaNormal;
                fsmTactica.CambiarEstado(fsmTactica.libre);
                break;

            case PerformativaFIPA.CFP:
                // ¡EL ARREGLO ESTÁ AQUÍ! 
                // Ya no dicen "Estoy ocupado", ahora evalúan la nueva emergencia.
                ResponderCFP(mensaje);
                break;
        }
    }

    private void ProcesarInform(MensajeFIPA inform)
    {
        if (inform.contenido == "ALARMA_ROBO")
        {
            cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
            return;
        }

        if (inform.contenido.Contains('|'))
        {
            string[] p = inform.contenido.Split('|');
            Vector3 nuevoPunto = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );

            cerebro.coordenadaTactica = nuevoPunto;
            cerebro.ultimaPosJugador = nuevoPunto; 

            // Solo nos movemos al GPS si nuestra misión actual es perseguir
            if (cerebro.capaSocial.miRolAsignado == RolTactico.PersecucionActiva)
            {
                GetComponent<IAMovimiento>().MoverA(nuevoPunto, GetComponent<IAMovimiento>().velocidadPersecucion);
            }
        }
    }

    // Funciones copiadas del Guardia Libre para que el Subordinado sepa pujar
    private void ResponderCFP(MensajeFIPA cfp)
    {
        Vector3 objetivo;
        if (cfp.contenido.Contains('|'))
        {
            string[] p = cfp.contenido.Split('|');
            objetivo = new Vector3(
                float.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture)
            );
        }
        else
        {
            DatosContrato datos = JsonUtility.FromJson<DatosContrato>(cfp.contenido);
            objetivo = datos.coordenadaObjetivo;
        }

        float distancia = CalcularDistanciaNavMesh(transform.position, objetivo);

        MensajeFIPA propuesta = new MensajeFIPA(
            PerformativaFIPA.PROPOSE,
            gameObject,
            cfp.emisor,
            distancia.ToString(System.Globalization.CultureInfo.InvariantCulture)
        );
        cfp.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(propuesta);
    }

    
    private float CalcularDistanciaNavMesh(Vector3 origen, Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        
        // EL IMÁN: Forzamos que el origen y el destino sean puntos válidos pegados al suelo del NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origen, out hit, 10.0f, NavMesh.AllAreas)) origen = hit.position;
        if (NavMesh.SamplePosition(destino, out hit, 10.0f, NavMesh.AllAreas)) destino = hit.position;

        Debug.DrawRay(origen, Vector3.up * 10f, Color.yellow, 10f); // Palo amarillo en el guardia
        Debug.DrawRay(destino, Vector3.up * 10f, Color.blue, 10f);  // Palo azul en el destino

        // Si logra trazar la ruta...
        if (NavMesh.CalculatePath(origen, destino, NavMesh.AllAreas, path))
        {
            // OJO: Si el camino está incompleto (ej. el jugador está en una zona inalcanzable)
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                return 9999f; // Le ponemos una distancia gigante para que pierda la subasta
            }

            float distanciaTotal = 0f;
            // Sumamos la distancia real caminando por las esquinas
            for (int i = 1; i < path.corners.Length; i++)
            {
                distanciaTotal += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            
                // LA PRUEBA DEL DELITO: Dibuja una línea roja en la pestaña 'Scene' que dura 10 segundos
                Debug.DrawLine(path.corners[i - 1], path.corners[i], Color.red, 10f); 
            }
            return distanciaTotal;
        }
        
        // Solo si todo falla estrepitosamente usamos la línea recta
        Debug.LogError($"[TRAMPA] {gameObject.name} no pudo usar NavMesh. Castigo de 9999m.");
        return 9999f; // ¡NUNCA MÁS LÍNEA RECTA!Debug.LogError($"[TRAMPA] {gameObject.name} no pudo usar NavMesh. Calculando línea recta atravesando paredes.");
        //return Vector3.Distance(origen, destino); 
    }
}