using UnityEngine;
using System.Collections.Generic;

public enum EstadoTactico
{
    Libre,
    Comandante,
    Subordinado
}

public enum FaseAlerta
{
    Tranquilidad,
    ContactoVisual,
    BusquedaActiva,
}

public enum RolTactico
{
    PatrullaNormal,
    PersecucionActiva,
    BloqueoSalida,
    ExplorarSectorSospechoso,
    PatrullaSectorAdyacente
}

[System.Serializable]
public class DatosContrato
{
    public RolTactico rolOfertado;
    public List<Vector3> puntosDeRuta = new List<Vector3>();
    public Vector3 coordenadaObjetivo;
    public string nombreSectorDestino;
}

public enum PerformativaFIPA
{
    INFORM,
    REQUEST,
    CFP,
    PROPOSE,
    REFUSE,
    ACCEPT_PROPOSAL,
    REJECT_PROPOSAL
}

[System.Serializable]
public class MensajeFIPA
{
    public PerformativaFIPA performativa;
    public GameObject emisor;
    public GameObject receptor;
    public string contenido;

    public MensajeFIPA(PerformativaFIPA perf, GameObject emi, GameObject rec, string cont)
    {
        performativa = perf;
        emisor = emi;
        receptor = rec;
        contenido = cont;
    }
}
