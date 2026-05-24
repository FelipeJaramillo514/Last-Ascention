using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RunSummaryUI : UIModalPanel
{
    [Header("Run Summary")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private Text titleText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text goldText;
    [SerializeField] private Text footerText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Text continueButtonLabel;
    [SerializeField] private Text hintText;

    private Coroutine showRoutine;
    private Action pendingContinue;
    private bool canDismiss;

    public void Show(RunStats stats, Action continueAction)
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        pendingContinue = continueAction;
        canDismiss = false;
        Open();
        showRoutine = StartCoroutine(ShowRoutine(stats));
    }

    protected override void OnEscapePressed()
    {
        if (!canDismiss)
        {
            return;
        }

        Dismiss();
    }

    protected override void OnClosing()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
    }

    protected override void ValidateEditorReferences()
    {
        base.ValidateEditorReferences();
        if (panelRoot == null || titleText == null || continueButton == null)
        {
            Debug.LogError("[RunSummaryUI] Asigna panel, textos y botón en el prefab.", this);
        }
    }

    private IEnumerator ShowRoutine(RunStats stats)
    {
        titleText.text = string.Empty;
        bodyText.text = string.Empty;
        goldText.text = string.Empty;
        footerText.text = string.Empty;

        bool playerDied = stats != null && stats.playerDied;
        titleText.color = stats != null && stats.completed ? new Color(0.3f, 1f, 0.45f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
        continueButton.interactable = false;
        continueButton.gameObject.SetActive(false);
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(Dismiss);

        if (hintText != null)
        {
            hintText.text = playerDied ? "ESC — Cerrar (tras animación)" : "ESC — Cerrar";
            hintText.gameObject.SetActive(true);
        }

        float fadeDuration = playerDied ? 3f : 0.35f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        string titleValue = stats != null && stats.completed ? "RUN COMPLETADA" : "KAISEN HA CAIDO";
        yield return Typewrite(titleText, titleValue, playerDied ? 0.05f : 0.015f);

        if (playerDied)
        {
            yield return new WaitForSecondsRealtime(0.45f);
            footerText.color = new Color(0.45f, 0.75f, 1f, 1f);
            yield return Typewrite(footerText, "El sistema continua. Lira te espera.", 0.035f);
            yield return new WaitForSecondsRealtime(0.4f);
        }
        else
        {
            footerText.text = string.Empty;
        }

        float countDuration = 1.4f;
        float countElapsed = 0f;
        int targetGold = stats != null ? stats.goldEarned : 0;
        while (countElapsed < countDuration)
        {
            countElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(countElapsed / countDuration);
            int displayedGold = Mathf.RoundToInt(Mathf.Lerp(0f, targetGold, t));
            bodyText.text = BuildStatsText(stats, t);
            goldText.text = "Cristales obtenidos: " + displayedGold;
            yield return null;
        }

        bodyText.text = BuildStatsText(stats, 1f);
        goldText.text = "Cristales obtenidos: " + targetGold;

        if (playerDied)
        {
            yield return new WaitForSecondsRealtime(5f);
        }

        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;
        canDismiss = true;
        if (hintText != null)
        {
            hintText.text = "ESC — Cerrar";
        }

        showRoutine = null;
    }

    private void Dismiss()
    {
        Action callback = pendingContinue;
        pendingContinue = null;
        Close();
        callback?.Invoke();
    }

    private IEnumerator Typewrite(Text target, string value, float interval)
    {
        target.text = string.Empty;
        if (string.IsNullOrEmpty(value))
        {
            yield break;
        }

        for (int i = 0; i < value.Length; i++)
        {
            target.text += value[i];
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private string BuildStatsText(RunStats stats, float t)
    {
        int enemies = stats != null ? Mathf.RoundToInt(stats.enemiesKilled * t) : 0;
        int rooms = stats != null ? Mathf.RoundToInt(stats.roomsCleared * t) : 0;
        int shadows = stats != null ? Mathf.RoundToInt(stats.shadowsExtracted * t) : 0;
        int level = stats != null ? Mathf.RoundToInt(stats.levelReached * t) : 0;
        float timeValue = stats != null ? stats.timeElapsed * t : 0f;
        return string.Format(
            "Enemigos derrotados: {0}\nSalas limpiadas: {1}\nSombras extraidas: {2}\nNivel alcanzado: {3}\nTiempo: {4}",
            enemies,
            rooms,
            shadows,
            level,
            FormatTime(timeValue));
    }

    private string FormatTime(float timeElapsed)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(Mathf.Max(0f, timeElapsed));
        return string.Format("{0:00}:{1:00}", timeSpan.Minutes, timeSpan.Seconds);
    }
}
