using UnityEngine;

/// <summary>
/// این اسکریپت زاویه جسم را در یک مقدار ثابت نگه می‌دارد
/// حتی اگر والد آن بچرخد، زاویه این جسم تغییر نمی‌کند
/// مناسب برای سایه مصنوعی (fake shadow)
/// </summary>
public class FixedRotation : MonoBehaviour
{
    [Header("تنظیمات زاویه ثابت")]
    [Tooltip("زاویه ثابت جسم (درجه)")]
    public Vector3 fixedRotation = Vector3.zero;

    [Tooltip("قفل کردن محور X")]
    public bool lockX = true;

    [Tooltip("قفل کردن محور Y")]
    public bool lockY = true;

    [Tooltip("قفل کردن محور Z")]
    public bool lockZ = true;

    private Quaternion targetRotation;

    private void Start()
    {
        // تبدیل زاویه به Quaternion
        targetRotation = Quaternion.Euler(fixedRotation);
        
        // تنظیم زاویه اولیه
        ApplyRotation();
    }

    private void LateUpdate()
    {
        // در هر فریم زاویه را به مقدار ثابت تنظیم می‌کنیم
        // LateUpdate استفاده می‌شود تا بعد از تمام تغییرات والد اجرا شود
        ApplyRotation();
    }

    private void ApplyRotation()
    {
        // استفاده از world space برای نادیده گرفتن کامل چرخش والد
        Vector3 currentEuler = transform.eulerAngles;
        Vector3 newEuler = currentEuler;

        // اعمال زاویه ثابت فقط برای محورهای قفل شده
        if (lockX) newEuler.x = fixedRotation.x;
        if (lockY) newEuler.y = fixedRotation.y;
        if (lockZ) newEuler.z = fixedRotation.z;

        // تنظیم در فضای جهانی
        transform.rotation = Quaternion.Euler(newEuler);
    }

    /// <summary>
    /// تغییر زاویه ثابت در زمان اجرا
    /// </summary>
    public void SetFixedRotation(Vector3 newRotation)
    {
        fixedRotation = newRotation;
        targetRotation = Quaternion.Euler(fixedRotation);
    }

    /// <summary>
    /// تغییر زاویه ثابت در زمان اجرا (با Quaternion)
    /// </summary>
    public void SetFixedRotation(Quaternion newRotation)
    {
        fixedRotation = newRotation.eulerAngles;
        targetRotation = newRotation;
    }
}

