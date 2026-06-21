using UnityEngine;

public static class UnitHelper
{
    public enum UnitMode { Auto, ForceMeter, ForceCm, ForceMm }

    public static string FormatLength(float meters, UnitMode mode = UnitMode.Auto, int decimals = 2)
    {
        switch (mode)
        {
            case UnitMode.ForceMeter:
                return $"{meters.ToString($"F{decimals}")} m";
            case UnitMode.ForceCm:
                return $"{(meters * 100f).ToString($"F{decimals}")} cm";
            case UnitMode.ForceMm:
                return $"{(meters * 1000f).ToString("F0")} mm";
            case UnitMode.Auto:
            default:
                if (Mathf.Abs(meters) >= 1f)
                    return $"{meters.ToString($"F{decimals}")} m";
                else if (Mathf.Abs(meters) >= 0.01f)
                    return $"{(meters * 100f).ToString($"F{decimals}")} cm";
                else
                    return $"{(meters * 1000f).ToString("F0")} mm";
        }
    }

    public static string FormatVolume(float metersCubed, UnitMode mode = UnitMode.Auto, int decimals = 2)
    {
        switch (mode)
        {
            case UnitMode.ForceMeter:
                return $"{metersCubed.ToString($"F{decimals})")} m³";
            case UnitMode.ForceCm:
                return $"{(metersCubed * 1_000_000f).ToString("F0")} cm³";
            case UnitMode.Auto:
            default:
                if (Mathf.Abs(metersCubed) >= 0.001f)
                    return $"{metersCubed.ToString($"F{decimals}")} m³";
                else
                    return $"{(metersCubed * 1_000_000f).ToString("F0")} cm³";
        }
    }
}