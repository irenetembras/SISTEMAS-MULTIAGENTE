using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
public class IACerebro : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento")]
    public EstadoIA patrulla;
    public EstadoIA persecucion;
    public EstadoIA emboscada;
    public EstadoIA busqueda;              
    public EstadoIA exploracion;           
    public EstadoIA comprobandoObjetivo;   

    [Header("Ajustes de Tiempo")]
    public float tiempoPerdidoParaBuscar = 2.0f; // Tiempo de duda al perder al jugador
    private float cronometroPerdido = 0f;
    
    [Header("Memoria Global")]
    public Transform puntoMeta;
    public Transform puntoObjetivo;

    public EstadoIA estadoActual { get; private set; }
    private GestorSensores sensores;
    private IAMovimiento movimiento;

    // Memoria que usarán los cartuchos
    public bool objetivoDetectado = false; 
    public Vector3 ultimaPosJugador;

    // --- VARIABLES DE COMUNICACIÓN MULTIAXENTE (TU CÓDIGO) ---
    private BuzonMensajes miBuzon;
    private IACerebro[] todosLosGuardias;

    private struct Oferta
    {
        public GameObject guardia;
        public float distancia;
    }
    private List<Oferta> listaDeOfertas = new List<Oferta>();

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();
        miBuzon = GetComponent<BuzonMensajes>();
        todosLosGuardias = FindObjectsOfType<IACerebro>(); 

        if(patrulla) patrulla.Configurar(this, movimiento);
        if(persecucion) persecucion.Configurar(this, movimiento);
        if(emboscada) emboscada.Configurar(this, movimiento);
        if(busqueda) busqueda.Configurar(this, movimiento);                       
        if(exploracion) exploracion.Configurar(this, movimiento);                 
        if(comprobandoObjetivo) comprobandoObjetivo.Configurar(this, movimiento); 
    }

    void OnEnable()
    {
        sensores.OnJugadorDetectado += AlDetectar;
        sensores.OnJugadorPerdido += AlPerder;
    }

    void OnDisable()
    {
        sensores.OnJugadorDetectado -= AlDetectar;
        sensores.OnJugadorPerdido -= AlPerder;
    }

    void Start()
    {
        CambiarEstado(patrulla); // Siempre empezamos patrullando
    }

    void Update()
    {
        if (estadoActual == null) return;

        // 1. MIRAMOS EL BUZÓN CONTINUAMENTE (TU CÓDIGO)
        if (miBuzon != null && miBuzon.HayMensajesNuevos())
        {
            ProcesarBuzon();
        }

        // --- TRANSICIONES SEGÚN EL ESTADO ACTUAL (EL CÓDIGO DE TU COMPAÑERO) ---
        if (estadoActual == patrulla && RecogerObjetivo.tieneElBotin)
        {
            CambiarEstado(emboscada);
        }
        else if (estadoActual == persecucion)
        {
            if (!objetivoDetectado)
            {
                cronometroPerdido += Time.deltaTime;
                if (cronometroPerdido >= tiempoPerdidoParaBuscar)
                {
                    if (RecogerObjetivo.tieneElBotin) CambiarEstado(emboscada);
                    else CambiarEstado(busqueda);
                }
            }
            else cronometroPerdido = 0f;
        }
        else if (estadoActual == busqueda)
        {
            if (movimiento.HaLlegadoAlDestino())
            {
                CambiarEstado(exploracion);
            }
        }
        else if (estadoActual == exploracion)
        {
            // Ojo aquí: Si en tu script exploracion no tienes "exploracionTerminada", 
            // asegúrate de que el código de tu compañero coincide con tus scripts de estado.
            if (((EstadoExploracion)exploracion).exploracionTerminada)
            {
                CambiarEstado(comprobandoObjetivo);
            }
        }
        else if (estadoActual == comprobandoObjetivo)
        {
            float distAlBotin = Vector3.Distance(transform.position, puntoObjetivo.position);
            if (movimiento.HaLlegadoAlDestino() || distAlBotin < 4.0f)
            {
                CambiarEstado(patrulla);
            }
        }
    }

    // --- RESPUESTA A LOS EVENTOS DE LOS SENSORES (FUSIONADO) ---
    private void AlDetectar(Vector3 pos)
    {
        // TU CÓDIGO (APRETAR EL GATILLO Y ABRIR SUBASTA)
        if (estadoActual != persecucion)
        {
            listaDeOfertas.Clear();
            EnviarAvisoDeLadron(pos);
            StartCoroutine(CerrarSubastaYAsignar(pos));
        }

        objetivoDetectado = true;
        ultimaPosJugador = pos;
        CambiarEstado(persecucion); // Cambio inmediato de cartucho
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }

    // --- EL MOTOR QUE CAMBIA LOS ESTADOS ---
    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;
        if (estadoActual != null) estadoActual.AlSalir(); 
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar(); 
        Debug.Log("<color=yellow>CEREBRO: Cambiando al estado -> " + nuevoEstado.GetType().Name + "</color>");
    }

    // --- MÉTODOS DE COMUNICACIÓN MULTIAXENTE (TU CÓDIGO) ---
    private void EnviarAvisoDeLadron(Vector3 posLadron)
    {
        string contenido = posLadron.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

        foreach (IACerebro compañero in todosLosGuardias)
        {
            if (compañero != this)
            {
                MensajeFIPA aviso = new MensajeFIPA(PerformativaFIPA.CFP, this.gameObject, compañero.gameObject, contenido);
                compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
            }
        }
        Debug.Log("📢 ¡" + gameObject.name + " pide voluntarios! Ladrón en: " + contenido);
    }

    private void ProcesarBuzon()
    {
        MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
        if (mensaje == null) return;

        if (mensaje.performativa == PerformativaFIPA.CFP)
        {
            string[] coordenadas = mensaje.contenido.Split('|'); 
            if (coordenadas.Length == 3)
            {
                float x = float.Parse(coordenadas[0], System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(coordenadas[1], System.Globalization.CultureInfo.InvariantCulture);
                float z = float.Parse(coordenadas[2], System.Globalization.CultureInfo.InvariantCulture);
                Vector3 posLadron = new Vector3(x, y, z);
                
                if (estadoActual == persecucion || estadoActual == emboscada) return;

                float miDistancia = Vector3.Distance(transform.position, posLadron);
                MensajeFIPA oferta = new MensajeFIPA(PerformativaFIPA.PROPOSE, this.gameObject, mensaje.emisor, miDistancia.ToString(System.Globalization.CultureInfo.InvariantCulture));
                mensaje.emisor.GetComponent<BuzonMensajes>().RecibirMensaje(oferta);
            }
        }
        else if (mensaje.performativa == PerformativaFIPA.PROPOSE)
        {
            float distancia = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
            listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = distancia });
        }
        else if (mensaje.performativa == PerformativaFIPA.ACCEPT_PROPOSAL)
        {
            string[] coordenadas = mensaje.contenido.Split('|'); 
            if (coordenadas.Length == 3)
            {
                float x = float.Parse(coordenadas[0], System.Globalization.CultureInfo.InvariantCulture);
                float y = float.Parse(coordenadas[1], System.Globalization.CultureInfo.InvariantCulture);
                float z = float.Parse(coordenadas[2], System.Globalization.CultureInfo.InvariantCulture);

                Vector3 posicionTactica = new Vector3(x, y, z);
                
                // Le decimos a su memoria cuál es el punto de flanqueo
                ultimaPosJugador = posicionTactica; 
                
                // ¡AQUÍ ESTÁ LA MAGIA! 
                // Le mandamos a buscar. Gracias al código de tu compañero, 
                // en cuanto llegue a ese punto, pasará a 'exploracion' automáticamente.
                CambiarEstado(busqueda); 
            }
        }
        else if (mensaje.performativa == PerformativaFIPA.REJECT_PROPOSAL)
        {
            // Ignorado, sigue con la patrulla de tu compañero
        }
    }

    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        yield return new WaitForSeconds(0.5f);

        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        int guardiasAceptados = 0;
        int maxGuardias = 2; 


        // Usamos tu función de Movimiento para garantizar que los puntos tácticos
        // caen obligatoriamente encima del suelo azul del NavMesh (radio de 5m para buscar)
        Vector3[] puntosEstrategicos = new Vector3[] {
            movimiento.ObtenerPuntoAleatorioCercano(posLadron + new Vector3(10f, 0, 10f), 5f),  
            movimiento.ObtenerPuntoAleatorioCercano(posLadron + new Vector3(-10f, 0, -10f), 5f) 
        };

        foreach (Oferta oferta in listaDeOfertas)
        {
            if (guardiasAceptados < maxGuardias)
            {
                Vector3 punto = puntosEstrategicos[guardiasAceptados];
                string contAceptar = punto.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, this.gameObject, oferta.guardia, contAceptar);
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
                guardiasAceptados++;
            }
            else
            {
                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, this.gameObject, oferta.guardia, "Sigue patrullando");
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
            }
        }
    }
}