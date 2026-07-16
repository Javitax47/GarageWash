using System;

public static class TutorialEvents
{
    // Evento para cuando el preview aparece por primera vez
    public static event Action OnSurfaceFound;
    public static void FireOnSurfaceFound() => OnSurfaceFound?.Invoke();

    // Evento para cuando el usuario usa los gestos de rotar/escalar
    public static event Action OnObjectScaledOrRotated;
    public static void FireOnObjectScaledOrRotated() => OnObjectScaledOrRotated?.Invoke();

    // Evento para cuando el objeto final es colocado
    public static event Action OnObjectPlaced;
    public static void FireOnObjectPlaced() => OnObjectPlaced?.Invoke();

    // Evento para cuando el usuario empieza a lavar
    public static event Action OnWashingStarted;
    public static void FireOnWashingStarted() => OnWashingStarted?.Invoke();
}