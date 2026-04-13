using UnityEngine;

public class MaquinaDeEstados : MonoBehaviour
{
    [Header("Cartuchos de Comportamiento")]
    public EstadoIA patrulla;
    public EstadoIA persecucion;
    public EstadoIA emboscada;
    public EstadoIA busqueda;              
    public EstadoIA exploracion;           
    public EstadoIA comprobandoObjetivo;   

    [Header("Ajustes de Tiempo")]
    public float tiempoPerdidoParaBuscar = 2.0f;
    private float cronometroPerdido = 0f;

    public EstadoIA estadoActual { get; private set; }

    // Referencias para poder leer la memoria y movernos
    private IACerebro cerebro;
    private IAMovimiento movimiento;

    // El cerebro llama a esto al despertar
    public void Inicializar(IACerebro cerebroRef, IAMovimiento movRef)
    {
        cerebro = cerebroRef;
        movimiento = movRef;

        if(patrulla) patrulla.Configurar(cerebro, movimiento);
        if(persecucion) persecucion.Configurar(cerebro, movimiento);
        if(emboscada) emboscada.Configurar(cerebro, movimiento);
        if(busqueda) busqueda.Configurar(cerebro, movimiento); 
        if(exploracion) exploracion.Configurar(cerebro, movimiento);
        if(comprobandoObjetivo) comprobandoObjetivo.Configurar(cerebro, movimiento); 

        CambiarEstado(patrulla);
    }

    // El cerebro llama a esto en su propio Update
    public void ActualizarMaquina()
    {
        if (estadoActual == null) return;

        // --- TRANSICIONES LIMPIAS (Sin magia) ---
        
        // (¡Fíjate que hemos borrado el if de la patrulla! Ahora solo dejan de patrullar si ven algo o se lo dicen por radio)

        if (estadoActual == persecucion)
        {
            if (!cerebro.objetivoDetectado)
            {
                cronometroPerdido += Time.deltaTime;
                if (cronometroPerdido >= tiempoPerdidoParaBuscar)
                {
                    // Si te pierden de vista, se ponen a buscarte. Ya no saben mágicamente si tienes el botín.
                    CambiarEstado(busqueda);
                }
            }
            else cronometroPerdido = 0f;
        }
        else if (estadoActual == busqueda)
        {
            if (((EstadoBusqueda)busqueda).busquedaTerminada) CambiarEstado(exploracion);
        }
        else if (estadoActual == exploracion)
        {
            if (((EstadoExploracion)exploracion).exploracionTerminada) CambiarEstado(comprobandoObjetivo);
        }
        else if (estadoActual == comprobandoObjetivo)
        {
            float distAlBotin = Vector3.Distance(transform.position, cerebro.puntoObjetivo.position);
            if (movimiento.HaLlegadoAlDestino() || distAlBotin < 4.0f)
            {
                CambiarEstado(patrulla);
            }
        }
    }

    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;

        // Si volvemos a patrullar, le avisamos al cerebro de que estamos libres
        if (nuevoEstado == patrulla) cerebro.enMisionAsignada = false; 

        if (estadoActual != null) estadoActual.AlSalir(); 
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar(); 
        Debug.Log("<color=cyan>FSM: Cambiando al estado -> " + nuevoEstado.GetType().Name + "</color>");
    }
}