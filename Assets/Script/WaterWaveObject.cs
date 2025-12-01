using UnityEngine;

public class WaterWaveObject : MonoBehaviour
{
    private Vector3 basePosition;
    private float waveSpeed;
    private float waveAmplitude;
    private float phaseOffset;
    private float time;
    
    public float PhaseOffset => phaseOffset;
    
    public void Initialize(Vector3 basePos, float speed, float amplitude, float offset)
    {
        basePosition = basePos;
        waveSpeed = speed;
        waveAmplitude = amplitude;
        phaseOffset = offset;
        time = 0f;
    }
    
    public void UpdateSettings(float speed, float amplitude, float offset)
    {
        waveSpeed = speed;
        waveAmplitude = amplitude;
        phaseOffset = offset;
    }
    
    private void Update()
    {
        time += Time.deltaTime;
        
        // محاسبه حرکت دایره‌ای با استفاده از Sin و Cos
        // برای موج دو بعدی، معمولاً فقط در یک محور (مثلاً Y) حرکت می‌کنه
        // و در محور دیگه (مثلاً X) هم می‌تونه حرکت کنه برای موج‌وارگی بیشتر
        
        float wavePhase = (time * waveSpeed) + phaseOffset;
        
        // حرکت دایره‌ای: استفاده از Sin برای Y و Cos برای X (یا برعکس)
        // این یک حرکت دایره‌ای کامل ایجاد می‌کنه
        float offsetX = Mathf.Cos(wavePhase) * waveAmplitude;
        float offsetY = Mathf.Sin(wavePhase) * waveAmplitude;
        
        // اعمال حرکت به موقعیت
        transform.position = basePosition + new Vector3(offsetX, offsetY, 0f);
    }
    
    // برای تنظیم موقعیت پایه در runtime
    public void SetBasePosition(Vector3 newBasePosition)
    {
        basePosition = newBasePosition;
    }
}









