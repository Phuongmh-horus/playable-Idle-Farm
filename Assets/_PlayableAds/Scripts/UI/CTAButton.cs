using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class CTAButton : MonoBehaviour
{

    private Button button;
    private Coroutine _buttonPulseRoutine;

    void Start()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
            _buttonPulseRoutine = StartCoroutine(ButtonImpactThenPulseRoutine());
        }
    }

    protected virtual void OnButtonClicked()
    {
        // Play button click sound effect
        SoundManager.Instance.PlayOneShot(AudioClipName.sfx_ui_click);

    }



    [SerializeField] private float buttonPulseDuration = 0.6f;

    // [SerializeField] private float ctaImpactStartAngle = -18f;

    [SerializeField] private float ctaImpactDuration = 0.18f;
    [SerializeField] private float ctaImpactStartScale = 1.7f;
    [SerializeField] private float ctaOnlyPulseScale = 1.1f;
    [SerializeField] private float ctaOnlyStartScale = 0.9f;
    [SerializeField] private float buttonPulseScale = 1.08f;

    private IEnumerator ButtonImpactThenPulseRoutine()
    {
        // float impactDuration = Mathf.Max(0.05f, ctaImpactDuration);
        // float impactT = 0f;
        // while (impactT < impactDuration)
        // {
        //     impactT += Time.unscaledDeltaTime;
        //     float k = Mathf.Clamp01(impactT / impactDuration);
        //     float scale = Mathf.Lerp(ctaImpactStartScale, 1f, k);
        //     float angle = Mathf.Lerp(ctaImpactStartAngle, 0f, k);
        //     for (int i = 0; i < ctaButtons.Count; i++)
        //     {
        //         var btn = ctaButtons[i];
        //         if (btn == null || !btn.gameObject.activeInHierarchy) continue;
        //         var rt = btn.transform as RectTransform;
        //         if (rt != null)
        //         {
        //             rt.localScale = new Vector3(scale, scale, scale);
        //             rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        //         }
        //         else
        //         {
        //             btn.transform.localScale = new Vector3(scale, scale, scale);
        //             btn.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        //         }
        //     }
        //     yield return null;
        // }



        button.transform.localRotation = Quaternion.identity;


        float duration = Mathf.Max(0.05f, buttonPulseDuration);
        while (true)
        {
            float t = Mathf.PingPong(Time.unscaledTime / duration, 1f);
            float scale;
            // if (true)
            // {
            scale = Mathf.Lerp(ctaOnlyStartScale, ctaOnlyPulseScale, t);
            // }
            // else
            // {
            //     float targetScale = buttonPulseScale;
            //     scale = Mathf.Lerp(1f, targetScale, t);
            // }

            button.transform.localScale = new Vector3(scale, scale, scale);

            yield return null;
        }

        // _buttonPulseRoutine = null;
    }
}
