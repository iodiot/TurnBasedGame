using UnityEngine;

public class HealthBar : MonoBehaviour
{
    private GameObject greenBarObject;

    private void Awake()
    {
        greenBarObject = transform.Find("GreenBar").gameObject;
    }

    /// <summary>
    /// Updates the width of the progress-bar.
    /// </summary>
    /// <param name="value">
    /// The range of possible values is [0f, 1f].
    /// </param>
    public void SetHealth(float value)
    {
        greenBarObject.transform.localScale = new Vector3(value, 1f, 1f);

        var pos = transform.position;
        pos.x += value * .5f - .5f ;

        greenBarObject.transform.position = pos;
    }
}
