using UnityEngine;

// Módulo exclusivo para la lógica visual (Responsabilidad Única)
public class SensorVision : MonoBehaviour
{
    [Header("Visión")]
    public float distanciaVision = 15f;
    [Range(90f, 250f)] public float anguloVision = 90f;
    public float alturaOjos = 1.6f;
    public float alturaObjetivoRelativa = 1.0f;

    // Ya no hace Update, solo evalúa cuando se lo piden
    public bool EvaluarVision(Transform jugador)
    {
        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 objetivo = jugador.position + Vector3.up * alturaObjetivoRelativa;
        Vector3 dir = objetivo - ojos;
        float distancia = dir.magnitude;

        if (distancia > distanciaVision) return false;

        Vector3 dirH = dir; dirH.y = 0f;
        if (dirH.sqrMagnitude < 0.001f) return true;

        float ang = Vector3.Angle(transform.forward, dirH.normalized);
        if (ang > anguloVision / 2f) return false;

        RaycastHit[] hits = Physics.RaycastAll(ojos, dir.normalized, distancia, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

            if (hit.transform == jugador || hit.transform.IsChildOf(jugador) || hit.collider.CompareTag("Player"))
                return true;
            else
                return false; 
        }
        return false;
    }
}