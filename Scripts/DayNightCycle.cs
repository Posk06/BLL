using UnityEngine;

public class DayNightCycle : MonoBehaviour
{

    public int minituresPerDay = 1440;
    Transform sun;
    Transform moon;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sun = transform.GetChild(0);
        moon = transform.GetChild(1);
    }

    float time = 0f;
    // Update is called once per frame
    void Update()
    { 
        time += Time.deltaTime / 60f;
        float sunAngle = time / minituresPerDay * 360f;
        sun.rotation = Quaternion.Euler(sunAngle, 0f, 0f);
        moon.rotation = Quaternion.Euler(sunAngle + 180f, 0f, 0f);
    }
}
