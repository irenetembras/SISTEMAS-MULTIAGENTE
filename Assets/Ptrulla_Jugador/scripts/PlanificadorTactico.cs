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
    private TransmisorGPS transmisorGPS;
    public void Inicializar(IACerebro c)
    {
        cerebro = c;
        movimiento = GetComponent<IAMovimiento>();
        sensores = GetComponent<GestorSensores>();
        transmisorGPS = GetComponent<TransmisorGPS>();
        todosLosSociales = FindObjectsByType<GestorSocial>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    void Update()
    {
        if (faseActual != FaseAlerta.Tranquilidad)
        {
            ActualizarFase();
        }
    }


    // ── PUNTO DE ENTRADA ─────────────────────────────────────────────────────

    public void IniciarPlanificacion(Vector3 posLadron)
    {
        faseActual = FaseAlerta.ContactoVisual;
        ultimaPosConocida = posLadron;
        debeTerminar = false;

        planDesplegado = false;

        ofertas.Clear();
        transmisorGPS.LimpiarPerseguidores();

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

        // 1. Si hay contacto visual, reseteamos el estado a ContactoVisual
        if (sensores.EnContactoConJugador)
        {
            faseActual = FaseAlerta.ContactoVisual;
            ultimaPosConocida = sensores.TransformJugador.position;
            return;
        }

        if (!planDesplegado) return;

        // 2. Si lo acabamos de perder (estábamos en ContactoVisual y ya no lo vemos)
        if (faseActual == FaseAlerta.ContactoVisual)
        {
            faseActual = FaseAlerta.BusquedaActiva;
            
            // Delegamos toda la lógica compleja a una función específica
            DesplegarCercoPermanente();
        }
    }

    // ── LÓGICA DE BÚSQUEDA CUANDO SE PIERDE AL JUGADOR ───────────────────────

    private void DesplegarCercoPermanente()
    {
        // --- CORTAFUEGOS: ¡NADIE ABANDONA LA PUERTA SI HAY ROBO! ---
        if (RecogerObjetivo.tieneElBotin)
        {
            // Si soy un guardia físico, me pongo a explorar yo solo (bloquear salida).
            if (cerebro != null)
            {
                cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
                if (cerebro.puntoMeta != null) cerebro.coordenadaTactica = cerebro.puntoMeta.position;
            }
            
            debeTerminar = true; // El jefe dimite para no dar más órdenes
            return; 
        }

        Debug.Log($"[PLAN] {gameObject.name}: Objetivo perdido. Iniciando cerco permanente y repartiendo puntos.");

        // 1. Buscamos en qué SectorTactico desapareció
        SectorTactico sectorLadron = EncontrarSector(ultimaPosConocida);

        // 2. Sacamos todos los puntos de ese sector a una lista
        List<Vector3> puntosDelSector = new List<Vector3>();
        if (sectorLadron != null && sectorLadron.puntosDeInteres != null) {
            foreach(Transform t in sectorLadron.puntosDeInteres) puntosDelSector.Add(t.position);
        }

        string nombreSec = sectorLadron != null ? sectorLadron.gameObject.name : "NULO";
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> Jugador perdido en la coordenada: {ultimaPosConocida}.");
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> El Planificador cree que el sector más cercano es: {nombreSec}");
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> Puntos de exploración extraídos de {nombreSec}: {puntosDelSector.Count}");

        // 3. REPARTO INTELIGENTE a los perseguidores
        List<GameObject> exploradoresDisponibles = new List<GameObject>(transmisorGPS.ObtenerPerseguidores());
        if (cerebro != null) exploradoresDisponibles.Insert(0, gameObject); // Me añado el primero si tengo cuerpo

        int indiceExplorador = 0;

        while (exploradoresDisponibles.Count > 0)
        {
            GameObject p = exploradoresDisponibles[0];
            exploradoresDisponibles.RemoveAt(0);
            if (p == null) continue;

            DatosContrato d = new DatosContrato { 
                rolOfertado = RolTactico.ExplorarSectorSospechoso, 
                coordenadaObjetivo = ultimaPosConocida 
            };

            // Reparto de la ruta (Mitad para el Jefe, mitad para el Apoyo)
            if (puntosDelSector.Count > 0)
            {
                puntosDelSector.Sort((a, b) => UtilidadesNavMesh.CalcularDistancia(ultimaPosConocida, a).CompareTo(UtilidadesNavMesh.CalcularDistancia(ultimaPosConocida, b)));
                int mitad = puntosDelSector.Count / 2;

                if (indiceExplorador == 0) d.puntosDeRuta = puntosDelSector.GetRange(0, mitad);
                else d.puntosDeRuta = puntosDelSector.GetRange(mitad, puntosDelSector.Count - mitad);
            }

            int puntosQueLeTocan = d.puntosDeRuta != null ? d.puntosDeRuta.Count : 0;
            Debug.Log($"<color=cyan>[REPARTO]</color> Asignando al guardia {p.name} un total de {puntosQueLeTocan} puntos para explorar.");
            
            // Asignación de la tarea
            if (p == gameObject && cerebro != null)
            {
                cerebro.capaSocial.miRolAsignado = RolTactico.ExplorarSectorSospechoso;
                cerebro.coordenadaTactica = ultimaPosConocida;
                cerebro.ultimaPosJugador = ultimaPosConocida;
                if (d.puntosDeRuta != null) cerebro.rutaExploracion = d.puntosDeRuta;
            }
            else
            {
                p.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, p, JsonUtility.ToJson(d)));
            }

            indiceExplorador++;
        }

        debeTerminar = true; 
    }

    public void RecibirOferta(GameObject guardia, float distancia)
    {
        ofertas.Add(new OfertaTactica { guardia = guardia, distancia = distancia });
        Debug.Log($"[PLAN {gameObject.name}] Oferta de {guardia.name}: {distancia:F1}m. Total ofertas: {ofertas.Count}");
    }

    public bool DebeTerminarMando() => debeTerminar;

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

        // Cortafuegos
        if (RecogerObjetivo.tieneElBotin) 
        {
            Debug.Log($"[PLAN] Subasta cancelada. El ladrón pilló el botín.");
            planDesplegado = true;
            yield break;
        }

        // Ordenar a los ofertantes de más cercano a más lejano
        ofertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        // Separar las ofertas en una lista de guardias disponibles
        List<GameObject> guardiasDisponibles = new List<GameObject>();
        foreach (var o in ofertas) guardiasDisponibles.Add(o.guardia);

        bool soyVigia = (cerebro == null); // Si no tengo cerebro, soy vigia 
        // 1. ESTRATEGIA: Generar la lista de tareas necesarias
        List<TareaContrato> tareasNecesarias = GenerarTareas(posLadron, soyVigia);

        // 2. EMPAREJAMIENTO: Asignar guardias disponibles a las tareas
        AsignarTareas(guardiasDisponibles, tareasNecesarias, posLadron);

        planDesplegado = true;
    }


    // Motor de asignación: Empareja la lista de guardias ordenados con las tareas
    private void AsignarTareas(List<GameObject> guardiasDisponibles, List<TareaContrato> tareas, Vector3 posLadron)
    {
        SectorTactico sectorLadron = EncontrarSector(posLadron);
        
        foreach (TareaContrato tarea in tareas)
        {
            for (int i = 0; i < tarea.guardiasNecesarios; i++)
            {
                if (guardiasDisponibles.Count == 0) return;

                // Por defecto, el candidato es el más cercano al ladrón
                GameObject guardiaElegido = guardiasDisponibles[0];
                Vector3 destinoFinal = tarea.coordenada;

                // Si es Bloqueo, buscamos al más cercano a la TRAMPA
                if (tarea.rol == RolTactico.BloqueoSalida && sectorLadron != null && sectorLadron.puntosDeTrampa != null && sectorLadron.puntosDeTrampa.Length > 0)
                {
                    destinoFinal = sectorLadron.puntosDeTrampa[i % sectorLadron.puntosDeTrampa.Length].position;
                    
                    float mejorDist = float.MaxValue;
                    foreach (GameObject g in guardiasDisponibles)
                    {
                        float dist = UtilidadesNavMesh.CalcularDistancia(g.transform.position, destinoFinal);
                        if (dist < mejorDist)
                        {
                            mejorDist = dist;
                            guardiaElegido = g;
                        }
                    }
                }

                MandarContrato(guardiaElegido, tarea.rol, destinoFinal);
                
                if (tarea.rol == RolTactico.PersecucionActiva)
                {
                    transmisorGPS?.RegistrarPerseguidor(guardiaElegido);
                }

                guardiasDisponibles.Remove(guardiaElegido); // Guardia ocupado, se retira del pool
            }
        }
    
        // TAREA POR DEFECTO: Los que sobren a patrullar sectores adyacentes
        int indiceAdyacente = 0;
        foreach (GameObject guardiaSobrante in guardiasDisponibles)
        {
            DatosContrato c = TareaPatrullaAdyacente(indiceAdyacente, sectorLadron);
            
            if (string.IsNullOrEmpty(c.nombreSectorDestino))
            {
                MandarContrato(guardiaSobrante, RolTactico.PersecucionActiva, posLadron);
                transmisorGPS?.RegistrarPerseguidor(guardiaSobrante);
            }
            else
            {
                guardiaSobrante.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, guardiaSobrante, JsonUtility.ToJson(c)));
            }
            indiceAdyacente++;
        }
    }


    // ── DEFINICIÓN DE TAREAS ─────────────────────────────────────────────────
    // Aquí se define qué hay que hacer en esta situación táctica.
    // Para añadir una nueva tarea: crea un TareaContrato y añádelo a la lista.

    private List<TareaContrato> GenerarTareas(Vector3 posLadron, bool soyVigia)
    {
        List<TareaContrato> tareas = new List<TareaContrato>();

        // Si soy el Vigía no puedo moverme, así que delego la persecución
        if (soyVigia)
        {
            tareas.Add(new TareaContrato
            {
                nombre = "Perseguir",
                rol = RolTactico.PersecucionActiva,
                guardiasNecesarios = 1,
                coordenada = posLadron
            });
        }

        // Tarea principal: interceptar al jugador bloqueando la salida
        tareas.Add(new TareaContrato
        {
            nombre = "BloquearSalida",
            rol = RolTactico.BloqueoSalida,
            guardiasNecesarios = 2,
            coordenada = (cerebro != null && cerebro.puntoMeta != null) ? cerebro.puntoMeta.position : posLadron
        });

        // 3. Tarea de apoyo: Otro guardia que corra hacia la posición del ladrón para ayudar a atraparlo.
        // Si nos pierden de vista, este guardia ya estará registrado en el GPS y pasará a explorar.
        tareas.Add(new TareaContrato
        {
            nombre = "ApoyoPersecucion",
            rol = RolTactico.PersecucionActiva,
            guardiasNecesarios = 1,
            coordenada = posLadron
        });

        // Los guardias sobrantes cubrirán sectores adyacentes (fallback en CerrarSubasta)
        return tareas;
    }


    // Tarea de cobertura para guardias sin plaza asignada
    private DatosContrato TareaPatrullaAdyacente(int indice, SectorTactico sectorL)
    {
        DatosContrato r = new DatosContrato { rolOfertado = RolTactico.PatrullaSectorAdyacente,
            puntosDeRuta = new List<Vector3>() 
            };
        
        // Usamos el sector donde está el ladrón, no nuestro propio sector
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

    // Función auxiliar para mantener el código limpio
    private SectorTactico EncontrarSector(Vector3 posicion)
    {
        SectorTactico[] todosSectores = FindObjectsByType<SectorTactico>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SectorTactico mejorSector = null;
        float minD = float.MaxValue;
        
        foreach (var s in todosSectores)
        {
            // Medimos la distancia SOLO a los puntos de interés internos del sector
            if (s.puntosDeInteres != null && s.puntosDeInteres.Length > 0)
            {
                foreach (Transform punto in s.puntosDeInteres)
                {
                    float d = Vector3.Distance(punto.position, posicion);
                    if (d < minD) 
                    { 
                        minD = d; 
                        mejorSector = s; 
                    }
                }
            }
            else
            {
                // Fallback de seguridad: Si por error olvidas asignar puntos a un sector en Unity, 
                // usará su centro para que el juego no se rompa con un error nulo.
                float dCentro = Vector3.Distance(s.transform.position, posicion);
                if (dCentro < minD) 
                { 
                    minD = dCentro; 
                    mejorSector = s; 
                }
            }
        }
        
        return mejorSector;
    }
}
