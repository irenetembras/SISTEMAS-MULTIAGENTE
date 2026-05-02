using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

// Gestiona el protocolo Contract Net y las fases de alerta cuando el guardia es Comandante.
public class PlanificadorTactico : MonoBehaviour
{
    [Header("Fases de Alerta (solo lectura en inspector)")]
    public FaseAlerta faseActual = FaseAlerta.Tranquilidad;

    private IACerebro cerebro; // null si es el vigía
    private IAMovimiento movimiento;
    private GestorSocial[] todosLosSociales;
    private GestorSensores sensores;

    private Vector3 ultimaPosConocida;
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
            return;
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

    public void ActualizarFase()
    {
        if (sensores == null) return;

        if (sensores.EnContactoConJugador)
        {
            faseActual = FaseAlerta.ContactoVisual;
            ultimaPosConocida = sensores.TransformJugador.position;
            return;
        }

        if (!planDesplegado) return;

        if (faseActual == FaseAlerta.ContactoVisual)
        {
            faseActual = FaseAlerta.BusquedaActiva;
            DesplegarCercoPermanente();
        }
    }

    private void DesplegarCercoPermanente()
    {
        // Si hay robo activo nadie abandona su posición de bloqueo
        if (RecogerObjetivo.tieneElBotin)
        {
            if (cerebro != null)
            {
                cerebro.capaSocial.miRolAsignado = RolTactico.BloqueoSalida;
                if (cerebro.puntoMeta != null) cerebro.coordenadaTactica = cerebro.puntoMeta.position;
            }
            debeTerminar = true;
            return;
        }

        Debug.Log($"[PLAN] {gameObject.name}: Objetivo perdido. Iniciando cerco permanente y repartiendo puntos.");

        SectorTactico sectorLadron = EncontrarSector(ultimaPosConocida);

        List<Vector3> puntosDelSector = new List<Vector3>();
        if (sectorLadron != null && sectorLadron.puntosDeInteres != null)
        {
            foreach(Transform t in sectorLadron.puntosDeInteres) puntosDelSector.Add(t.position);
        }

        string nombreSec = sectorLadron != null ? sectorLadron.gameObject.name : "NULO";
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> Jugador perdido en la coordenada: {ultimaPosConocida}.");
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> El Planificador cree que el sector más cercano es: {nombreSec}");
        Debug.Log($"<color=yellow>[PLAN DIAGNÓSTICO]</color> Puntos de exploración extraídos de {nombreSec}: {puntosDelSector.Count}");

        List<GameObject> exploradoresDisponibles = new List<GameObject>(transmisorGPS.ObtenerPerseguidores());
        if (cerebro != null) exploradoresDisponibles.Insert(0, gameObject);

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

            // Repartimos los puntos del sector en dos mitades para que no se solapen
            if (puntosDelSector.Count > 0)
            {
                puntosDelSector.Sort((a, b) => UtilidadesNavMesh.CalcularDistancia(ultimaPosConocida, a).CompareTo(UtilidadesNavMesh.CalcularDistancia(ultimaPosConocida, b)));
                int mitad = puntosDelSector.Count / 2;

                if (indiceExplorador == 0) d.puntosDeRuta = puntosDelSector.GetRange(0, mitad);
                else d.puntosDeRuta = puntosDelSector.GetRange(mitad, puntosDelSector.Count - mitad);
            }

            int puntosQueLeTocan = d.puntosDeRuta != null ? d.puntosDeRuta.Count : 0;
            Debug.Log($"<color=cyan>[REPARTO]</color> Asignando al guardia {p.name} un total de {puntosQueLeTocan} puntos para explorar.");

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
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.5f);

        if (RecogerObjetivo.tieneElBotin)
        {
            Debug.Log($"[PLAN] Subasta cancelada. El ladrón pilló el botín.");
            planDesplegado = true;
            yield break;
        }

        ofertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        List<GameObject> guardiasDisponibles = new List<GameObject>();
        foreach (var o in ofertas) guardiasDisponibles.Add(o.guardia);

        bool soyVigia = (cerebro == null);
        List<TareaContrato> tareasNecesarias = GenerarTareas(posLadron, soyVigia);

        AsignarTareas(guardiasDisponibles, tareasNecesarias, posLadron);

        planDesplegado = true;
    }

    // Empareja guardias disponibles (ordenados por distancia al ladrón) con las tareas generadas
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

                // Para bloqueo buscamos al más cercano a cada punto de trampa
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

                guardiasDisponibles.Remove(guardiaElegido);
            }
        }

        // Los guardias sobrantes patrullan sectores adyacentes al del ladrón
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

    // Define las tareas a cubrir según la situación táctica actual
    private List<TareaContrato> GenerarTareas(Vector3 posLadron, bool soyVigia)
    {
        List<TareaContrato> tareas = new List<TareaContrato>();

        // El vigía no se puede mover, así que delega la persecución directa a otro guardia
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

        tareas.Add(new TareaContrato
        {
            nombre = "BloquearSalida",
            rol = RolTactico.BloqueoSalida,
            guardiasNecesarios = 2,
            coordenada = (cerebro != null && cerebro.puntoMeta != null) ? cerebro.puntoMeta.position : posLadron
        });

        // Guardia de apoyo que también recibe GPS
        tareas.Add(new TareaContrato
        {
            nombre = "ApoyoPersecucion",
            rol = RolTactico.PersecucionActiva,
            guardiasNecesarios = 1,
            coordenada = posLadron
        });

        return tareas;
    }

    private DatosContrato TareaPatrullaAdyacente(int indice, SectorTactico sectorL)
    {
        DatosContrato r = new DatosContrato
        {
            rolOfertado = RolTactico.PatrullaSectorAdyacente,
            puntosDeRuta = new List<Vector3>()
        };

        // Usamos el sector del ladrón como referencia para elegir el adyacente
        if (sectorL != null && sectorL.sectoresAdyacentes != null && sectorL.sectoresAdyacentes.Length > 0)
        {
            SectorTactico destino = sectorL.sectoresAdyacentes[indice % sectorL.sectoresAdyacentes.Length];
            r.nombreSectorDestino = destino.gameObject.name;
        }
        return r;
    }

    private void MandarContrato(GameObject g, RolTactico rol, Vector3 destino)
    {
        DatosContrato c = new DatosContrato { rolOfertado = rol, coordenadaObjetivo = destino };
        g.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, g, JsonUtility.ToJson(c)));
    }

    private SectorTactico EncontrarSector(Vector3 posicion)
    {
        SectorTactico[] todosSectores = FindObjectsByType<SectorTactico>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        SectorTactico mejorSector = null;
        float minD = float.MaxValue;

        foreach (var s in todosSectores)
        {
            if (s.puntosDeInteres != null && s.puntosDeInteres.Length > 0)
            {
                foreach (Transform punto in s.puntosDeInteres)
                {
                    float d = UtilidadesNavMesh.CalcularDistancia(punto.position, posicion);
                    
                    if (d < minD)
                    {
                        minD = d;
                        mejorSector = s;
                    }
                }
            }
            else
            {
                float dCentro = UtilidadesNavMesh.CalcularDistancia(s.transform.position, posicion);
                
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
