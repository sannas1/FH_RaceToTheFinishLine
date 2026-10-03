using UnityEngine;

public static class RaceTimeFormatter
{
    public static string Format(float seconds)
    {
        if (seconds < 0f) seconds = 0f;

        int minutes = Mathf.FloorToInt(seconds / 60f);
        int wholeSeconds = Mathf.FloorToInt(seconds % 60f);
        int milliseconds = Mathf.FloorToInt((seconds - Mathf.Floor(seconds)) * 1000f);

        return $"{minutes:00}:{wholeSeconds:00}.{milliseconds:000}";
    }
}
