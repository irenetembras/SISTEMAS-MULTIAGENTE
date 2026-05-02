using System.Collections.Generic;
using UnityEngine;

public class BuzonMensajes : MonoBehaviour
{
    // Cola FIFO: el primer mensaje en entrar es el primero en procesarse
    private Queue<MensajeFIPA> bandejaDeEntrada = new Queue<MensajeFIPA>();

    public void RecibirMensaje(MensajeFIPA nuevoMensaje)
    {
        bandejaDeEntrada.Enqueue(nuevoMensaje);
    }

    public bool HayMensajesNuevos()
    {
        return bandejaDeEntrada.Count > 0;
    }

    public MensajeFIPA ExtraerSiguienteMensaje()
    {
        if (bandejaDeEntrada.Count > 0)
        {
            return bandejaDeEntrada.Dequeue();
        }
        return null;
    }
}
