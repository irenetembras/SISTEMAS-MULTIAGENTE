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
    // public void ActualizarMaquina()
    // {
    //     if (estadoActual == null) return;

    //     // --- TRANSICIONES LIMPIAS (Sin magia) ---
        
    //     // (¡Fíjate que hemos borrado el if de la patrulla! Ahora solo dejan de patrullar si ven algo o se lo dicen por radio)

    //     if (estadoActual == persecucion)
    //     {
    //         if (!cerebro.objetivoDetectado)
    //         {
    //             cronometroPerdido += Time.deltaTime;
    //             if (cronometroPerdido >= tiempoPerdidoParaBuscar)
    //             {
    //                 // Si te pierden de vista, se ponen a buscarte. Ya no saben mágicamente si tienes el botín.
    //                 CambiarEstado(busqueda);
    //             }
    //         }
    //         else cronometroPerdido = 0f;
    //     }
    //     else if (estadoActual == busqueda)
    //     {
    //         if (((EstadoBusqueda)busqueda).busquedaTerminada) CambiarEstado(exploracion);
    //     }
    //    else if (estadoActual == exploracion)
    //     {
    //         // Al terminar de explorar, SIEMPRE van a comprobar el tesoro
    //         if (((EstadoExploracion)exploracion).exploracionTerminada) 
    //             CambiarEstado(comprobandoObjetivo);
    //     }
    //     else if (estadoActual == comprobandoObjetivo)
    //     {
    //         // Si llegan al tesoro y todo está bien (el botín sigue allí), 
    //         // no se relajan: vuelven a buscarte por el sector eternamente.
    //         if (movimiento.HaLlegadoAlDestino() && !RecogerObjetivo.tieneElBotin)
    //         {
    //             CambiarEstado(exploracion); // Bucle infinito de vigilancia
    //         }
    //     }
    // }

    public void ActualizarMaquina()
    {
        if (estadoActual == null) return;

        // ARREGLO 1: En persecución ya no hay cronómetro. Se persigue hasta que el líder diga basta.
        if (estadoActual == persecucion) 
        { 
            /* No hacemos nada, el GPS o la vista nos guía */ 
        }
        // else if (estadoActual == busqueda)
        // {
        //     if (((EstadoBusqueda)busqueda).busquedaTerminada) CambiarEstado(exploracion);
        // }
        else if (estadoActual == exploracion)
        {
            // ARREGLO 2: Bucle infinito de exploración. Nunca vuelven a la normalidad.
            if (((EstadoExploracion)exploracion).exploracionTerminada) CambiarEstado(exploracion); 
        }
    }

    public void CambiarEstado(EstadoIA nuevoEstado)
    {
        if (nuevoEstado == null) return;

        if (estadoActual != null) estadoActual.AlSalir();
        estadoActual = nuevoEstado;
        estadoActual.AlEntrar();
    }
}