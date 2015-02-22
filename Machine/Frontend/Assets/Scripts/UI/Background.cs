using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class Background : MonoBehaviour
{
    void Start()
    {
        sp_renderer = renderer as SpriteRenderer;
    }

    void Update()
    {
        time += Time.deltaTime;

        float a = Mathf.Sin(startPoint.w + (time * Mathf.PI * 2 * blurFrequencies.w)) * 0.5f + 0.5f;
        float r = Mathf.Sin(startPoint.x + (time * Mathf.PI * 2 * blurFrequencies.x)) * 0.5f + 0.5f;
        float g = Mathf.Sin(startPoint.y + (time * Mathf.PI * 2 * blurFrequencies.y)) * 0.5f + 0.5f;
        float b = Mathf.Sin(startPoint.z + (time * Mathf.PI * 2 * blurFrequencies.z)) * 0.5f + 0.5f;
        //
        sp_renderer.color = new Color(r, g, b, a);
    }

    private float time;

    [SerializeField]
    private Vector4 startPoint;

    private SpriteRenderer sp_renderer;

    [SerializeField]
    private Vector4 blurFrequencies;
}
