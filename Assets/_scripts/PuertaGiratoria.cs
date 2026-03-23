using System.Collections; // ¡Súper importante para el yield!
using UnityEngine;

public class PuertaGiratoria : MonoBehaviour
{
    
    public bool estaAbierto = true; 
    public float tiempoEspera = 8.0f; // Tiempo entre abrir y cerrar (¡con la f de float!)
    public float anguloDeApertura = 90f; // Grados que va a girar
    public float velocidadGiro = 3.0f; // Lo rápido que se mueve

    private Quaternion rotacionCerrada;
    private Quaternion rotacionAbierta;


    void Start()
    {
        // Guardamos cómo está la puerta al empezar (Cerrada)
        rotacionCerrada = transform.rotation;
        
        // Calculamos cómo estará cuando se abra (Sumando los 90 grados al eje Y)
        rotacionAbierta = Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y + anguloDeApertura, transform.eulerAngles.z);

        
        // Arrancamos "El Cerebro"
        StartCoroutine(BucleDeTiempo());
    }

    // EL CEREBRO: Espera 3 segundos y cambia el interruptor
    IEnumerator BucleDeTiempo()
    {
        while (true) // Bucle infinito
        {
            yield return new WaitForSeconds(tiempoEspera);
            // 2. ¡IMPORTANTE! Actualizamos el NavMeshLink aqu
            // Si no lo haces aquí, el interruptor cambia pero el link nunca se entera
            estaAbierto =!estaAbierto;
        }
        
    }

    // LOS MÚSCULOS: Se ejecuta cada frame para mover la puerta suavemente
    void Update()
    {
        // Decide cuál es la meta según el interruptor
        Quaternion objetivo = estaAbierto ? rotacionAbierta : rotacionCerrada;
        
        // Mueve la puerta poco a poco hacia esa meta
        transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, Time.deltaTime * velocidadGiro);
    }
}