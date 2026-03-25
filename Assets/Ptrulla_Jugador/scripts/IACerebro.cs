using UnityEngine;
using System.Collections.Generic;
using System.Collections;
[RequireComponent(typeof(GestorSensores))]
[RequireComponent(typeof(IAMovimiento))]
public class IACerebro : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento (Arrastra tus scripts aquí)")]
    public EstadoIA patrulla;
    public EstadoIA persecucion;
    public EstadoIA emboscada;
    public EstadoIA busqueda;              
    public EstadoIA exploracion;           
    public EstadoIA comprobandoObjetivo;   
    
    [Header("Memoria Global")]
    public Transform puntoMeta;
    public Transform puntoObjetivo;

    public EstadoIA estadoActual { get; private set; }
    private GestorSensores sensores;
    private IAMovimiento movimiento;
    private BuzonMensajes miBuzon;
    private IACerebro[] todosLosGuardias;
    // --- VARIABLES DE NEGOCIACIÓN TÁCTICA (SUBASTA) ---
    private struct Oferta
    {
        public GameObject guardia;
        public float distancia;
    }
    private List<Oferta> listaDeOfertas = new List<Oferta>();

    // Memoria que usarán los cartuchos
    public bool objetivoDetectado = false; 
    public Vector3 ultimaPosJugador;

    void Awake()
    {
        sensores = GetComponent<GestorSensores>();
        movimiento = GetComponent<IAMovimiento>();

        if(patrulla) patrulla.Configurar(this, movimiento);
        if(persecucion) persecucion.Configurar(this, movimiento);
        if(emboscada) emboscada.Configurar(this, movimiento);
        if(busqueda) busqueda.Configurar(this, movimiento);                       // <-- NUEVO
        if(exploracion) exploracion.Configurar(this, movimiento);                 // <-- NUEVO
        if(comprobandoObjetivo) comprobandoObjetivo.Configurar(this, movimiento); // <-- NUEVO
        miBuzon = GetComponent<BuzonMensajes>();
        // Buscamos a todos los guardias de la escena dinámicamente (Descentralizado)
        todosLosGuardias = FindObjectsOfType<IACerebro>(); 
   
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
        // El guardia revisa su buzón en cada fotograma
        if (miBuzon != null && miBuzon.HayMensajesNuevos())
        {
            ProcesarBuzon();
        }
    }
    
    // --- RESPUESTA A LOS EVENTOS DE LOS SENSORES ---
    private void AlDetectar(Vector3 pos)
    {
        if (estadoActual != persecucion)
        {
            listaDeOfertas.Clear(); // Limpiamos la mesa de subastas
            EnviarAvisoDeLadron(pos); // Gritamos pidiendo ayuda (CFP)
            StartCoroutine(CerrarSubastaYAsignar(pos)); // Arrancamos el cronómetro
        }
        
        objetivoDetectado = true;
        ultimaPosJugador = pos;
        CambiarEstado(persecucion);
    }

    private void AlPerder()
    {
        objetivoDetectado = false;
    }

    // --- EL MOTOR QUE CAMBIA LOS ESTADOS ---
    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;
        
        if (estadoActual != null) estadoActual.AlSalir(); // Apaga el viejo
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar(); // Enciende el nuevo
    }

    private void EnviarAvisoDeLadron(Vector3 posLadron)
    {
        string contenido = posLadron.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                           posLadron.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

        foreach (IACerebro compañero in todosLosGuardias)
        {
            if (compañero != this)
            {
                MensajeFIPA aviso = new MensajeFIPA(
                    PerformativaFIPA.CFP, // <--- AHORA ES UNA PETICIÓN DE VOLUNTARIOS
                    this.gameObject,         
                    compañero.gameObject,    
                    contenido                
                );
                compañero.GetComponent<BuzonMensajes>().RecibirMensaje(aviso);
            }
        }
        Debug.Log("📢 ¡" + gameObject.name + " pide voluntarios (CFP)! Ladrón en: " + contenido);
    }

    private void ProcesarBuzon()
        {
            MensajeFIPA mensaje = miBuzon.ExtraerSiguienteMensaje();
            if (mensaje == null) return;

            // ==========================================================
            // CASO 1: SOY UN PATRULLA Y RECIBO UNA LLAMADA DE AUXILIO (CFP)
            // ==========================================================
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

            // ==========================================================
            // CASO 2: SOY EL LÍDER Y RECIBO OFERTAS DE MIS COMPAÑEROS (PROPOSE)
            // ==========================================================
            else if (mensaje.performativa == PerformativaFIPA.PROPOSE)
            {
                float distancia = float.Parse(mensaje.contenido, System.Globalization.CultureInfo.InvariantCulture);
                
                // Anotamos la oferta en la libreta
                listaDeOfertas.Add(new Oferta { guardia = mensaje.emisor, distancia = distancia });
                
                Debug.Log("📝 " + gameObject.name + " anota la oferta de " + mensaje.emisor.name + " (" + distancia + "m)");
            }

            // ==========================================================
            // CASO 3: SOY UN PATRULLA Y ME HAN ACEPTADO EN EL EQUIPO (ACCEPT)
            // ==========================================================
            else if (mensaje.performativa == PerformativaFIPA.ACCEPT_PROPOSAL)
            {
                // El contenido ahora son las coordenadas tácticas (El flanco Norte, Sur, etc.)
                string[] coordenadas = mensaje.contenido.Split('|'); 
                if (coordenadas.Length == 3)
                {
                    float x = float.Parse(coordenadas[0], System.Globalization.CultureInfo.InvariantCulture);
                    float y = float.Parse(coordenadas[1], System.Globalization.CultureInfo.InvariantCulture);
                    float z = float.Parse(coordenadas[2], System.Globalization.CultureInfo.InvariantCulture);

                    Vector3 posicionTactica = new Vector3(x, y, z);
                    
                    Debug.Log("🪖 ¡" + gameObject.name + " asume posición táctica en el equipo SWAT!");
                    
                    // Actualizo mi memoria al punto táctico (NO a donde está el ladrón)
                    ultimaPosJugador = posicionTactica; 
                    CambiarEstado(emboscada); // <-- USO EL CARTUCHO DE EMBOSCADA PARA IR AL FLANCO
                }
            }

            // ==========================================================
            // CASO 4: SOY UN PATRULLA Y ME HAN RECHAZADO (REJECT)
            // ==========================================================
            else if (mensaje.performativa == PerformativaFIPA.REJECT_PROPOSAL)
            {
                Debug.Log("🤷‍♂️ " + gameObject.name + " ignorado por el líder. Continúo mi patrulla.");
            }
        }
    
    private IEnumerator CerrarSubastaYAsignar(Vector3 posLadron)
    {
        // 1. Damos un tiempo de margen (0.5 seg) para que lleguen todas las cartas de los compañeros
        yield return new WaitForSeconds(0.5f);

        // 2. Ordenamos la libreta: del guardia más cercano al más lejano
        listaDeOfertas.Sort((a, b) => a.distancia.CompareTo(b.distancia));

        int guardiasAceptados = 0;
        int maxGuardias = 2; // SOLO QUEREMOS A LOS 2 MÁS CERCANOS

        // Puntos estratégicos para CERRAR EL PASO (Ej: a 5 metros por la derecha y por la izquierda)
        Vector3[] puntosEstrategicos = new Vector3[] {
            posLadron + new Vector3(5f, 0, 5f),  // Flanco 1
            posLadron + new Vector3(-5f, 0, -5f) // Flanco 2
        };

        // 3. Repartimos los puestos
        foreach (Oferta oferta in listaDeOfertas)
        {
            if (guardiasAceptados < maxGuardias)
            {
                // Le damos el punto estratégico a este guardia
                Vector3 punto = puntosEstrategicos[guardiasAceptados];
                string contAceptar = punto.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + 
                                     punto.z.ToString(System.Globalization.CultureInfo.InvariantCulture);

                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.ACCEPT_PROPOSAL, this.gameObject, oferta.guardia, contAceptar);
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
                
                guardiasAceptados++;
                Debug.Log("🏆 [SUBASTA] Gana " + oferta.guardia.name + ". Va a cerrar el paso.");
            }
            else
            {
                // A los demás (el 3º, 4º guardia...) los mandamos a freír espárragos
                MensajeFIPA respuesta = new MensajeFIPA(PerformativaFIPA.REJECT_PROPOSAL, this.gameObject, oferta.guardia, "Sigue patrullando");
                oferta.guardia.GetComponent<BuzonMensajes>().RecibirMensaje(respuesta);
                Debug.Log("❌ [SUBASTA] Rechazado " + oferta.guardia.name + " por ser el más lento/lejano.");
            }
        }
    }    
} 