using UnityEngine;
using System.Collections.Generic;

// Define una tarea táctica que el planificador asigna a un guardia
[System.Serializable]
public class TareaContrato
{
    public string nombre;
    public RolTactico rol;
    public int guardiasNecesarios = 1;
    public Vector3 coordenada;
    public List<Vector3> puntosDeRuta = new List<Vector3>();
}
