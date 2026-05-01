using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// El cerebro táctico del escuadrón. Solo activo cuando el guardia es Comandante.
// Gestiona el protocolo Contract Net y las fases de alerta.
//
// PARA AÑADIR UNA NUEVA TAREA: añade un TareaContrato en GenerarTareas().
// No es necesario tocar nada más del sistema.
public class PlanificadorTactico : MonoBehaviour
{
    [Header("Fases de Alerta (solo lectura en inspector)")]
    public FaseAlerta faseActual = FaseAlerta.Tranquilidad;

    private IACerebro cerebro; //nulo si es el vigia
    private IAMovimiento movimiento;
    private GestorSocial[] todosLosSociales;
    private GestorSensores sensores;

    
    private Vector3 ultimaPosConocida;

    // EL CANDADO DE SEGURIDAD
    private bool planDesplegado = false;
    private bool debeTerminar = false;

    private struct OfertaTactica { public GameObject guardia; public float distancia; }
    private List<OfertaTactica> ofertas = new List<OfertaTactica>();
    private List<GameObject> perseguidoresGPS = new List<GameObject>(); // Para el chivatazo continuo
    private float relojGPS = 0f;

    public void Inicializar(IACerebro c)
    {
        cerebro = c;
        movimiento = GetComponent<IAMovimiento>();
        sensores = GetComponent<GestorSensores>();
        todosLosSociales = FindObjectsByType<GestorSocial>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    void Update()
    {
        if (faseActual != FaseAlerta.Tranquilidad)
        {
            ActualizarFase();
            GestionarGPS();
        }
    }

    private void GestionarGPS()
    {
        if (perseguidoresGPS.Count == 0 || sensores.TransformJugador == null) return;

        // CORTAFUEGOS (Adiós Wallhack): Solo transmitimos si el sensor lo está detectando AHORA
        if (!sensores.EnContactoConJugador) return;

        relojGPS += Time.deltaTime;
        if (relojGPS >= 0.5f)
        {
            string pos = sensores.TransformJugador.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                         sensores.TransformJugador.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                         sensores.TransformJugador.position.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

            foreach (GameObject p in perseguidoresGPS)
            {
                p.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.INFORM, gameObject, p, pos));
            }
            relojGPS = 0f;
        }
    }

    // ── PUNTO DE ENTRADA ─────────────────────────────────────────────────────

    public void IniciarPlanificacion(Vector3 posLadron)
    {
        faseActual = FaseAlerta.ContactoVisual;
        ultimaPosConocida = posLadron;
        debeTerminar = false;
        // tiempoDesdePerdido = 0f;

        planDesplegado = false;

        ofertas.Clear();
        perseguidoresGPS.Clear();

       // Si soy un patrulla, yo mismo me pongo a perseguir
        if (cerebro != null) cerebro.capaSocial.miRolAsignado = RolTactico.PersecucionActiva;

        if (RecogerObjetivo.tieneElBotin)
        {
            Debug.Log("[PLAN] ¡Alarma Activa! No hay subasta, todos a la salida.");
            
            planDesplegado = true; 
            return; // Aquí salimos: ya no se ejecuta la subasta
        }

        EnviarCFP(posLadron);
        StartCoroutine(CerrarSubastaYAsignar(posLadron));
    }

    private void EnviarCFP(Vector3 posLadron)
    {
        string json = JsonUtility.ToJson(new DatosContrato { rolOfertado = RolTactico.PersecucionActiva, coordenadaObjetivo = posLadron });
        foreach (GestorSocial comp in todosLosSociales)
        {
            if (comp.gameObject == gameObject) continue;
            comp.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.CFP, gameObject, comp.gameObject, json));
        }
    }

    // ── ACTUALIZACIÓN DE FASE ────────────────────────────────────────────────

    public void ActualizarFase()
    {
        if (sensores == null) return;

        // Si hay contacto visual, reseteamos el estado de búsqueda
        if (sensores.EnContactoConJugador)
        {
            faseActual = FaseAlerta.ContactoVisual;
            ultimaPosConocida = sensores.TransformJugador.position;
            return;
        }

        if (!planDesplegado) return;

        // Si perdemos al jugador, pasamos a Búsqueda Activa ETERNA
        if (faseActual == FaseAlerta.ContactoVisual)
        {
            faseActual = FaseAlerta.BusquedaActiva;
            // --- CORTAFUEGOS 2: ¡NADIE ABANDONA LA PUERTA! ---
            if (RecogerObjetivo.tieneElBotin)
            {
                
                // Si soy un guardia físico, me pongo a explorar yo solo. Los demás ni los toco.
                if (cerebro != null)
                {
                    cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
                    if (cerebro.puntoMeta != null) cerebro.coordenadaTactica = cerebro.puntoMeta.position;
                }
                
                debeTerminar = true; // El jefe dimite para no dar más órdenes
                return; // <--- ¡ESTE RETURN ES VITAL! Evita que se repartan puntos de búsqueda.
            }
            Debug.Log($"[PLAN] {gameObject.name}: Objetivo perdido. Iniciando cerco permanente y repartiendo puntos.");

            // 1. Buscamos en qué SectorTactico desapareciste
            SectorTactico sectorLadron = null;
            SectorTactico[] todosSectores = FindObjectsByType<SectorTactico>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            float minD = float.MaxValue;
            foreach(var s in todosSectores) { 
                float d = Vector3.Distance(s.transform.position, ultimaPosConocida); 
                if(d < minD) { minD = d; sectorLadron = s; } 
            }

            // 2. Sacamos todos los puntos de ese sector a una lista
            System.Collections.Generic.List<Vector3> puntosDelSector = new System.Collections.Generic.List<Vector3>();
            if (sectorLadron != null && sectorLadron.puntosDeInteres != null) {
                foreach(Transform t in sectorLadron.puntosDeInteres) puntosDelSector.Add(t.position);
            }

            // 3. REPARTO INTELIGENTE: Repartimos los puntos a todos los que estaban persiguiendo
            List<GameObject> exploradoresDisponibles = new List<GameObject>(perseguidoresGPS);

            // Me añado a mí mismo a la lista de exploradores (si tengo cuerpo físico)
            if (cerebro != null) exploradoresDisponibles.Add(gameObject);
            
            // LOS APOYOS DESPUÉS
            foreach(GameObject p in perseguidoresGPS)
            {
                if (p != gameObject && !exploradoresDisponibles.Contains(p)) 
                    exploradoresDisponibles.Add(p);
            }

            int indiceExplorador = 0;

            while (exploradoresDisponibles.Count > 0)

            {
                GameObject p = exploradoresDisponibles[0];
                exploradoresDisponibles.RemoveAt(0);
                if (p == null) continue;

                Vector3 puntoLlegada = ultimaPosConocida;
                // El Guardia 0 (Jefe) usa la coordenada exacta. El Guardia 1 usa un radio de seguridad de 1.5m.
                if (indiceExplorador == 1) puntoLlegada += new Vector3(1.5f, 0f, 1.5f);

                DatosContrato d = new DatosContrato { 
                    rolOfertado = RolTactico.ExplorarSectorSospechoso, 
                    coordenadaObjetivo = ultimaPosConocida 
                };

                //   --- EL REPARTO PERFECTO ---
                if (puntosDelSector.Count > 0)
                {
                    // Ordenamos todos los puntos por cercanía a donde desapareció el ladrón
                    puntosDelSector.Sort((a, b) => CalcularDistanciaNavMesh(ultimaPosConocida, a).CompareTo(CalcularDistanciaNavMesh(ultimaPosConocida, b)));

                    int mitad = puntosDelSector.Count / 2;

                    if (indiceExplorador == 0)
                    {
                        // Guardia 1 (ej. Jefe): Empieza por el punto más cercano (Barrido normal frontal)
                        d.puntosDeRuta = puntosDelSector.GetRange(0, mitad);
                    }
                    else
                    {
                        // Guardia 2 (ej. Apoyo): Empieza por el punto más LEJANO (Corre al fondo y barre en reversa)
                        d.puntosDeRuta = puntosDelSector.GetRange(mitad, puntosDelSector.Count - mitad);
                    }
                }
                
                // Si la orden es para mí mismo, me la inyecto en el cerebro sin usar el buzón de radio
                if (p == gameObject && cerebro != null)
                {
                    cerebro.capaSocial.miRolAsignado = RolTactico.ExplorarSectorSospechoso;
                    cerebro.coordenadaTactica = ultimaPosConocida;
                    cerebro.ultimaPosJugador = ultimaPosConocida;
                    if (d.puntosDeRuta != null) cerebro.rutaExploracion = d.puntosDeRuta; // Guardamos la ruta en el jefe
                }
                else
                {
                    p.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, p, JsonUtility.ToJson(d)));
                }

                indiceExplorador++;
            }

            // El Comandante dimite y vuelve a ser un soldado normal que seguirá la ruta que se acaba de auto-asignar
            debeTerminar = true; 
        }


        
        // NOTA: Se han eliminado las fases de "Contención" por tiempo y "Disolución". 
        // El estado de alerta ahora es permanente.
    }

    // HEMOS BORRADO: 
    // - El paso a Contención a los 15s (porque ya están bloqueando).
    // - El "Sector Despejado" a los 45s.
    // - La disolución del escuadrón.

    public void RecibirOferta(GameObject guardia, float distancia)
    {
        ofertas.Add(new OfertaTactica { guardia = guardia, distancia = distancia });
        Debug.Log($"[PLAN {gameObject.name}] Oferta de {guardia.name}: {distancia:F1}m. Total ofertas: {ofertas.Count}");
    }

    public bool DebeTerminarMando() => debeTerminar;

    // public void TerminarMando()
    // {
    //     Debug.Log($"[PLAN {gameObject.name}] Disolviendo escuadron.");
    //     faseActual = FaseAlerta.Tranquilidad;
    //     debeTerminar = false;

    //     foreach (GestorSocial comp in todosLosSociales)
    //     {
    //         if (comp.gameObject == gameObject) continue;
    //         BuzonMensajes buzon = comp.GetComponent<BuzonMensajes>();
    //         if (buzon == null) continue;
    //         Debug.Log($"[PLAN {gameObject.name}] Vuelta a patrulla → {comp.gameObject.name}");
    //         buzon.RecibirMensaje(new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, gameObject, comp.gameObject, "VueltaPatrulla"));
    //     }
    // }

    public void TerminarMando()
    {
        Debug.Log($"[PLAN {gameObject.name}] Plan de búsqueda desplegado. Renuncio al mando táctico.");
        faseActual = FaseAlerta.Tranquilidad;
        debeTerminar = false;
        // Se apaga silenciosamente sin disolver el escuadrón.
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.5f);

        // --- CORTAFUEGOS 3: CANCELAR SUBASTA SI ROBARON MIENTRAS DORMÍA ---
        if (RecogerObjetivo.tieneElBotin) {

        Debug.Log($"[PLAN] Subasta cancelada. El ladrón pilló el botín.");
        
        planDesplegado = true;
        yield break;
        }
        ofertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        SectorTactico sectorLadron = null;
        SectorTactico[] todosSectores = FindObjectsByType<SectorTactico>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        float minD = float.MaxValue;
        foreach(var s in todosSectores) { 
            float d = Vector3.Distance(s.transform.position, posLadron); 
            if(d < minD) { minD = d; sectorLadron = s; } 
        }

        // CHIVATO PARA LA CONSOLA: Te dirá si está encontrando bien la habitación
        if (sectorLadron != null) {
            Debug.Log($"[PLAN] Ladrón detectado en: {sectorLadron.gameObject.name}");
        }

        List<Transform> trampasLibres = new List<Transform>();
        if (sectorLadron != null && sectorLadron.puntosDeTrampa != null) {
            trampasLibres.AddRange(sectorLadron.puntosDeTrampa);
        }

        List<GameObject> guardiasDisponibles = new List<GameObject>();
        foreach (var o in ofertas) guardiasDisponibles.Add(o.guardia);
        // TAREA 1: EL CAZADOR PRINCIPAL (El más cercano al jugador)
        // Si el jefe es el Vigía, necesita un perro de presa. Si es un patrulla, le mandamos apoyo.
        if (guardiasDisponibles.Count > 0 && cerebro == null )
        {
            GameObject cazador1 = guardiasDisponibles[0]; 
            MandarContrato(cazador1, RolTactico.PersecucionActiva, posLadron);
            perseguidoresGPS.Add(cazador1);
            guardiasDisponibles.RemoveAt(0); // Lo sacamos de la lista
        }

        // TAREA 2: LOS BLOQUEADORES (Los más cercanos a cada trampa)
        int bloqueadoresAsignados = 0;
        int indiceTrampa = 0;
        while (trampasLibres.Count > 0 && guardiasDisponibles.Count > 0 && bloqueadoresAsignados < 2)
        {
            // El truco matemático: Si hay 1 trampa, siempre dará 0. Si hay 2, alternará entre 0 y 1.
            Transform trampa = trampasLibres[indiceTrampa % trampasLibres.Count];
            
            GameObject mejorGuardia = guardiasDisponibles[0];
            float mejorDistancia = CalcularDistanciaNavMesh(mejorGuardia.transform.position, trampa.position);

            foreach (GameObject g in guardiasDisponibles)
            {
                float dist = CalcularDistanciaNavMesh(g.transform.position, trampa.position);
                if (dist < mejorDistancia)
                {
                    mejorDistancia = dist;
                    mejorGuardia = g;
                }
            }

            MandarContrato(mejorGuardia, RolTactico.BloqueoSalida, trampa.position);
            guardiasDisponibles.Remove(mejorGuardia); // Sacamos al bloqueador de la lista
            bloqueadoresAsignados++;
            indiceTrampa++;
        }

        // TAREA 3: EL EXPLORADOR DE APOYO (El que quedó más cerca del sector)
        // Como hemos sacado a los bloqueadores, el que está ahora en el índice [0] 
        // es matemáticamente el guardia libre más cercano a la habitación.
        if (guardiasDisponibles.Count > 0)
        {
            GameObject explorador = guardiasDisponibles[0];
            // Le mandamos a perseguir/explorar la posición del jugador
            MandarContrato(explorador, RolTactico.PersecucionActiva, posLadron); 
            perseguidoresGPS.Add(explorador);
            guardiasDisponibles.RemoveAt(0); // Lo sacamos de la lista
        }

        // TAREA 4: LOS DEMÁS (Patrulla Adyacente)
        int indiceAdyacente = 0;
        foreach (GameObject g in guardiasDisponibles)
        {
            DatosContrato c = TareaPatrullaAdyacente(indiceAdyacente, sectorLadron);
            
            // Ahora comprobamos si tiene nombre de sector asignado, ya no miramos la lista de puntos
            if (string.IsNullOrEmpty(c.nombreSectorDestino))
            {
                MandarContrato(g, RolTactico.PersecucionActiva, posLadron);
                perseguidoresGPS.Add(g);
            }
            else
            {
                g.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, g, JsonUtility.ToJson(c)));
            }
            indiceAdyacente++;
        }

        planDesplegado = true;
    }

    // ── DEFINICIÓN DE TAREAS ─────────────────────────────────────────────────
    // Aquí se define qué hay que hacer en esta situación táctica.
    // Para añadir una nueva tarea: crea un TareaContrato y añádelo a la lista.

    // private List<TareaContrato> GenerarTareas(Vector3 posLadron, bool soyVigia)
    // {
    //     List<TareaContrato> tareas = new List<TareaContrato>();

    //     // Si soy el Vigía no puedo moverme, así que delego la persecución
    //     if (soyVigia)
    //     {
    //         tareas.Add(new TareaContrato
    //         {
    //             nombre = "Perseguir",
    //             rol = RolTactico.PersecucionActiva,
    //             guardiasNecesarios = 1,
    //             coordenada = posLadron
    //         });
    //     }

    //     // Tarea principal: interceptar al jugador bloqueando la salida
    //     tareas.Add(new TareaContrato
    //     {
    //         nombre = "BloquearSalida",
    //         rol = RolTactico.BloqueoSalida,
    //         guardiasNecesarios = 2,
    //         coordenada = cerebro.puntoMeta != null ? cerebro.puntoMeta.position : posLadron
    //     });

    //     // Tarea secundaria: explorar la zona donde se perdió al jugador
    //     tareas.Add(new TareaContrato
    //     {
    //         nombre = "ExplorarZonaSospechosa",
    //         rol = RolTactico.ExplorarSectorSospechoso,
    //         guardiasNecesarios = 1,
    //         coordenada = posLadron
    //     });

    //     // Los guardias sobrantes cubrirán sectores adyacentes (fallback en CerrarSubasta)
    //     return tareas;
    // }

    private float CalcularDistanciaNavMesh(Vector3 origen, Vector3 destino)
    {
        NavMeshPath path = new NavMeshPath();
        
        // EL IMÁN: Forzamos que el origen y el destino sean puntos válidos pegados al suelo del NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(origen, out hit, 2.0f, NavMesh.AllAreas)) origen = hit.position;
        if (NavMesh.SamplePosition(destino, out hit, 2.0f, NavMesh.AllAreas)) destino = hit.position;

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
            }
            return distanciaTotal;
        }
        
        // Solo si todo falla estrepitosamente usamos la línea recta
        return Vector3.Distance(origen, destino); 
    }
    

    // Tarea de cobertura para guardias sin plaza asignada
    private DatosContrato TareaPatrullaAdyacente(int indice, SectorTactico sectorL)
    {
        DatosContrato r = new DatosContrato { rolOfertado = RolTactico.PatrullaSectorAdyacente,
            puntosDeRuta = new List<Vector3>() 
            };
        
        // ARREGLO 3: Usamos el sector donde está el ladrón, no nuestro propio sector
        if (sectorL != null && sectorL.sectoresAdyacentes != null && sectorL.sectoresAdyacentes.Length > 0)
        {
            SectorTactico destino = sectorL.sectoresAdyacentes[indice % sectorL.sectoresAdyacentes.Length];
            r.nombreSectorDestino = destino.gameObject.name;
        }
        return r;
    }

    // NUEVA FUNCIÓN AYUDANTE: Para escribir menos código al enviar contratos simples
    private void MandarContrato(GameObject g, RolTactico rol, Vector3 destino)
    {
        DatosContrato c = new DatosContrato { rolOfertado = rol, coordenadaObjetivo = destino };
        g.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, g, JsonUtility.ToJson(c)));
    }
}
