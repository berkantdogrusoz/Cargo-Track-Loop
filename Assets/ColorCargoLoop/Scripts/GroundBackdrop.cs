using UnityEngine;

namespace ColorCargoLoop
{
    /// <summary>
    /// ARKA PLAN zemin quad'i (kum vb. duz dokulu tema zemini): kameranin tum gorusune oturur.
    /// BackgroundWater'dan farki: materyale HIC dokunmaz (unlit kum materyali Inspector'dan),
    /// ve her kare kendini oturtur - cameraZoomOut/aspect degisse de kenar acigi kalmaz.
    /// Kurulum: "Color Cargo Loop > Dekor > Col Kum Zemini Kur" menusu.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class GroundBackdrop : MonoBehaviour
    {
        [Tooltip("Bos = Camera.main. Zemin bu kameranin gorusune tam oturur.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Kameradan uzaklik - oyundaki her seyin ARKASINDA kalacak kadar buyuk olsun.")]
        [SerializeField] private float distance = 30f;
        [Tooltip("Ekran kenarlarindan tasma payi (1.15 = %15 tasar; kenar acigi kalmasin).")]
        [SerializeField] private float overscan = 1.15f;

        void LateUpdate()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            float d = Mathf.Min(distance, cam.farClipPlane * 0.9f);
            Vector3 pos = cam.transform.position + cam.transform.forward * d;
            Quaternion rot = cam.transform.rotation;
            float h = cam.orthographic
                ? cam.orthographicSize * 2f
                : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Vector3 scale = new Vector3(h * cam.aspect * overscan, h * overscan, 1f);
            // degismediyse yazma: edit modunda sahneyi surekli "kirli" isaretlemesin
            if ((transform.position - pos).sqrMagnitude < 0.0001f &&
                Quaternion.Angle(transform.rotation, rot) < 0.01f &&
                (transform.localScale - scale).sqrMagnitude < 0.0001f) return;
            transform.position = pos;
            transform.rotation = rot;
            transform.localScale = scale;
        }
    }
}
