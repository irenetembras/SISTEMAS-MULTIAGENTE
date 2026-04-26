using System.Collections.Generic;
using UnityEngine;

public class BuzonMensajes : MonoBehaviour
{
    // Usamos una "Queue" (Cola) en lugar de un Array o una List.
    // Una cola es FIFO (First In, First Out): El primer mensaje en llegar es el primero en leerse.
    private Queue<MensajeFIPA> bandejaDeEntrada = new Queue<MensajeFIPA>();

    // 1. FUNCIÓN PARA RECIBIR (Cualquier otro guardia llama a esta función para dejar una carta)
    public void RecibirMensaje(MensajeFIPA nuevoMensaje)
    {
        // Añadimos el mensaje a la cola
        bandejaDeEntrada.Enqueue(nuevoMensaje);
        // Opcional: Un chivato en la consola para ver que funciona
        // Debug.Log(gameObject.name + " ha recibido un mensaje de " + nuevoMensaje.emisor.name);
        
    }

    // 2. FUNCIÓN PARA SABER SI HAY CARTAS SIN LEER
    public bool HayMensajesNuevos()
    {
        return bandejaDeEntrada.Count > 0;
    }

    // 3. FUNCIÓN PARA LEER LA CARTA MÁS ANTIGUA
    public MensajeFIPA ExtraerSiguienteMensaje()
    {
        if (bandejaDeEntrada.Count > 0)
        {
            // Dequeue saca el mensaje de la cola y nos lo da (lo borra del buzón)
            return bandejaDeEntrada.Dequeue();
        }

        return null;
    }
} 