using UnityEngine;

/// <summary>
/// Makes a field read-only in the inspector. This is purely cosmetic — it does not prevent
/// writing to the field in code, and it does not make the field serialized if it otherwise
/// wouldn't be (private fields are not serialized by default, even with this attribute).
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute
{
}