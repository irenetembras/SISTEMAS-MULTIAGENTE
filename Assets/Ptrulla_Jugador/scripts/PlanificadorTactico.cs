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

    private IACerebro cerebro;
    private IAMovimiento movimiento;
    private GestorSocial[] todosLosSociales;

    private float tiempoDesdePerdido = 0f;
    private bool debeTerminar = false;

    private struct OfertaTactica { public GameObject guardia; public float distancia; }
    private List<OfertaTactica> ofertas = new List<OfertaTactica>();

    public void Inicializar(IACerebro c)
    {
        cerebro = c;
        movimiento = GetComponent<IAMovimiento>();
        todosLosSociales = FindObjectsByType<GestorSocial>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    // ── PUNTO DE ENTRADA ─────────────────────────────────────────────────────

    public void IniciarPlanificacion(Vector3 posLadron)
    {
        faseActual = FaseAlerta.ContactoVisual;
        tiempoDesdePerdido = 0f;
        debeTerminar = false;
        ofertas.Clear();

        bool soyVigia = GetComponent<IACerebroVigia>() != null;
        if (!soyVigia)
            cerebro.capaSocial.miRolAsignado = RolTactico.PersecucionActiva;

        Debug.Log($"[PLAN {gameObject.name}] Planificacion iniciada. Soy vigia: {soyVigia}. Mi rol: {cerebro.capaSocial.miRolAsignado}. Posicion ladron: {posLadron}");
        EnviarCFP(posLadron);
        StartCoroutine(CerrarSubastaYAsignar(posLadron));
    }

    // ── ACTUALIZACIÓN DE FASE ────────────────────────────────────────────────

    public void ActualizarFase()
    {
        if (cerebro.objetivoDetectado)
        {
            tiempoDesdePerdido = 0f;
            faseActual = FaseAlerta.ContactoVisual;
            return;
        }

        tiempoDesdePerdido += Time.deltaTime;

        if (faseActual == FaseAlerta.ContactoVisual && tiempoDesdePerdido > 2f)
        {
            faseActual = FaseAlerta.BusquedaActiva;
            if (cerebro.capaSocial.miRolAsignado == RolTactico.PersecucionActiva)
                cerebro.capaSocial.miRolAsignado = RolTactico.ExplorarSectorSospechoso;
            Debug.Log($"[PLAN {gameObject.name}] Fase → BUSQUEDA ACTIVA");
        }
        else if (faseActual == FaseAlerta.BusquedaActiva && tiempoDesdePerdido > 15f)
        {
            faseActual = FaseAlerta.Contencion;
            Debug.Log($"[PLAN {gameObject.name}] Fase → CONTENCION. Bloqueos activos.");
        }
        else if (faseActual == FaseAlerta.Contencion && tiempoDesdePerdido > 45f)
        {
            Debug.Log($"[PLAN {gameObject.name}] Sector despejado. Disolviendo escuadron.");
            debeTerminar = true;
        }
    }

    public void RegistrarOferta(GameObject guardia, float distancia)
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

    // ── CONTRACT NET PROTOCOL ────────────────────────────────────────────────

    private void EnviarCFP(Vector3 posLadron)
    {
        DatosContrato contrato = new DatosContrato
        {
            rolOfertado = RolTactico.PersecucionActiva,
            coordenadaObjetivo = posLadron
        };
        string json = JsonUtility.ToJson(contrato);

        int enviados = 0;
        foreach (GestorSocial comp in todosLosSociales)
        {
            if (comp.gameObject == gameObject) continue;
            BuzonMensajes buzon = comp.GetComponent<BuzonMensajes>();
            if (buzon == null)
            {
                Debug.LogWarning($"[PLAN {gameObject.name}] {comp.gameObject.name} no tiene BuzonMensajes. No recibira el CFP.");
                continue;
            }
            Debug.Log($"[PLAN {gameObject.name}] CFP enviado a {comp.gameObject.name}");
            buzon.RecibirMensaje(new MensajeFIPA(PerformativaFIPA.CFP, gameObject, comp.gameObject, json));
            enviados++;
        }
        Debug.Log($"[PLAN {gameObject.name}] CFP enviado a {enviados} de {todosLosSociales.Length - 1} guardias. Esperando ofertas 0.4s...");
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.4f);

        Debug.Log($"[PLAN {gameObject.name}] Subasta cerrada. {ofertas.Count} ofertas recibidas.");
        ofertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        bool soyVigia = GetComponent<IACerebroVigia>() != null;

        // Generamos la lista de tareas para esta situación
        List<TareaContrato> tareas = GenerarTareas(posLadron, soyVigia);

        // Imprimimos el briefing completo de la misión
        System.Text.StringBuilder briefing = new System.Text.StringBuilder();
        briefing.AppendLine($"[COMANDANTE {gameObject.name}] === ORDEN DE MISION ===");
        briefing.AppendLine($"  Posicion del intruso: {posLadron}");
        briefing.AppendLine($"  Tareas a repartir:");
        foreach (TareaContrato t in tareas)
            briefing.AppendLine($"    - '{t.nombre}' | Rol: {t.rol} | Guardias necesarios: {t.guardiasNecesarios} | Coordenada: {t.coordenada}");
        briefing.AppendLine($"  Candidatos disponibles: {ofertas.Count}");
        Debug.Log(briefing.ToString());

        // Expandimos las tareas en slots individuales (una plaza por guardia necesario)
        List<TareaContrato> slots = new List<TareaContrato>();
        foreach (TareaContrato tarea in tareas)
            for (int k = 0; k < tarea.guardiasNecesarios; k++)
                slots.Add(tarea);

        // Asignamos cada candidato (ordenado por proximidad) a la siguiente plaza disponible
        for (int i = 0; i < ofertas.Count; i++)
        {
            DatosContrato respuesta = new DatosContrato();

            if (i < slots.Count)
            {
                TareaContrato tarea = slots[i];
                respuesta.rolOfertado = tarea.rol;
                respuesta.coordenadaObjetivo = tarea.coordenada;
                respuesta.puntosDeRuta = tarea.puntosDeRuta;
                Debug.Log($"[COMANDANTE {gameObject.name}] Orden a {ofertas[i].guardia.name}: tarea '{tarea.nombre}' → rol {tarea.rol}, dirigirse a {tarea.coordenada}");
            }
            else
            {
                respuesta = TareaPatrullaAdyacente(i - slots.Count);
                Debug.Log($"[COMANDANTE {gameObject.name}] Orden a {ofertas[i].guardia.name}: sin tarea especifica → patrullar sector adyacente");
            }

            string json = JsonUtility.ToJson(respuesta);
            ofertas[i].guardia.GetComponent<BuzonMensajes>()
                .RecibirMensaje(new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, gameObject, ofertas[i].guardia, json));
        }

        int totalEquipo = ofertas.Count + 1;
        Debug.Log($"[PLAN {gameObject.name}] Equipo formado: {totalEquipo} guardias ({ofertas.Count} subordinados + 1 comandante).");
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
            coordenada = cerebro.puntoMeta != null ? cerebro.puntoMeta.position : posLadron
        });

        // Tarea secundaria: explorar la zona donde se perdió al jugador
        tareas.Add(new TareaContrato
        {
            nombre = "ExplorarZonaSospechosa",
            rol = RolTactico.ExplorarSectorSospechoso,
            guardiasNecesarios = 1,
            coordenada = posLadron
        });

        // Los guardias sobrantes cubrirán sectores adyacentes (fallback en CerrarSubasta)
        return tareas;
    }

    // Tarea de cobertura para guardias sin plaza asignada
    private DatosContrato TareaPatrullaAdyacente(int indice)
    {
        DatosContrato r = new DatosContrato { rolOfertado = RolTactico.PatrullaSectorAdyacente };
        SectorTactico miSector = movimiento.sectorActual;
        if (miSector != null && miSector.sectoresAdyacentes != null && miSector.sectoresAdyacentes.Length > 0)
        {
            SectorTactico destino = miSector.sectoresAdyacentes[indice % miSector.sectoresAdyacentes.Length];
            foreach (Transform t in destino.puntosDeInteres)
                r.puntosDeRuta.Add(t.position);
        }
        return r;
    }
}
