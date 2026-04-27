using UnityEngine;
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

    private float tiempoDesdePerdido = 0f;
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
        tiempoDesdePerdido = 0f;
        debeTerminar = false;
        ofertas.Clear();
        perseguidoresGPS.Clear();

       // Si soy un patrulla, yo mismo me pongo a perseguir
        if (cerebro != null) cerebro.capaSocial.miRolAsignado = RolTactico.PersecucionActiva;

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
        if (sensores.TransformJugador != null)
        {
            faseActual = FaseAlerta.ContactoVisual;
            return;
        }

        // Si perdemos al jugador, pasamos a Búsqueda Activa ETERNA
        if (faseActual == FaseAlerta.ContactoVisual)
        {
            faseActual = FaseAlerta.BusquedaActiva;
            Debug.Log($"[PLAN] {gameObject.name}: Objetivo perdido. Iniciando cerco permanente.");

            foreach (GameObject p in perseguidoresGPS)
            {
                // Ordenamos a los perseguidores que inicien la búsqueda
                // Esto les llevará eventualmente a comprobar el tesoro
                DatosContrato d = new DatosContrato { 
                    rolOfertado = RolTactico.ExplorarSectorSospechoso, 
                    coordenadaObjetivo = transform.position 
                };
                p.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, p, JsonUtility.ToJson(d)));
            }
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

    public void TerminarMando()
    {
        Debug.Log($"[PLAN {gameObject.name}] Disolviendo escuadron.");
        faseActual = FaseAlerta.Tranquilidad;
        debeTerminar = false;

        foreach (GestorSocial comp in todosLosSociales)
        {
            if (comp.gameObject == gameObject) continue;
            BuzonMensajes buzon = comp.GetComponent<BuzonMensajes>();
            if (buzon == null) continue;
            Debug.Log($"[PLAN {gameObject.name}] Vuelta a patrulla → {comp.gameObject.name}");
            buzon.RecibirMensaje(new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, gameObject, comp.gameObject, "VueltaPatrulla"));
        }
    }


    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.5f);
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

        for (int i = 0; i < ofertas.Count; i++)
        {
            DatosContrato c = new DatosContrato();
            GameObject g = ofertas[i].guardia;

            // 1º Y 4º GUARDIA: Persiguen
            if (i == 0 || (i == 3 && GetComponent<IACerebroVigia>() != null)) 
            {
                c.rolOfertado = RolTactico.PersecucionActiva;
                c.coordenadaObjetivo = posLadron;
                perseguidoresGPS.Add(g); 
            }
            // 2º Y 3º GUARDIA: A las trampas
            else if (i == 1 || i == 2)
            {
                c.rolOfertado = RolTactico.BloqueoSalida;
                
                if (sectorLadron != null && sectorLadron.puntosDeTrampa != null && sectorLadron.puntosDeTrampa.Length > 0)
                {
                    int idx = (i - 1) % sectorLadron.puntosDeTrampa.Length;
                    c.coordenadaObjetivo = sectorLadron.puntosDeTrampa[idx].position;
                }
                else
                {
                    // AQUÍ ESTABA MI ERROR. ¡AHORA SÍ ESTÁ TOTALMENTE ELIMINADA LA META!
                    // Si el sector no tiene trampas, van a emboscarte a TU POSICIÓN.
                    Debug.LogWarning($"[PLAN] El {sectorLadron?.gameObject.name} no tiene trampas en el Inspector. ¡Emboscada directa al jugador!");
                    c.coordenadaObjetivo = posLadron;
                }
            }
            // RESTO: Patrulla adyacente
            else 
            {
                c = TareaPatrullaAdyacente(i, sectorLadron);
                if (c.puntosDeRuta == null || c.puntosDeRuta.Count == 0)
                {
                    c.rolOfertado = RolTactico.PersecucionActiva;
                    c.coordenadaObjetivo = posLadron;
                    if (!perseguidoresGPS.Contains(g)) perseguidoresGPS.Add(g);
                }
            }

            g.GetComponent<BuzonMensajes>().RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, g, JsonUtility.ToJson(c)));
        }
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

    // Tarea de cobertura para guardias sin plaza asignada
    private DatosContrato TareaPatrullaAdyacente(int indice, SectorTactico sectorL)
    {
        DatosContrato r = new DatosContrato { rolOfertado = RolTactico.PatrullaSectorAdyacente };
        
        // ARREGLO 3: Usamos el sector donde está el ladrón, no nuestro propio sector
        if (sectorL != null && sectorL.sectoresAdyacentes != null && sectorL.sectoresAdyacentes.Length > 0)
        {
            SectorTactico destino = sectorL.sectoresAdyacentes[indice % sectorL.sectoresAdyacentes.Length];
            foreach (Transform t in destino.puntosDeInteres)
                r.puntosDeRuta.Add(t.position);
        }
        return r;
    }
}
